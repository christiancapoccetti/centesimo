using System.Globalization;
using System.IO.Compression;
using Centesimo.Application;
using Microsoft.Data.Sqlite;

namespace Centesimo.Infrastructure;

public sealed class MoneyManagerBackupReader : IMoneyManagerBackupReader
{
    private const long MaximumArchiveBytes = 64L * 1024 * 1024;
    private const long MaximumDatabaseBytes = 256L * 1024 * 1024;
    private const int MaximumEntries = 32;
    private const int MaximumRowsPerTable = 100_000;
    private const int MaximumUidLength = 200;
    private static readonly string[] Icons = ["cart", "home", "car", "heart", "more"];
    private static readonly string[] Colors = ["#176B5B", "#8B4A5D", "#725C00", "#4F5F7A", "#6A4F88"];

    public async Task<Result<MoneyManagerImportData>> Read(Stream backup,
        CancellationToken cancellationToken = default)
    {
        var token = Guid.NewGuid().ToString("N");
        var archivePath = Path.Combine(Path.GetTempPath(), $"centesimo-{token}.zip");
        var databasePath = Path.Combine(Path.GetTempPath(), $"centesimo-{token}.db");
        try
        {
            var header = new byte[8];
            if (await backup.ReadAtLeastAsync(header, header.Length, false, cancellationToken) != header.Length ||
                header.Any(value => value != 0x08))
                return Result<MoneyManagerImportData>.Failure(MoneyManagerImportErrors.InvalidBackup);

            await using (var archiveFile = File.Create(archivePath))
                await CopyWithLimit(backup, archiveFile, MaximumArchiveBytes, cancellationToken);

            using var archive = ZipFile.OpenRead(archivePath);
            if (archive.Entries.Count > MaximumEntries)
                return Result<MoneyManagerImportData>.Failure(MoneyManagerImportErrors.InvalidArchive);

            if (archive.Entries.Any(entry => entry.FullName.Contains("..", StringComparison.Ordinal) ||
                Path.IsPathRooted(entry.FullName)))
                return Result<MoneyManagerImportData>.Failure(MoneyManagerImportErrors.InvalidArchive);

            var databaseEntries = archive.Entries
                .Where(entry => entry.FullName == "MyFinance.db")
                .ToList();
            if (databaseEntries.Count != 1)
                return Result<MoneyManagerImportData>.Failure(MoneyManagerImportErrors.DatabaseMissing);

            var databaseEntry = databaseEntries[0];
            if (databaseEntry.Length < 16 || databaseEntry.Length > MaximumDatabaseBytes ||
                databaseEntry.CompressedLength > MaximumArchiveBytes)
                return Result<MoneyManagerImportData>.Failure(MoneyManagerImportErrors.BackupTooLarge);

            await using (var source = databaseEntry.Open())
            await using (var destination = File.Create(databasePath))
                await CopyWithLimit(source, destination, MaximumDatabaseBytes, cancellationToken);

            if (!await HasSqliteHeader(databasePath, cancellationToken))
                return Result<MoneyManagerImportData>.Failure(MoneyManagerImportErrors.InvalidDatabase);

            return await ReadDatabase(databasePath, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (BackupLimitException)
        {
            return Result<MoneyManagerImportData>.Failure(MoneyManagerImportErrors.BackupTooLarge);
        }
        catch (MoneyManagerDataException)
        {
            return Result<MoneyManagerImportData>.Failure(MoneyManagerImportErrors.InvalidDatabase);
        }
        catch (InvalidDataException)
        {
            return Result<MoneyManagerImportData>.Failure(MoneyManagerImportErrors.InvalidArchive);
        }
        catch (SqliteException)
        {
            return Result<MoneyManagerImportData>.Failure(MoneyManagerImportErrors.InvalidDatabase);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<MoneyManagerImportData>.Failure(MoneyManagerImportErrors.InvalidBackup);
        }
        finally
        {
            TryDelete(archivePath);
            TryDelete(databasePath);
        }
    }

    private static async Task CopyWithLimit(Stream source, Stream destination, long maximumBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long copied = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            copied = checked(copied + read);
            if (copied > maximumBytes)
                throw new BackupLimitException();

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Cleanup must never replace the import result or cancellation.
        }
    }

    private static async Task<bool> HasSqliteHeader(string path, CancellationToken cancellationToken)
    {
        var expected = "SQLite format 3\0"u8.ToArray();
        var actual = new byte[expected.Length];
        await using var stream = File.OpenRead(path);
        return await stream.ReadAtLeastAsync(actual, actual.Length, false, cancellationToken) == actual.Length &&
            actual.SequenceEqual(expected);
    }

    private static async Task<Result<MoneyManagerImportData>> ReadDatabase(string path,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            }.ToString());
        await connection.OpenAsync(cancellationToken);
        if (!await HasRequiredSchema(connection, cancellationToken))
            return Result<MoneyManagerImportData>.Failure(MoneyManagerImportErrors.UnsupportedSchema);

        var categories = await ReadCategories(connection, cancellationToken);
        var tags = await ReadTags(connection, cancellationToken);
        var links = await ReadLinks(connection, cancellationToken);
        var transactions = await ReadTransactions(connection, cancellationToken);
        var validExpenses = new Dictionary<string, MoneyManagerExpense>(StringComparer.Ordinal);
        var usedCategories = new HashSet<string>(StringComparer.Ordinal);
        var importedTags = new Dictionary<string, MoneyManagerTag>(StringComparer.Ordinal);
        var ignored = transactions.InvalidCount;
        var uncategorized = 0;
        foreach (var transaction in transactions.Items)
        {
            if (!transaction.IsExpense || transaction.IsRemoved || transaction.AmountCents <= 0 ||
                links.IsRecurring(transaction.Uid))
            {
                ignored++;
                continue;
            }

            var categoryUid = links.Category(transaction.Uid);
            if (categoryUid is null || !categories.ContainsKey(categoryUid))
            {
                uncategorized++;
                continue;
            }

            var tagUid = links.Tags(transaction.Uid)
                .Where(tags.ContainsKey)
                .OrderBy(value => value, StringComparer.Ordinal)
                .FirstOrDefault();
            usedCategories.Add(categoryUid);
            if (tagUid is not null)
            {
                var importedTagUid = $"{tagUid}:{categoryUid}";
                importedTags.TryAdd(importedTagUid,
                    new MoneyManagerTag(importedTagUid, categoryUid, tags[tagUid]));
                tagUid = importedTagUid;
            }

            if (!validExpenses.TryAdd(transaction.Uid, new MoneyManagerExpense(transaction.Uid, categoryUid,
                    tagUid, transaction.AmountCents, transaction.Date, transaction.Comment)))
                ignored++;
        }

        var importedCategories = categories.Values
            .Where(value => usedCategories.Contains(value.SourceUid))
            .ToList();
        return Result<MoneyManagerImportData>.Success(new MoneyManagerImportData(
            importedCategories, importedTags.Values.ToList(), validExpenses.Values.ToList(), ignored, uncategorized));
    }

    private static async Task<bool> HasRequiredSchema(SqliteConnection connection, CancellationToken token)
    {
        var required = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["category"] = ["uid", "title", "type", "icon", "color", "isRemoved"],
            ["tag"] = ["uid", "name", "isRemoved"],
            ["transaction"] = ["uid", "type", "amountInDefaultCurrency", "date", "comment", "isRemoved"],
            ["sync_link"] = ["entityType", "entityUid", "otherType", "otherUid", "isRemoved"]
        };
        foreach (var table in required)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info([{table.Key}])";
            await using var reader = await command.ExecuteReaderAsync(token);
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (await reader.ReadAsync(token))
                columns.Add(reader.GetString(1));
            if (table.Value.Any(column => !columns.Contains(column)))
                return false;
        }

        return true;
    }

    private static async Task<Dictionary<string, MoneyManagerCategory>> ReadCategories(
        SqliteConnection connection, CancellationToken token)
    {
        var result = new Dictionary<string, MoneyManagerCategory>(StringComparer.Ordinal);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT uid,title,icon,color FROM category WHERE isRemoved=0 AND type='Expense' LIMIT 100001";
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            EnsureCapacity(result.Count);
            var uid = ReadUid(reader, 0);
            var name = ReadText(reader, 1, 80) ?? DefaultCategoryName(uid);
            if (uid is null || name is null)
                continue;

            var icon = ReadText(reader, 2, 100, true) ?? "";
            var color = ReadScalarText(reader, 3, 20);
            result.TryAdd(uid, new MoneyManagerCategory(uid, name, MapIcon(icon, uid), MapColor(color, uid)));
        }

        return result;
    }

    private static async Task<Dictionary<string, string>> ReadTags(
        SqliteConnection connection, CancellationToken token)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT uid,name FROM tag WHERE isRemoved=0 LIMIT 100001";
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            EnsureCapacity(result.Count);
            var uid = ReadUid(reader, 0);
            var name = ReadText(reader, 1, 80);
            if (uid is not null && name is not null)
                result.TryAdd(uid, name);
        }

        return result;
    }

    private static async Task<LinkIndex> ReadLinks(SqliteConnection connection, CancellationToken token)
    {
        var result = new LinkIndex();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT entityType,entityUid,otherType,otherUid FROM sync_link WHERE isRemoved=0 LIMIT 100001";
        await using var reader = await command.ExecuteReaderAsync(token);
        var count = 0;
        while (await reader.ReadAsync(token))
        {
            EnsureCapacity(count++);
            var entityType = ReadText(reader, 0, 40);
            var entityUid = ReadUid(reader, 1);
            var otherType = ReadText(reader, 2, 40);
            var otherUid = ReadUid(reader, 3);
            if (entityType is not null && entityUid is not null && otherType is not null && otherUid is not null)
                result.Add(entityType, entityUid, otherType, otherUid);
        }

        return result;
    }

    private static async Task<TransactionReadResult> ReadTransactions(
        SqliteConnection connection, CancellationToken token)
    {
        var result = new List<SourceTransaction>();
        var invalid = 0;
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT uid,type,amountInDefaultCurrency,date,comment,isRemoved FROM [transaction] LIMIT 100001";
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            EnsureCapacity(result.Count + invalid);
            var uid = ReadUid(reader, 0);
            var type = ReadText(reader, 1, 40);
            var dateText = ReadText(reader, 3, 10);
            if (uid is null || type is null || dateText is null ||
                !TryReadInt64(reader, 2, out var amount) || !TryReadBoolean(reader, 5, out var isRemoved) ||
                !DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
            {
                invalid++;
                continue;
            }

            var comment = ReadText(reader, 4, 500, true) ?? "";
            result.Add(new SourceTransaction(uid,
                type.Equals("Expense", StringComparison.OrdinalIgnoreCase), amount, date, comment, isRemoved));
        }

        return new TransactionReadResult(result, invalid);
    }

    private static string? ReadUid(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;
        if (reader.GetFieldType(ordinal) != typeof(string))
            throw new MoneyManagerDataException();

        var value = reader.GetString(ordinal).Trim();
        if (value.Length == 0)
            return null;
        if (value.Length > MaximumUidLength)
            throw new MoneyManagerDataException();
        return value;
    }

    private static string? DefaultCategoryName(string? uid) => uid switch
    {
        "DefaultCafe" => "Ristoranti e bar",
        "DefaultEducation" => "Istruzione",
        "DefaultFamily" => "Famiglia",
        "DefaultHealth" => "Salute",
        "DefaultHome" => "Casa",
        "DefaultPresents" => "Regali",
        "DefaultProducts" => "Acquisti",
        "DefaultSport" => "Sport",
        "other_expense" => "Altro",
        _ => null
    };
    private static string? ReadText(SqliteDataReader reader, int ordinal, int maximumLength,
        bool allowNull = false)
    {
        if (reader.IsDBNull(ordinal))
            return null;
        if (reader.GetFieldType(ordinal) != typeof(string))
            throw new MoneyManagerDataException();

        var value = reader.GetString(ordinal).Trim();
        if (value.Length == 0)
            return allowNull ? "" : null;
        return value.Length <= maximumLength ? value : value[..maximumLength];
    }

    private static string ReadScalarText(SqliteDataReader reader, int ordinal, int maximumLength)
    {
        if (reader.IsDBNull(ordinal))
            return "";
        var value = reader.GetValue(ordinal);
        if (value is byte[])
            throw new MoneyManagerDataException();
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        return text.Length <= maximumLength ? text : text[..maximumLength];
    }

    private static bool TryReadInt64(SqliteDataReader reader, int ordinal, out long value)
    {
        value = 0;
        if (reader.IsDBNull(ordinal) || reader.GetValue(ordinal) is not long parsed)
            return false;
        value = parsed;
        return true;
    }

    private static bool TryReadBoolean(SqliteDataReader reader, int ordinal, out bool value)
    {
        value = false;
        if (!TryReadInt64(reader, ordinal, out var parsed) || parsed is < 0 or > 1)
            return false;
        value = parsed == 1;
        return true;
    }

    private static void EnsureCapacity(int count)
    {
        if (count >= MaximumRowsPerTable)
            throw new BackupLimitException();
    }

    private static string MapIcon(string icon, string uid)
    {
        var normalized = icon.ToLowerInvariant();
        if (normalized.Contains("home"))
            return "home";
        if (normalized.Contains("car") || normalized.Contains("transport"))
            return "car";
        if (normalized.Contains("heart") || normalized.Contains("health"))
            return "heart";
        if (normalized.Contains("cart") || normalized.Contains("food"))
            return "cart";
        return Icons[StableIndex(uid, Icons.Length)];
    }

    private static string MapColor(string color, string uid) =>
        Colors.Contains(color, StringComparer.OrdinalIgnoreCase)
            ? Colors.First(value => value.Equals(color, StringComparison.OrdinalIgnoreCase))
            : Colors[StableIndex(uid, Colors.Length)];

    private static int StableIndex(string value, int length) =>
        MoneyManagerImportIds.Create("fallback", value).ToByteArray()[0] % length;

    private sealed record SourceTransaction(string Uid, bool IsExpense, long AmountCents,
        DateOnly Date, string Comment, bool IsRemoved);
    private sealed record TransactionReadResult(IReadOnlyList<SourceTransaction> Items, int InvalidCount);
    private sealed class BackupLimitException : Exception;
    private sealed class MoneyManagerDataException : Exception;

    private sealed class LinkIndex
    {
        private readonly Dictionary<string, string> _categories = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> _tags = new(StringComparer.Ordinal);
        private readonly HashSet<string> _recurring = new(StringComparer.Ordinal);

        public void Add(string entityType, string entityUid, string otherType, string otherUid)
        {
            if (entityType.Equals("Transaction", StringComparison.OrdinalIgnoreCase))
            {
                if (otherType.Equals("Category", StringComparison.OrdinalIgnoreCase))
                    _categories[entityUid] = otherUid;
                else if (otherType.Equals("Tag", StringComparison.OrdinalIgnoreCase))
                {
                    if (!_tags.TryGetValue(entityUid, out var values))
                    {
                        values = [];
                        _tags[entityUid] = values;
                    }

                    if (values.Count < 100)
                        values.Add(otherUid);
                }
                else if (otherType.Contains("recurr", StringComparison.OrdinalIgnoreCase))
                    _recurring.Add(entityUid);
            }

            if (entityType.Equals("RegularPaymentPeriod", StringComparison.OrdinalIgnoreCase) &&
                otherType.Equals("Transaction", StringComparison.OrdinalIgnoreCase))
                _recurring.Add(otherUid);
        }

        public string? Category(string transactionUid) => _categories.GetValueOrDefault(transactionUid);
        public IEnumerable<string> Tags(string transactionUid) => _tags.GetValueOrDefault(transactionUid) ?? [];
        public bool IsRecurring(string transactionUid) => _recurring.Contains(transactionUid);
    }
}
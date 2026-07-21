using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure;

public sealed class MoneyManagerImportRepository(CentesimoDbContext context)
    : RepositoryBase(context), IMoneyManagerImportRepository
{
    private static Money? ToBudget(long? cents) => cents.HasValue ? new Money(cents.Value) : null;

    public Task<Result<MoneyManagerPersisted>> Preview(MoneyManagerImportData data,
        CancellationToken cancellationToken = default) =>
        UseContext(async (db, token) =>
        {
            var existingCategories = await db.Categories
                .AsNoTracking()
                .Select(value => new { value.CategoryId, value.Name })
                .ToListAsync(token);
            var categoryMap = new Dictionary<string, Guid>(StringComparer.Ordinal);
            var categoryIds = existingCategories.Select(value => value.CategoryId).ToHashSet();
            var categoryNames = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var category in existingCategories)
                categoryNames.TryAdd(category.Name, category.CategoryId);

            var categoriesAdded = 0;
            foreach (var source in data.Categories.OrderBy(value => value.SourceUid, StringComparer.Ordinal))
            {
                var deterministicId = MoneyManagerImportIds.Create("category", source.SourceUid);
                if (!categoryIds.Contains(deterministicId) && !categoryNames.ContainsKey(source.Name))
                {
                    categoryIds.Add(deterministicId);
                    categoryNames[source.Name] = deterministicId;
                    categoriesAdded++;
                }

                categoryMap[source.SourceUid] = categoryIds.Contains(deterministicId)
                    ? deterministicId
                    : categoryNames[source.Name];
            }

            var existingTags = await db.Tags
                .AsNoTracking()
                .Select(value => new { value.TagId, value.CategoryId, value.Name })
                .ToListAsync(token);
            var tagIds = existingTags.Select(value => value.TagId).ToHashSet();
            var tagKeys = new Dictionary<(Guid CategoryId, string Name), Guid>();
            foreach (var tag in existingTags)
                tagKeys.TryAdd((tag.CategoryId, tag.Name.ToUpperInvariant()), tag.TagId);

            var tagsAdded = 0;
            foreach (var source in data.Tags.OrderBy(value => value.SourceUid, StringComparer.Ordinal))
            {
                if (!categoryMap.TryGetValue(source.CategorySourceUid, out var categoryId))
                    continue;

                var deterministicId = MoneyManagerImportIds.Create("tag", source.SourceUid);
                var key = (categoryId, source.Name.ToUpperInvariant());
                if (!tagIds.Contains(deterministicId) && !tagKeys.ContainsKey(key))
                {
                    tagIds.Add(deterministicId);
                    tagKeys[key] = deterministicId;
                    tagsAdded++;
                }

            }

            var sourceExpenseIds = data.Expenses
                .Select(value => MoneyManagerImportIds.Create("expense", value.SourceUid))
                .ToList();
            var existingExpenseIds = await db.Expenses
                .AsNoTracking()
                .Where(value => sourceExpenseIds.Contains(value.ExpenseId))
                .Select(value => value.ExpenseId)
                .ToHashSetAsync(token);
            var expensesAdded = 0;
            foreach (var source in data.Expenses.OrderBy(value => value.SourceUid, StringComparer.Ordinal))
            {
                var expenseId = MoneyManagerImportIds.Create("expense", source.SourceUid);
                if (existingExpenseIds.Contains(expenseId) ||
                    !categoryMap.ContainsKey(source.CategorySourceUid))
                    continue;

                existingExpenseIds.Add(expenseId);
                expensesAdded++;
            }

            var recurringIds = data.RecurringPaymentsOrEmpty
                .Select(value => MoneyManagerImportIds.Create("recurring-payment", value.SourceUid))
                .ToList();
            var existingRecurringIds = await db.RecurringPayments.AsNoTracking()
                .Where(value => recurringIds.Contains(value.RecurringPaymentId))
                .Select(value => value.RecurringPaymentId).ToHashSetAsync(token);
            var recurringAdded = data.RecurringPaymentsOrEmpty.Count(source =>
                categoryMap.ContainsKey(source.CategorySourceUid) &&
                !existingRecurringIds.Contains(MoneyManagerImportIds.Create("recurring-payment", source.SourceUid)));
            return new MoneyManagerPersisted(categoriesAdded, tagsAdded, expensesAdded, recurringAdded);
        }, cancellationToken);

    public Task<Result<MoneyManagerPersisted>> Import(MoneyManagerImportData data,
        CancellationToken cancellationToken = default) =>
        SaveContextInTransaction(async (db, token) =>
        {
            var existingCategories = await db.Categories.ToListAsync(token);
            var categoryMap = new Dictionary<string, Guid>(StringComparer.Ordinal);
            var categoriesAdded = 0;
            foreach (var source in data.Categories.OrderBy(value => value.SourceUid, StringComparer.Ordinal))
            {
                var deterministicId = MoneyManagerImportIds.Create("category", source.SourceUid);
                var category = existingCategories.FirstOrDefault(value => value.CategoryId == deterministicId) ??
                    existingCategories.FirstOrDefault(value =>
                        value.Name.Equals(source.Name, StringComparison.OrdinalIgnoreCase));
                if (category is null)
                {
                    category = new Category(deterministicId, source.Name, source.Icon, source.Color,
                        ToBudget(source.MonthlyBudgetCents));
                    db.Categories.Add(category);
                    existingCategories.Add(category);
                    categoriesAdded++;
                }

                if (category.MonthlyBudget is null)
                    category.SetBudget(ToBudget(source.MonthlyBudgetCents));

                category.Restore();
                categoryMap[source.SourceUid] = category.CategoryId;
            }

            var existingTags = await db.Tags.ToListAsync(token);
            var tagMap = new Dictionary<string, Guid>(StringComparer.Ordinal);
            var tagsAdded = 0;
            foreach (var source in data.Tags.OrderBy(value => value.SourceUid, StringComparer.Ordinal))
            {
                if (!categoryMap.TryGetValue(source.CategorySourceUid, out var categoryId))
                    continue;

                var deterministicId = MoneyManagerImportIds.Create("tag", source.SourceUid);
                var tag = existingTags.FirstOrDefault(value => value.TagId == deterministicId) ??
                    existingTags.FirstOrDefault(value => value.CategoryId == categoryId &&
                        value.Name.Equals(source.Name, StringComparison.OrdinalIgnoreCase));
                if (tag is null)
                {
                    tag = new Tag(deterministicId, categoryId, source.Name);
                    db.Tags.Add(tag);
                    existingTags.Add(tag);
                    tagsAdded++;
                }

                tag.Restore();
                tagMap[source.SourceUid] = tag.TagId;
            }

            var expenseIds = data.Expenses
                .Select(value => MoneyManagerImportIds.Create("expense", value.SourceUid))
                .ToList();
            var existingExpenseIds = await db.Expenses
                .Where(value => expenseIds.Contains(value.ExpenseId))
                .Select(value => value.ExpenseId)
                .ToHashSetAsync(token);
            var expensesAdded = 0;
            foreach (var source in data.Expenses.OrderBy(value => value.SourceUid, StringComparer.Ordinal))
            {
                var expenseId = MoneyManagerImportIds.Create("expense", source.SourceUid);
                if (existingExpenseIds.Contains(expenseId) ||
                    !categoryMap.TryGetValue(source.CategorySourceUid, out var categoryId))
                    continue;

                Guid? tagId = null;
                if (source.TagSourceUid is not null && tagMap.TryGetValue(source.TagSourceUid, out var mappedTagId))
                    tagId = mappedTagId;

                db.Expenses.Add(new Expense(expenseId, categoryId, new Money(source.AmountCents),
                    source.OccurredOn, tagId, source.Note));
                existingExpenseIds.Add(expenseId);
                expensesAdded++;
            }

            var recurringIds = data.RecurringPaymentsOrEmpty
                .Select(value => MoneyManagerImportIds.Create("recurring-payment", value.SourceUid))
                .ToList();
            var existingRecurringIds = await db.RecurringPayments
                .Where(value => recurringIds.Contains(value.RecurringPaymentId))
                .Select(value => value.RecurringPaymentId).ToHashSetAsync(token);
            var recurringAdded = 0;
            foreach (var source in data.RecurringPaymentsOrEmpty.OrderBy(value => value.SourceUid, StringComparer.Ordinal))
            {
                var recurringPaymentId = MoneyManagerImportIds.Create("recurring-payment", source.SourceUid);
                if (existingRecurringIds.Contains(recurringPaymentId) ||
                    !categoryMap.TryGetValue(source.CategorySourceUid, out var categoryId))
                    continue;

                Guid? tagId = null;
                if (source.TagSourceUid is not null && tagMap.TryGetValue(source.TagSourceUid, out var mappedTagId))
                    tagId = mappedTagId;
                db.RecurringPayments.Add(new RecurringPayment(recurringPaymentId, categoryId,
                    new Money(source.AmountCents), source.Frequency, source.NextDueOn, tagId,
                    source.Note, source.EndsOn));
                existingRecurringIds.Add(recurringPaymentId);
                recurringAdded++;
            }
            return new MoneyManagerPersisted(categoriesAdded, tagsAdded, expensesAdded, recurringAdded);
        }, cancellationToken, true);
}

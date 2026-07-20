using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure;

public sealed class MoneyManagerImportRepository(CentesimoDbContext context)
    : RepositoryBase(context), IMoneyManagerImportRepository
{
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
                    category = new Category(deterministicId, source.Name, source.Icon, source.Color);
                    db.Categories.Add(category);
                    existingCategories.Add(category);
                    categoriesAdded++;
                }

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

            return new MoneyManagerPersisted(categoriesAdded, tagsAdded, expensesAdded);
        }, cancellationToken);
}
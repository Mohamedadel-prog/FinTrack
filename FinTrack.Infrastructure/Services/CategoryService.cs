
using FinTrack.Application.Categories.DTOs;
using FinTrack.Application.Categories.Interfaces;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(
        string userId,
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = new Category(
            userId,
            request.Name,
            request.Type);

        var exists = await _context.Categories
            .AnyAsync(c =>
                c.UserId == userId &&
                c.Type == category.Type &&
                c.Name == category.Name &&
                !c.IsArchived,
                cancellationToken);

        if (exists)
            throw new InvalidOperationException(
                "A category with this name and type already exists.");

        _context.Categories.Add(category);

        await SaveChangesAsync(cancellationToken);

        return category.Id;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(c =>
                c.UserId == userId &&
                !c.IsArchived)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Type))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto?> GetByIdAsync(
        string userId,
        int categoryId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(c =>
                c.Id == categoryId &&
                c.UserId == userId &&
                !c.IsArchived)
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Type))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> RenameAsync(
        string userId,
        int categoryId,
        string newName,
        CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == categoryId &&
                c.UserId == userId &&
                !c.IsArchived,
                cancellationToken);

        if (category is null)
            return false;

        var normalizedName = newName?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
            throw new ArgumentException(
                "Category name is required.", nameof(newName));

        var exists = await _context.Categories
            .AnyAsync(c =>
                c.Id != categoryId &&
                c.UserId == userId &&
                c.Type == category.Type &&
                c.Name == normalizedName &&
                !c.IsArchived,
                cancellationToken);

        if (exists)
            throw new InvalidOperationException(
                "A category with this name and type already exists.");

        category.Rename(normalizedName);

        await SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ArchiveAsync(
        string userId,
        int categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == categoryId &&
                c.UserId == userId &&
                !c.IsArchived,
                cancellationToken);

        if (category is null)
            return false;

        category.Archive();

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    // The pre-check above can race with a concurrent request; the filtered
    // unique index is the real guard, so map its violation to the same error.
    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            _context.ChangeTracker.Clear();

            throw new InvalidOperationException(
                "A category with this name and type already exists.", ex);
        }
    }
}

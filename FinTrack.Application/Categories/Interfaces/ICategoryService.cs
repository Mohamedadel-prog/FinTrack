
using FinTrack.Application.Categories.DTOs;

namespace FinTrack.Application.Categories.Interfaces;

public interface ICategoryService
{
    Task<int> CreateAsync(
        string userId,
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<CategoryDto?> GetByIdAsync(
        string userId,
        int categoryId,
        CancellationToken cancellationToken = default);

    Task<bool> RenameAsync(
        string userId,
        int categoryId,
        string newName,
        CancellationToken cancellationToken = default);

    Task<bool> ArchiveAsync(
        string userId,
        int categoryId,
        CancellationToken cancellationToken = default);
}


using FinTrack.Domain.Enums;

namespace FinTrack.Application.Categories.DTOs;

public sealed record CreateCategoryRequest(
    string Name,
    CategoryType Type
);

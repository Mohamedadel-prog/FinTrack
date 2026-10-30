
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Categories.DTOs;

public sealed record CategoryDto(
    int Id,
    string Name,
    CategoryType Type
);

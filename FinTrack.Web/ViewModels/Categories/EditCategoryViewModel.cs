
using System.ComponentModel.DataAnnotations;
using FinTrack.Domain.Enums;

namespace FinTrack.Web.ViewModels.Categories;

public class EditCategoryViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public CategoryType Type { get; set; }
}

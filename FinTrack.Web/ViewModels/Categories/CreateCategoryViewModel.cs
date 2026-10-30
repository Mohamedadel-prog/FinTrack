
using System.ComponentModel.DataAnnotations;
using FinTrack.Domain.Enums;

namespace FinTrack.Web.ViewModels.Categories;

public class CreateCategoryViewModel
{
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(CategoryType))]
    public CategoryType Type { get; set; } = CategoryType.Expense;
}

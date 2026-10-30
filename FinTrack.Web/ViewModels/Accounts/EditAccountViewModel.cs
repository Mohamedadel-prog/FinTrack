
using System.ComponentModel.DataAnnotations;
using FinTrack.Domain.Enums;

namespace FinTrack.Web.ViewModels.Accounts;

public class EditAccountViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(AccountType))]
    public AccountType Type { get; set; }
}

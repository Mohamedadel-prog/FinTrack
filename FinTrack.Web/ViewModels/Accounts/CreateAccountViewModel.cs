
using System.ComponentModel.DataAnnotations;
using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;

namespace FinTrack.Web.ViewModels.Accounts;

public class CreateAccountViewModel
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(AccountType))]
    public AccountType Type { get; set; } = AccountType.Cash;

    [Required]
    [Range(
        typeof(decimal),
        "-9999999999999999.99",
        "9999999999999999.99")]
    [Display(Name = "Opening Balance")]
    public decimal OpeningBalance { get; set; }

    // Rendered as a <select>; the domain rejects unsupported codes.
    [Required]
    public string Currency { get; set; } = Currencies.Default;
}

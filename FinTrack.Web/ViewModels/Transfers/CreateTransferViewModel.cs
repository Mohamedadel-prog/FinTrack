
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinTrack.Web.ViewModels.Transfers;

public class CreateTransferViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Select the account to move money from.")]
    [Display(Name = "From account")]
    public int FromAccountId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select the account to move money to.")]
    [Display(Name = "To account")]
    public int ToAccountId { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999999.99",
        ErrorMessage = "Enter a valid positive amount.")]
    public decimal Amount { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime TransferDate { get; set; } = DateTime.Today;

    [StringLength(500)]
    public string? Note { get; set; }

    public List<SelectListItem> Accounts { get; set; } = [];

    /// <summary>Account id → currency code, used to label the amount field.</summary>
    public Dictionary<int, string> AccountCurrencies { get; set; } = [];
}

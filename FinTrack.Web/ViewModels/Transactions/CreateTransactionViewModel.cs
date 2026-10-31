
using System.ComponentModel.DataAnnotations;
using FinTrack.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinTrack.Web.ViewModels.Transactions;

public class CreateTransactionViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Select an account.")]
    [Display(Name = "Account")]
    public int AccountId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999999.99",
        ErrorMessage = "Enter a valid positive amount.")]
    public decimal Amount { get; set; }

    [EnumDataType(typeof(TransactionType))]
    public TransactionType Type { get; set; } =
        TransactionType.Expense;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Transaction Date")]
    public DateTime TransactionDate { get; set; } =
        DateTime.Today;

    [StringLength(500)]
    public string? Note { get; set; }

    public List<SelectListItem> Accounts { get; set; } = [];

    public List<SelectListItem> Categories { get; set; } = [];

    /// <summary>Account id → currency code, used to label the amount field.</summary>
    public Dictionary<int, string> AccountCurrencies { get; set; } = [];
}

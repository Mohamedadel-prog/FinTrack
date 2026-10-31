
using System.ComponentModel.DataAnnotations;
using FinTrack.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinTrack.Web.ViewModels.Transactions;

public class EditTransactionViewModel
{
    public long Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a category.")]
    public int CategoryId { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999999.99",
        ErrorMessage = "Enter a valid positive amount.")]
    public decimal Amount { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime TransactionDate { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;

    public TransactionType Type { get; set; }

    public List<SelectListItem> Categories { get; set; } = [];
}

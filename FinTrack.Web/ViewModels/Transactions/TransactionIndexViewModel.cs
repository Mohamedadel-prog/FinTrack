
using FinTrack.Application.Transactions.DTOs;
using FinTrack.Domain.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinTrack.Web.ViewModels.Transactions;

/// <summary>
/// Filter values come from the query string, so a filtered page
/// can be bookmarked or shared and paging keeps the filters.
/// </summary>
public class TransactionIndexViewModel
{
    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public int? AccountId { get; set; }

    public int? CategoryId { get; set; }

    public TransactionType? Type { get; set; }

    public string? Search { get; set; }

    public int Page { get; set; } = 1;

    public bool HasFilters =>
        From.HasValue ||
        To.HasValue ||
        AccountId.HasValue ||
        CategoryId.HasValue ||
        Type.HasValue ||
        !string.IsNullOrWhiteSpace(Search);

    [BindNever]
    public TransactionListResult Result { get; set; } = null!;

    [BindNever]
    public List<SelectListItem> Accounts { get; set; } = [];

    [BindNever]
    public List<SelectListItem> Categories { get; set; } = [];
}

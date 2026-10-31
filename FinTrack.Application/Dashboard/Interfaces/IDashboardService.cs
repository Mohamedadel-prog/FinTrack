
using FinTrack.Application.Dashboard.DTOs;

namespace FinTrack.Application.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(
        string userId,
        DateTime today,
        string? currency = null,
        CancellationToken cancellationToken = default);
}

using Alza_delivery.DTOs.V1.Requests;
using Alza_delivery.DTOs.V1.Responses;

namespace Alza_delivery.Services.Contracts
{
    public interface IPlanningService
    {
        Task<PlanningResponse> PlanAsync(
            PlanningRequest request,
            CancellationToken cancellationToken = default);
    }
}

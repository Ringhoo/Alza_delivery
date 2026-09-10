using Alza_delivery.DTOs.V1.Requests;
using Alza_delivery.DTOs.V1.Responses;
using Alza_delivery.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Alza_delivery.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PlanningController : ControllerBase
    {
        private readonly IPlanningService _planningService;

        public PlanningController(IPlanningService planningService)
        {
            _planningService = planningService;
        }

        [HttpPost]
        public async Task<ActionResult<PlanningResponse>> PlanAsync(
            PlanningRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _planningService.PlanAsync(request, cancellationToken);

            return Ok(response);
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Alza_delivery.DTOs.V1.Requests
{
    public class PlanningRequest
    {
        public DateTime PlanningDate { get; set; } = DateTime.UtcNow;

        public List<long> WarehouseIds { get; set; } = [];
    }
}

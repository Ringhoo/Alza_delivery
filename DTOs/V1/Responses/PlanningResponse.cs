namespace Alza_delivery.DTOs.V1.Responses
{
    public class PlanningResponse
    {
        public DateTime PlanningDate { get; set; }

        public List<WarehousePlanningResponse> WarehousePlans { get; set; } = [];
    }

    public class WarehousePlanningResponse
    {
        public long WarehouseId { get; set; }

        public List<long> PrioritizedPackageIds { get; set; } = [];

        public List<long> UnassignedPackageIds { get; set; } = [];

        public List<VehiclePlanningResponse> VehiclePlans { get; set; } = [];
    }

    public class VehiclePlanningResponse
    {
        public long VehicleId { get; set; }

        public List<long> PackageIds { get; set; } = [];

        public decimal TotalWeightKg { get; set; }

        public decimal TotalVolumeM3 { get; set; }

        public decimal TotalProfit { get; set; }
    }
}

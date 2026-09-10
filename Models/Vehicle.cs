using Alza_delivery.Enums;

namespace Alza_delivery.Models
{
    public class Vehicle
    {
        public const decimal MaxWeightKg = 5500;

        public const decimal MaxVolumeM3 = 7;

        public long Id { get; set; }

        public VehicleStatus Status { get; set; } = VehicleStatus.Ready;

        public long WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;
    }
}

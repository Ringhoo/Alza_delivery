namespace Alza_delivery.Models
{
    public class Package
    {
        public long Id { get; set; }

        public decimal WeightKg { get; set; }

        public decimal VolumeM3 { get; set; }

        public decimal Profit { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public long WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;
    }
}

namespace Alza_delivery.Models
{
    public class Warehouse
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<Package> Packages { get; set; } = [];

        public List<Vehicle> Vehicles { get; set; } = [];
    }
}

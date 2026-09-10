using Alza_delivery.Databases.Contexts;
using Alza_delivery.Enums;
using Alza_delivery.Factories;
using Alza_delivery.Models;

namespace Alza_delivery.Seeders
{
    public class VehicleSeeder
    {
        private readonly AppDbContext _dbContext;

        public VehicleSeeder(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SeedAsync(
            IReadOnlyList<Warehouse> warehouses,
            int vehiclesPerWarehouse,
            CancellationToken cancellationToken = default)
        {
            if (vehiclesPerWarehouse <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(vehiclesPerWarehouse));
            }

            var vehicles = warehouses.SelectMany(warehouse =>
                VehicleFactory
                    .ForWarehouse(warehouse.Id)
                    .WithStatus(VehicleStatus.Ready)
                    .Count(vehiclesPerWarehouse)
                    .Create());

            await _dbContext.Vehicles.AddRangeAsync(vehicles, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

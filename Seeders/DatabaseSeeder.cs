using Alza_delivery.Databases.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Alza_delivery.Seeders
{
    public class DatabaseSeeder
    {
        private const int WarehouseCount = 1;
        private const int VehiclesPerWarehouse = 120;
        private const int PackagesPerWarehouse = 1_000;

        private readonly AppDbContext _dbContext;
        private readonly WarehouseSeeder _warehouseSeeder;
        private readonly VehicleSeeder _vehicleSeeder;
        private readonly PackageSeeder _packageSeeder;

        public DatabaseSeeder(
            AppDbContext dbContext,
            WarehouseSeeder warehouseSeeder,
            VehicleSeeder vehicleSeeder,
            PackageSeeder packageSeeder)
        {
            _dbContext = dbContext;
            _warehouseSeeder = warehouseSeeder;
            _vehicleSeeder = vehicleSeeder;
            _packageSeeder = packageSeeder;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            //"duplication seed check" - existuje uz nejaky warehouse?
            if (await _dbContext.Warehouses.AnyAsync(cancellationToken))
            {
                return;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var warehouses = await _warehouseSeeder.SeedAsync(
                WarehouseCount,
                cancellationToken);
            await _vehicleSeeder.SeedAsync(
                warehouses,
                VehiclesPerWarehouse,
                cancellationToken);
            await _packageSeeder.SeedAsync(
                warehouses,
                PackagesPerWarehouse,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }
}

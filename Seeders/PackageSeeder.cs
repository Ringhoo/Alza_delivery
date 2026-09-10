using Alza_delivery.Databases.Contexts;
using Alza_delivery.Factories;
using Alza_delivery.Models;
using Microsoft.EntityFrameworkCore;

namespace Alza_delivery.Seeders
{
    public class PackageSeeder
    {
        private readonly AppDbContext _dbContext;

        public PackageSeeder(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SeedAsync(
            IReadOnlyList<Warehouse> warehouses,
            int packagesPerWarehouse,
            CancellationToken cancellationToken = default)
        {
            if (packagesPerWarehouse <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(packagesPerWarehouse));
            }

            var packages = warehouses.SelectMany(warehouse =>
                PackageFactory
                    .ForWarehouse(warehouse.Id)
                    .Count(packagesPerWarehouse)
                    .Create());

            await _dbContext.Packages.AddRangeAsync(packages, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

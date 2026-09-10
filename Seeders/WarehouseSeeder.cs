using Alza_delivery.Databases.Contexts;
using Alza_delivery.Models;
using Microsoft.EntityFrameworkCore;

namespace Alza_delivery.Seeders
{
    public class WarehouseSeeder
    {
        private readonly AppDbContext _dbContext;

        public WarehouseSeeder(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Warehouse>> SeedAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var warehouses = Enumerable.Range(1, count)
                .Select(index => new Warehouse { Name = $"Test warehouse {index}" })
                .ToList();

            await _dbContext.Warehouses.AddRangeAsync(warehouses, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return warehouses;
        }
    }
}

using Alza_delivery.Models;
using Microsoft.EntityFrameworkCore;

namespace Alza_delivery.Databases.Contexts
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Warehouse> Warehouses => Set<Warehouse>();

        public DbSet<Package> Packages => Set<Package>();

        public DbSet<Vehicle> Vehicles => Set<Vehicle>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}

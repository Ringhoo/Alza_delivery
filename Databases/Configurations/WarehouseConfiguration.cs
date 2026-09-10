using Alza_delivery.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alza_delivery.Databases.Configurations
{
    public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.Property(warehouse => warehouse.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.HasMany(warehouse => warehouse.Packages)
                .WithOne(package => package.Warehouse)
                .HasForeignKey(package => package.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(warehouse => warehouse.Vehicles)
                .WithOne(vehicle => vehicle.Warehouse)
                .HasForeignKey(vehicle => vehicle.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

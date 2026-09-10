using Alza_delivery.Enums;
using Alza_delivery.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alza_delivery.Databases.Configurations
{
    public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.Property(vehicle => vehicle.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(VehicleStatus.Ready);
            builder.HasIndex(vehicle => vehicle.WarehouseId);
        }
    }
}

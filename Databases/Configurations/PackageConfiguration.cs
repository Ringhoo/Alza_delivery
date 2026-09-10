using Alza_delivery.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alza_delivery.Databases.Configurations
{
    public class PackageConfiguration : IEntityTypeConfiguration<Package>
    {
        public void Configure(EntityTypeBuilder<Package> builder)
        {
            builder.Property(package => package.WeightKg)
                .HasPrecision(18, 3);
            builder.Property(package => package.VolumeM3)
                .HasPrecision(18, 6);
            builder.Property(package => package.Profit)
                .HasPrecision(18, 2);
           
            
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Packages_WeightKg_Positive", "[WeightKg] > 0");
                table.HasCheckConstraint("CK_Packages_VolumeM3_Positive", "[VolumeM3] > 0");
            });

            builder.HasIndex(package => new { package.WarehouseId, package.CreatedAt });
        }
    }
}

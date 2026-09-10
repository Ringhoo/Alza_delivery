using Alza_delivery.Factories;
using Alza_delivery.Models;
using Xunit;

namespace Alza_delivery.Tests.Factories;

public class PackageFactoryTests
{
    [Fact]
    public void Create_WithRandomValues_CreatesPackagesWithinVehicleCapacity()
    {
        var packages = PackageFactory
            .ForWarehouse(10)
            .Count(25)
            .Create();

        Assert.Equal(25, packages.Count);
        Assert.All(packages, package =>
        {
            Assert.Equal(10, package.WarehouseId);
            Assert.InRange(package.WeightKg, 0.001m, Vehicle.MaxWeightKg);
            Assert.InRange(package.VolumeM3, 0.000001m, Vehicle.MaxVolumeM3);
            Assert.True(package.Profit > 0);
        });
    }

    [Fact]
    public void Create_WithConfiguredValues_AppliesValuesToEveryPackage()
    {
        var createdAt = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

        var packages = PackageFactory
            .ForWarehouse(10)
            .WithWeightKg(12.345m)
            .WithVolumeM3(0.123456m)
            .WithProfit(99.99m)
            .WithCreatedAt(createdAt)
            .Count(2)
            .Create();

        Assert.All(packages, package =>
        {
            Assert.Equal(12.345m, package.WeightKg);
            Assert.Equal(0.123456m, package.VolumeM3);
            Assert.Equal(99.99m, package.Profit);
            Assert.Equal(createdAt, package.CreatedAt);
        });
    }

    [Fact]
    public void Create_WithoutWarehouse_ThrowsException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            PackageFactory.ForWarehouse(0).Create());

        Assert.Equal("Warehouse must be defined before creating packages.", exception.Message);
    }
}

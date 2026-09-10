using Alza_delivery.Enums;
using Alza_delivery.Factories;
using Xunit;

namespace Alza_delivery.Tests.Factories;

public class VehicleFactoryTests
{
    [Fact]
    public void Create_WithConfiguredValues_CreatesRequestedVehicles()
    {
        var vehicles = VehicleFactory
            .ForWarehouse(10)
            .WithStatus(VehicleStatus.Maintenance)
            .Count(3)
            .Create();

        Assert.Equal(3, vehicles.Count);
        Assert.All(vehicles, vehicle =>
        {
            Assert.Equal(10, vehicle.WarehouseId);
            Assert.Equal(VehicleStatus.Maintenance, vehicle.Status);
        });
    }

    [Fact]
    public void Count_WithNonPositiveValue_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            VehicleFactory.ForWarehouse(10).Count(0));
    }

    [Fact]
    public void Create_WithoutWarehouse_ThrowsException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            VehicleFactory.ForWarehouse(0).Create());

        Assert.Equal("Warehouse must be defined before creating vehicles.", exception.Message);
    }
}

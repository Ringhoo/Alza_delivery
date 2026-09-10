using Alza_delivery.Databases.Contexts;
using Alza_delivery.DTOs.V1.Requests;
using Alza_delivery.DTOs.V1.Responses;
using Alza_delivery.Enums;
using Alza_delivery.Models;
using Alza_delivery.Services.Contracts;
using Alza_delivery.Services.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Alza_delivery.Tests.Services.Planning;

public class PlanningServiceTests
{
    [Fact]
    public async Task PlanAsync_AssignsPackagesWithoutExceedingVehicleCapacity()
    {
        await using var provider = CreateServiceProvider();
        var planningDate = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
        var warehouseId = await SeedWarehouseAsync(
            provider,
            "Prague",
            [new Vehicle()],
            [
                new Package { WeightKg = 4_000, VolumeM3 = 3, Profit = 300, CreatedAt = planningDate },
                new Package { WeightKg = 1_500, VolumeM3 = 4, Profit = 200, CreatedAt = planningDate },
                new Package { WeightKg = 100, VolumeM3 = 1, Profit = 100, CreatedAt = planningDate }
            ]);

        var response = await PlanAsync(provider, new PlanningRequest
        {
            PlanningDate = planningDate,
            WarehouseIds = [warehouseId]
        });

        var warehousePlan = Assert.Single(response.WarehousePlans);
        var vehiclePlan = Assert.Single(warehousePlan.VehiclePlans);
        var plannedPackageIds = vehiclePlan.PackageIds;

        Assert.InRange(vehiclePlan.TotalWeightKg, 0, Vehicle.MaxWeightKg);
        Assert.InRange(vehiclePlan.TotalVolumeM3, 0, Vehicle.MaxVolumeM3);
        Assert.Equal(3, plannedPackageIds.Count + warehousePlan.UnassignedPackageIds.Count);
        Assert.Equal(
            plannedPackageIds.Count + warehousePlan.UnassignedPackageIds.Count,
            plannedPackageIds.Concat(warehousePlan.UnassignedPackageIds).Distinct().Count());
    }

    [Fact]
    public async Task PlanAsync_UsesOnlyReadyVehiclesAndPackagesCreatedByPlanningDate()
    {
        await using var provider = CreateServiceProvider();
        var planningDate = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
        var warehouseId = await SeedWarehouseAsync(
            provider,
            "Brno",
            [
                new Vehicle { Status = VehicleStatus.Ready },
                new Vehicle { Status = VehicleStatus.Maintenance }
            ],
            [
                new Package { WeightKg = 100, VolumeM3 = 1, Profit = 100, CreatedAt = planningDate },
                new Package { WeightKg = 100, VolumeM3 = 1, Profit = 100, CreatedAt = planningDate.AddTicks(1) }
            ]);

        var response = await PlanAsync(provider, new PlanningRequest
        {
            PlanningDate = planningDate,
            WarehouseIds = [warehouseId]
        });

        var warehousePlan = Assert.Single(response.WarehousePlans);

        Assert.Single(warehousePlan.VehiclePlans);
        Assert.Single(warehousePlan.PrioritizedPackageIds);
        Assert.Single(warehousePlan.VehiclePlans.Single().PackageIds);
    }

    [Fact]
    public async Task PlanAsync_OrdersPackagesByPriority()
    {
        await using var provider = CreateServiceProvider();
        var planningDate = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
        var warehouseId = await SeedWarehouseAsync(
            provider,
            "Ostrava",
            [new Vehicle()],
            [
                new Package { WeightKg = 100, VolumeM3 = 1, Profit = 10, CreatedAt = planningDate },
                new Package { WeightKg = 100, VolumeM3 = 1, Profit = 20, CreatedAt = planningDate }
            ]);

        var response = await PlanAsync(provider, new PlanningRequest
        {
            PlanningDate = planningDate,
            WarehouseIds = [warehouseId]
        });

        var prioritizedPackageIds = Assert.Single(response.WarehousePlans).PrioritizedPackageIds;

        Assert.Equal([2L, 1L], prioritizedPackageIds);
    }

    [Fact]
    public async Task PlanAsync_UsesOnlyRequestedWarehouses()
    {
        await using var provider = CreateServiceProvider();
        var planningDate = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
        var requestedWarehouseId = await SeedWarehouseAsync(
            provider,
            "Plzeň",
            [new Vehicle()],
            [new Package { WeightKg = 100, VolumeM3 = 1, Profit = 100, CreatedAt = planningDate }]);
        await SeedWarehouseAsync(
            provider,
            "Liberec",
            [new Vehicle()],
            [new Package { WeightKg = 100, VolumeM3 = 1, Profit = 100, CreatedAt = planningDate }]);

        var response = await PlanAsync(provider, new PlanningRequest
        {
            PlanningDate = planningDate,
            WarehouseIds = [requestedWarehouseId]
        });

        var warehousePlan = Assert.Single(response.WarehousePlans);

        Assert.Equal(requestedWarehouseId, warehousePlan.WarehouseId);
    }

    [Fact]
    public async Task PlanAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        await using var provider = CreateServiceProvider();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await using var scope = provider.CreateAsyncScope();
        var planningService = scope.ServiceProvider.GetRequiredService<IPlanningService>();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            planningService.PlanAsync(new PlanningRequest(), cancellationTokenSource.Token));
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IPlanningService, PlanningService>();

        return services.BuildServiceProvider();
    }

    private static async Task<long> SeedWarehouseAsync(
        IServiceProvider provider,
        string warehouseName,
        List<Vehicle> vehicles,
        List<Package> packages)
    {
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var warehouse = new Warehouse { Name = warehouseName };
        dbContext.Warehouses.Add(warehouse);
        await dbContext.SaveChangesAsync();

        foreach (var vehicle in vehicles)
        {
            vehicle.WarehouseId = warehouse.Id;
        }

        foreach (var package in packages)
        {
            package.WarehouseId = warehouse.Id;
        }

        dbContext.AddRange(vehicles);
        dbContext.AddRange(packages);
        await dbContext.SaveChangesAsync();

        return warehouse.Id;
    }

    private static async Task<PlanningResponse> PlanAsync(
        IServiceProvider provider,
        PlanningRequest request)
    {
        await using var scope = provider.CreateAsyncScope();
        var planningService = scope.ServiceProvider.GetRequiredService<IPlanningService>();

        return await planningService.PlanAsync(request);
    }
}

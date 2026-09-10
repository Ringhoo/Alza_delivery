using Alza_delivery.Databases.Contexts;
using Alza_delivery.DTOs.V1.Requests;
using Alza_delivery.DTOs.V1.Responses;
using Alza_delivery.Models;
using Alza_delivery.Enums;
using Alza_delivery.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Alza_delivery.Services.Planning
{
    public class PlanningService : IPlanningService
    {
        
        private readonly record struct PrioritizedPackage(Package Package, decimal Priority);
        private readonly record struct FleetCapacity(decimal WeightKg, decimal VolumeM3);
        private readonly record struct PackageDemandDimensions(decimal WeightKg, decimal VolumeM3);

        private readonly IServiceScopeFactory _serviceScopeFactory;

        public PlanningService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<PlanningResponse> PlanAsync(
            PlanningRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var warehouseIds = await GetWarehouseIdsAsync(request.WarehouseIds, cancellationToken);

            //každý sklad se plánuje na samostatném vlákně
            //pokud je to nežádoucí, stačí zavolat EP několikrát se sólo skladem ID
            var warehousePlanningTasks = warehouseIds.Select(warehouseId =>
                Task.Run(
                    () => PlanWarehouseAsync(warehouseId, request.PlanningDate, cancellationToken),
                    cancellationToken));

            //čeká se až doběhnou všechny sklady, abych mohl vrátit response
            var warehousePlans = await Task.WhenAll(warehousePlanningTasks);


            return new PlanningResponse
            {
                PlanningDate = request.PlanningDate,
                WarehousePlans = warehousePlans.ToList()
            };
        }

        /**
         * Vrátí validní (ale není zaručená jeho existence v db) seznam skladů - buď z neprázdného listu a nebo všechny z db.
         */
        private async Task<List<long>> GetWarehouseIdsAsync(
            List<long> requestedWarehouseIds,
            CancellationToken cancellationToken)
        {
            //vem unikátní hodnoty z pole
            if (requestedWarehouseIds.Count > 0)
            {
                if (requestedWarehouseIds.Any(warehouseId => warehouseId <= 0))
                {
                    throw new ArgumentOutOfRangeException(nameof(requestedWarehouseIds));
                }

                return requestedWarehouseIds.Distinct().ToList();
            }

            //pokud není definován žádný, tak vem všechny sklady

            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await dbContext.Warehouses
                .AsNoTracking()
                .Select(warehouse => warehouse.Id)
                .ToListAsync(cancellationToken);
        }

        private async Task<WarehousePlanningResponse> PlanWarehouseAsync(
            long warehouseId,
            DateTime planningDate,
            CancellationToken cancellationToken)
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var vehicles = await dbContext.Vehicles
                .AsNoTracking()
                .Where(vehicle => vehicle.WarehouseId == warehouseId && vehicle.Status == VehicleStatus.Ready)
                .ToListAsync(cancellationToken);
            var packages = await dbContext.Packages
                .AsNoTracking()
                .Where(package => package.WarehouseId == warehouseId && package.CreatedAt <= planningDate)
                .ToListAsync(cancellationToken);

            return ExecuteWarehousePlanning(warehouseId, vehicles, packages, cancellationToken);
        }

        private static WarehousePlanningResponse ExecuteWarehousePlanning(
            long warehouseId,
            List<Vehicle> vehicles,
            List<Package> packages,
            CancellationToken cancellationToken)
        {
            //celková kapacita dodávek
            var fleetCapacity = CalculateFleetCapacity(vehicles);

            if (fleetCapacity.WeightKg <= 0 || fleetCapacity.VolumeM3 <= 0)
            {
                return new WarehousePlanningResponse
                {
                    WarehouseId = warehouseId
                };
            }

            //celková dimenze balíků
            var packageDemand = CalculatePackageDemand(packages);

            // Vzácnost hmotnosti a objemu balíčků vůči dostupnému místu v dodávkách.
            var weightScarcity = packageDemand.WeightKg / fleetCapacity.WeightKg;
            var volumeScarcity = packageDemand.VolumeM3 / fleetCapacity.VolumeM3;

            //seřazení balíčků podle priority
            var prioritizedPackages = CreatePrioritizedPackages(
                packages,
                weightScarcity,
                volumeScarcity,
                cancellationToken);

            //wrapper pro výpočty algoritmu
            var vehiclePlanningStates = vehicles
                .Select(vehicle => new VehiclePlanningState(vehicle))
                .ToList();

            //přiřazení do dodávek
            var unassignedPackageIds = AssignPackages(
                prioritizedPackages,
                vehiclePlanningStates,
                weightScarcity,
                volumeScarcity,
                cancellationToken);

            return new WarehousePlanningResponse
            {
                WarehouseId = warehouseId,
                PrioritizedPackageIds = prioritizedPackages
                    .Select(package => package.Package.Id)
                    .ToList(),
                UnassignedPackageIds = unassignedPackageIds,
                VehiclePlans = vehiclePlanningStates
                    .Select(CreateVehiclePlanningResponse)
                    .ToList()
            };
        }

        /*
         * Mapper z wrapperu na response
         */
        private static VehiclePlanningResponse CreateVehiclePlanningResponse(
            VehiclePlanningState vehiclePlanningState)
        {
            return new VehiclePlanningResponse
            {
                VehicleId = vehiclePlanningState.SourceVehicle.Id,
                PackageIds = vehiclePlanningState.PackageIds.ToList(),
                TotalWeightKg = vehiclePlanningState.CurrentWeightKg,
                TotalVolumeM3 = vehiclePlanningState.CurrentVolumeM3,
                TotalProfit = vehiclePlanningState.TotalProfit
            };
        }

        /*
         * Spočítá maximální kapacitu daných dodávek. 
         */
        private static FleetCapacity CalculateFleetCapacity(List<Vehicle> vehicles)
        {
            return new FleetCapacity(
                vehicles.Count * Vehicle.MaxWeightKg,
                vehicles.Count * Vehicle.MaxVolumeM3);
        }

        /*
         * Spočítá celkový objem a hmotnost daných balíčků
         */
        private static PackageDemandDimensions CalculatePackageDemand(List<Package> packages)
        {
            decimal totalWeightKg = 0;
            decimal totalVolumeM3 = 0;

            foreach (var package in packages)
            {
                totalWeightKg += package.WeightKg;
                totalVolumeM3 += package.VolumeM3;
            }

            return new PackageDemandDimensions(totalWeightKg, totalVolumeM3);
        }

        /*
         * Vrátí balíčky seřazené podle priority, kterou spočítá
         */
        private static List<PrioritizedPackage> CreatePrioritizedPackages(
            List<Package> packages,
            decimal weightScarcity,
            decimal volumeScarcity,
            CancellationToken cancellationToken)
        {
            List<PrioritizedPackage> prioritizedPackages = [];


            //kalkulace priority na základě poměru profitability balíčku a jeho váhy+objemu = profit per "rozměry"
            //s tím že vzácný zdroj je dražší/penalizovanýí, takže jeho spotřeba prioritu více snižuje.
            foreach (var package in packages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                prioritizedPackages.Add(new PrioritizedPackage(
                    package,
                    CalculatePriority(package, weightScarcity, volumeScarcity)));
            }

            //nejvyšší priorita jde první - při shodě starší
            prioritizedPackages.Sort((first, second) =>
            {
                var priorityComparison = second.Priority.CompareTo(first.Priority);

                return priorityComparison != 0
                    ? priorityComparison
                    : first.Package.Id.CompareTo(second.Package.Id);
            });

            return prioritizedPackages;
        }


        /*
         * Přiřadí jednotlivé balíčky do vozidel. 
         * Rozhoduje se dle nejlépe využitelné zbylé kapacity vozidla po vložení zásilky. Váhu hraje i vzácnost zdrojů podobně jako v prioritě balíčků.
         * Vrací list IDs nepřiřazených balíčků
         */
        private static List<long> AssignPackages(
            List<PrioritizedPackage> prioritizedPackages,
            List<VehiclePlanningState> vehiclePlanningStates,
            decimal weightScarcity,
            decimal volumeScarcity,
            CancellationToken cancellationToken)
        {
            List<long> unassignedPackageIds = [];

            foreach (var prioritizedPackage in prioritizedPackages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                VehiclePlanningState? selectedVehiclePlanningState = null;
                var lowestPenalty = decimal.MaxValue;

                foreach (var vehiclePlanningState in vehiclePlanningStates)
                {
                    //pokud se nevejde preskakuji vozidlo
                    if (!vehiclePlanningState.TryGetPenalty(
                            prioritizedPackage.Package,
                            weightScarcity,
                            volumeScarcity,
                            out var penalty))
                    {
                        continue;
                    }

                    //volím nejmenší penalty, případně starší auto při shodě
                    if (
                        penalty < lowestPenalty ||
                        (
                            penalty == lowestPenalty &&
                            (
                                selectedVehiclePlanningState is null ||
                                vehiclePlanningState.SourceVehicle.Id < selectedVehiclePlanningState.SourceVehicle.Id
                            )
                        )
                    )
                    {
                        selectedVehiclePlanningState = vehiclePlanningState;
                        lowestPenalty = penalty;
                    }
                }

                //balíček se nikam nevešel
                if (selectedVehiclePlanningState is null)
                {
                    unassignedPackageIds.Add(prioritizedPackage.Package.Id);
                    continue;
                }

                //přiřadím do vybraného vozidla
                selectedVehiclePlanningState.Assign(prioritizedPackage.Package);
            }

            return unassignedPackageIds;
        }


        /*
         * Spočítá prioritu pro daný balíček na základě vzácnosti a profitu balíku per jeho dimenzi
         */
        private static decimal CalculatePriority(
            Package package,
            decimal weightScarcity,
            decimal volumeScarcity)
        {
            if (package.Profit <= 0)
            {
                return 0;
            }

            //vzácnější "komodita" je dražší - čím víc jí balíček má, tím menší priorita
            var penalizedResourceCost =
                weightScarcity * (package.WeightKg / Vehicle.MaxWeightKg) +
                volumeScarcity * (package.VolumeM3 / Vehicle.MaxVolumeM3);

            return package.Profit / penalizedResourceCost;
        }

    }
}

using Alza_delivery.Models;

namespace Alza_delivery.Factories
{
    public class PackageFactory
    {
        private const decimal GramsPerKilogram = 1_000m;
        private const decimal MillilitersPerCubicMeter = 1_000_000m;

        private long _warehouseId;
        private decimal? _weightKg;
        private decimal? _volumeM3;
        private decimal? _profit;
        private DateTime? _createdAt;
        private int _count = 1;

        private PackageFactory()
        {
        }

        public static PackageFactory ForWarehouse(long warehouseId)
        {
            return new PackageFactory { _warehouseId = warehouseId };
        }

        public PackageFactory WithWeightKg(decimal weightKg)
        {
            if (weightKg <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weightKg));
            }

            _weightKg = weightKg;
            return this;
        }

        public PackageFactory WithVolumeM3(decimal volumeM3)
        {
            if (volumeM3 <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(volumeM3));
            }

            _volumeM3 = volumeM3;
            return this;
        }

        public PackageFactory WithProfit(decimal profit)
        {
            _profit = profit;
            return this;
        }

        public PackageFactory WithCreatedAt(DateTime createdAt)
        {
            _createdAt = createdAt;
            return this;
        }

        public PackageFactory Count(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            _count = count;
            return this;
        }

        public List<Package> Create()
        {
            if (_warehouseId <= 0)
            {
                throw new InvalidOperationException("Warehouse must be defined before creating packages.");
            }

            var packages = new List<Package>(_count);

            for (var index = 0; index < _count; index++)
            {
                packages.Add(new Package
                {
                    WarehouseId = _warehouseId,
                    WeightKg = _weightKg ?? GetRandomWeightKg(),
                    VolumeM3 = _volumeM3 ?? GetRandomVolumeM3(),
                    Profit = _profit ?? GetRandomProfit(),
                    CreatedAt = _createdAt ?? DateTime.UtcNow
                });
            }

            return packages;
        }

        private static decimal GetRandomWeightKg()
        {
            var maxWeightGrams = (long)(Vehicle.MaxWeightKg * GramsPerKilogram);

            return Random.Shared.NextInt64(1, maxWeightGrams + 1) / GramsPerKilogram;
        }

        private static decimal GetRandomVolumeM3()
        {
            var maxVolumeMilliliters = (long)(Vehicle.MaxVolumeM3 * MillilitersPerCubicMeter);

            return Random.Shared.NextInt64(1, maxVolumeMilliliters + 1) / MillilitersPerCubicMeter;
        }

        private static decimal GetRandomProfit()
        {
            return Random.Shared.NextInt64(1, 100_001) / 100m;
        }
    }
}

using Alza_delivery.Models;

namespace Alza_delivery.Services.Planning
{
    internal sealed class VehiclePlanningState
    {
        public VehiclePlanningState(Vehicle vehicle)
        {
            SourceVehicle = vehicle;
        }

        public Vehicle SourceVehicle { get; }

        public decimal CurrentWeightKg { get; private set; }

        public decimal CurrentVolumeM3 { get; private set; }

        public decimal TotalProfit { get; private set; }

        public List<long> PackageIds { get; } = [];


        /*
         * Zjistí "cenu" vložení balíčku do tohoto vozidla.
         * "Cena" se počítá dle zbývajícího místa s váhou vzácnosti zdrojů.
         * Prioritizuje se plnější vozidlo s penalizací vzácnějšího zdroje.
         */
        public bool TryGetPenalty(
            Package package,
            decimal weightScarcity,
            decimal volumeScarcity,
            out decimal penalty)
        {
            var remainingWeightKg = Vehicle.MaxWeightKg - CurrentWeightKg - package.WeightKg;
            var remainingVolumeM3 = Vehicle.MaxVolumeM3 - CurrentVolumeM3 - package.VolumeM3;

            //balíček se nevejde
            if (remainingWeightKg < 0 || remainingVolumeM3 < 0)
            {
                penalty = 0m;
                return false;
            }

            penalty =
                weightScarcity * (remainingWeightKg / Vehicle.MaxWeightKg) +
                volumeScarcity * (remainingVolumeM3 / Vehicle.MaxVolumeM3);

            return true;
        }

        /*
         * Přiřadí balíček do tohohle vozidla
         */
        public void Assign(Package package)
        {
            CurrentWeightKg += package.WeightKg;
            CurrentVolumeM3 += package.VolumeM3;
            TotalProfit += package.Profit;
            PackageIds.Add(package.Id);
        }
    }
}

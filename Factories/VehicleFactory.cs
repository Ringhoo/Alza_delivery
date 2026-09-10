using Alza_delivery.Enums;
using Alza_delivery.Models;

namespace Alza_delivery.Factories
{
    public class VehicleFactory
    {
        private long _warehouseId;
        private VehicleStatus _status = VehicleStatus.Ready;
        private int _count = 1;

        private VehicleFactory()
        {
        }

        public static VehicleFactory ForWarehouse(long warehouseId)
        {
            return new VehicleFactory { _warehouseId = warehouseId };
        }

        public VehicleFactory WithStatus(VehicleStatus status)
        {
            _status = status;
            return this;
        }

        public VehicleFactory Count(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            _count = count;
            return this;
        }

        public List<Vehicle> Create()
        {
            if (_warehouseId <= 0)
            {
                throw new InvalidOperationException("Warehouse must be defined before creating vehicles.");
            }

            var vehicles = new List<Vehicle>(_count);

            for (var index = 0; index < _count; index++)
            {
                vehicles.Add(new Vehicle
                {
                    WarehouseId = _warehouseId,
                    Status = _status
                });
            }

            return vehicles;
        }
    }
}

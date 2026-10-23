public class SimulationEngine
{
    public int Tick { get; private set; }
    public int DeliveredTotal { get; private set; }

    // A call advances one tick and makes at most one delivery visit.
    public int Step(List<Shelter> shelters, Vehicle vehicle)
    {
        // Validate the whole input before changing any state.
        if (vehicle.Capacity <= 0)
        {
            throw new ArgumentException("Vehicle capacity must be positive.");
        }

        bool hasDemand = false;
        foreach (Shelter shelter in shelters)
        {
            if (shelter.DemandLeft < 0)
            {
                throw new ArgumentException("Shelter demand must be nonnegative.");
            }

            if (shelter.DemandLeft > 0)
            {
                hasDemand = true;
            }
        }

        if (hasDemand == false)
        {
            return 0;
        }

        Tick = Tick + 1;

        foreach (Shelter shelter in shelters)
        {
            if (shelter.DemandLeft > 0)
            {
                int deliveredNow = Math.Min(shelter.DemandLeft, vehicle.Capacity);
                shelter.DemandLeft = shelter.DemandLeft - deliveredNow;
                DeliveredTotal = DeliveredTotal + deliveredNow;
                return deliveredNow;
            }
        }

        return 0;
    }
}

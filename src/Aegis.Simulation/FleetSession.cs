using System.Globalization;
namespace Aegis.Simulation;

public sealed record FleetVehicleState(string Id, VehicleState State);

public sealed partial class SimulationSession
{
    private FleetVehicleState[] fleet = Array.Empty<FleetVehicleState>();
    public IReadOnlyList<FleetVehicleState> Vehicles => UsesFleet ? Array.AsReadOnly(fleet) :
        Array.AsReadOnly(new[] { new FleetVehicleState("Vehicle-01", Vehicle) });

    private void InitializeFleet()
    {
        fleet = UsesFleet ? Enumerable.Range(1, Scenario.FleetSize!.Value).Select(i =>
            new FleetVehicleState("Vehicle-" + i.ToString("00", CultureInfo.InvariantCulture), new(VehiclePhase.AtDepot, null, 0, 0, 0))).ToArray() : Array.Empty<FleetVehicleState>();
    }

    private DeliveryEvent StepFleet()
    {
        Tick++;
        var actions = new VehicleAction[fleet.Length];
        // Movement precedes dispatch. Only vehicles idle at tick start may dispatch this tick.
        var idleAtStart = fleet.Select(v => v.State.Phase == VehiclePhase.AtDepot).ToArray();
        for (int i = 0; i < fleet.Length; i++)
        {
            var state = fleet[i].State;
            if (state.Phase == VehiclePhase.AtDepot) continue;
            string destination = state.Destination!;
            string kind; int amount = 0;
            if (state.Phase == VehiclePhase.Outbound)
            {
                int target = Array.FindIndex(shelters, s => s.Name == destination);
                if (shelters[target].BlockedUntil >= Tick) kind = "RouteWait";
                else if (state.TicksRemaining > 1) { state = state with { TicksRemaining = state.TicksRemaining - 1 }; kind = "Travelling"; }
                else
                {
                    amount = state.Load; DeliveredTotal += amount;
                    shelters[target] = shelters[target] with { Remaining = shelters[target].Remaining - amount };
                    state = Scenario.ReturnTicks == 0 ? new(VehiclePhase.AtDepot, null, 0, 0, 0) :
                        new(VehiclePhase.Returning, destination, 0, Scenario.ReturnTicks, Scenario.ReturnTicks);
                    kind = "Delivery";
                }
            }
            else
            {
                state = state.TicksRemaining == 1 ? new(VehiclePhase.AtDepot, null, 0, 0, 0) : state with { TicksRemaining = state.TicksRemaining - 1 };
                kind = state.Phase == VehiclePhase.AtDepot ? "Returned" : "Returning";
            }
            fleet[i] = fleet[i] with { State = state };
            actions[i] = new(fleet[i].Id, destination, amount, kind);
        }
        for (int i = 0; i < fleet.Length; i++)
        {
            if (!idleAtStart[i]) continue;
            int target = Array.FindIndex(shelters, s => s.BlockedUntil < Tick && s.Remaining >
                fleet.Where(v => v.State.Phase == VehiclePhase.Outbound && v.State.Destination == s.Name).Sum(v => v.State.Load));
            if (target < 0 || DepotStockRemaining == 0)
            {
                bool unassigned = shelters.Any(s => s.Remaining > fleet.Where(v => v.State.Phase == VehiclePhase.Outbound && v.State.Destination == s.Name).Sum(v => v.State.Load));
                actions[i] = new(fleet[i].Id, "Depot", 0, !unassigned ? "Standby" : DepotStockRemaining == 0 ? "SupplyWait" : "Waiting");
                continue;
            }
            int reserved = fleet.Where(v => v.State.Phase == VehiclePhase.Outbound && v.State.Destination == shelters[target].Name).Sum(v => v.State.Load);
            int load = Math.Min(Math.Min(Capacity, DepotStockRemaining!.Value), shelters[target].Remaining - reserved);
            int duration = Scenario.Shelters[target].TravelTicks ?? Scenario.TravelTicks;
            DepotStockRemaining -= load;
            fleet[i] = fleet[i] with { State = new(VehiclePhase.Outbound, shelters[target].Name, load, duration, duration) };
            actions[i] = new(fleet[i].Id, shelters[target].Name, 0, "Dispatched");
        }
        UpdateFleetStatus();
        return new(Tick, "Fleet", actions.Sum(a => a.Amount), "FleetTick", actions);
    }

    private void UpdateFleetStatus()
    {
        if (fleet.Any(v => v.State.Phase != VehiclePhase.AtDepot)) return;
        if (DeliveredTotal == InitialDemand) Status = RunStatus.Completed;
        else if (DepotStockRemaining == 0) Status = RunStatus.SupplyExhausted;
    }
}

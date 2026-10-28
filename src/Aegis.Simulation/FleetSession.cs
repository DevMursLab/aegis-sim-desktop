using System.Globalization;
namespace Aegis.Simulation;

public sealed record FleetVehicleState(string Id, VehicleState State);

public sealed partial class SimulationSession
{
    private FleetVehicleState[] fleet = Array.Empty<FleetVehicleState>();
    public IReadOnlyList<FleetVehicleState> Vehicles => UsesFleet ? Array.AsReadOnly(fleet.Select(v =>
        v with { State = v.State.Snapshot() }).ToArray()) :
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
            if (JunctionRoutingEnabled)
            {
                actions[i] = AdvanceJunctionVehicle(i);
                continue;
            }
            string kind; int amount = 0;
            var assignedRoute = state.Route ?? Scenario.Geography?.Routes.Single(r => r.Shelter == destination);
            if (assignedRoute != null && RoadBlocked(assignedRoute))
            {
                actions[i] = new(fleet[i].Id, destination, 0, state.Phase == VehiclePhase.Returning ? "ReturnRoadWait" : "RoadWait", "Assigned road closed; trip position and cargo retained.");
                continue;
            }
            if (state.Phase == VehiclePhase.Outbound)
            {
                int target = Array.FindIndex(shelters, s => s.Name == destination);
                if (shelters[target].BlockedUntil >= Tick) kind = "RouteWait";
                else if (state.TicksRemaining > 1) { state = state with { TicksRemaining = state.TicksRemaining - 1 }; kind = "Travelling"; }
                else
                {
                    amount = state.Load; DeliveredTotal += amount;
                    shelters[target] = shelters[target] with { Remaining = shelters[target].Remaining - amount };
                    int back = Scenario.Geography == null ? Scenario.ReturnTicks : state.Duration;
                    state = back == 0 ? new(VehiclePhase.AtDepot, null, 0, 0, 0) :
                        new(VehiclePhase.Returning, destination, 0, back, back, state.Route);
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
        var routes = Scenario.Geography != null && (roadClosures.Count > 0 || JunctionRoutingEnabled) ?
            shelters.ToDictionary(s => s.Name, s => AvailableRoute(s.Name), StringComparer.Ordinal) : null;
        for (int i = 0; i < fleet.Length; i++)
        {
            if (!idleAtStart[i]) continue;
            int target = Enumerable.Range(0, shelters.Length).Where(index => (routes == null || routes[shelters[index].Name] != null) && shelters[index].BlockedUntil < Tick && shelters[index].Remaining >
                fleet.Where(v => v.State.Phase == VehiclePhase.Outbound && v.State.Destination == shelters[index].Name).Sum(v => v.State.Load))
                .OrderByDescending(index => shelters[index].Priority).ThenBy(index => index).DefaultIfEmpty(-1).First();
            if (target < 0 || DepotStockRemaining == 0)
            {
                bool unassigned = shelters.Any(s => s.Remaining > fleet.Where(v => v.State.Phase == VehiclePhase.Outbound && v.State.Destination == s.Name).Sum(v => v.State.Load));
                actions[i] = new(fleet[i].Id, "Depot", 0, !unassigned ? "Standby" : DepotStockRemaining == 0 ? "SupplyWait" : "Waiting");
                continue;
            }
            int reserved = fleet.Where(v => v.State.Phase == VehiclePhase.Outbound && v.State.Destination == shelters[target].Name).Sum(v => v.State.Load);
            int load = Math.Min(Math.Min(Capacity, DepotStockRemaining!.Value), shelters[target].Remaining - reserved);
            var route = routes?[shelters[target].Name];
            int duration = route == null ? Scenario.Shelters[target].TravelTicks ?? Scenario.TravelTicks :
                (route.Meters + Scenario.Geography!.MetersPerTick - 1) / Scenario.Geography.MetersPerTick;
            DepotStockRemaining -= load;
            fleet[i] = fleet[i] with { State = JunctionRoutingEnabled ?
                new(VehiclePhase.Outbound, shelters[target].Name, load, 0, 0, route,
                    AtNode(Scenario.Geography!.DepotId, route!.TargetNode)) :
                new(VehiclePhase.Outbound, shelters[target].Name, load, duration, duration, route) };
            string? reason = shelters.Any(s => s.Priority != 0) ? FormattableString.Invariant($"Priority {shelters[target].Priority}; unreserved demand {shelters[target].Remaining - reserved}; loaded {load}; stock after {DepotStockRemaining}. Eligible highest priority first; ties keep shelter-list order.") : null;
            if (route != null) reason = (reason == null ? "" : reason + " ") + (JunctionRoutingEnabled ?
                $"Junction dispatch path: {string.Join(",", route.RoadIds)}; {route.Meters} m. One road per tick; paths reconsidered at nodes." :
                $"Open road path: {string.Join(",", route.RoadIds)}; {route.Meters} m; {duration} ticks each way.");
            actions[i] = new(fleet[i].Id, shelters[target].Name, 0, "Dispatched", reason);
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

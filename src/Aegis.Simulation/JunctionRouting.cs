using System.Text.Json.Serialization;

namespace Aegis.Simulation;

// Node is the last reached junction. An entered road is completed even if it closes.
public sealed record RoadJourney(string Node, string Target, string? RoadId, string? NextNode,
    int MetersRemaining, int RoadMeters, RoadPoint[] Points)
{
    [JsonIgnore]
    public RoadPoint Position
    {
        get
        {
            if (RoadId == null) return Points[0];
            double fraction = 1.0 - (double)MetersRemaining / RoadMeters;
            var lengths = Points.Zip(Points.Skip(1), (a, b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2))).ToArray();
            double distance = fraction * lengths.Sum();
            for (int i = 0; i < lengths.Length; i++)
            {
                if (lengths[i] > 0 && distance <= lengths[i])
                {
                    double t = distance / lengths[i];
                    return new(Points[i].X + (Points[i + 1].X - Points[i].X) * t, Points[i].Y + (Points[i + 1].Y - Points[i].Y) * t);
                }
                distance -= lengths[i];
            }
            return Points[^1];
        }
    }
}

public sealed partial class SimulationSession
{
    public bool JunctionRoutingEnabled { get; private set; }

    public void EnableJunctionRouting()
    {
        if (Scenario.Geography == null) throw new ArgumentException("Junction routing requires a road exercise.");
        if (Status == RunStatus.Completed) throw new InvalidOperationException("Completed runs are immutable.");
        if (JunctionRoutingEnabled) return;
        if (fleet.Any(v => v.State.Phase != VehiclePhase.AtDepot))
            throw new InvalidOperationException("Enable junction routing while every vehicle is at the depot.");
        JunctionRoutingEnabled = true;
    }

    private RoadJourney AtNode(string node, string target) =>
        new(node, target, null, null, 0, 0, new[] { RoadRouting.NodePoint(Scenario.Geography!, node) });

    private VehicleAction AdvanceJunctionVehicle(int index)
    {
        var state = fleet[index].State;
        var journey = state.Journey!;
        string destination = state.Destination!;
        bool outbound = state.Phase == VehiclePhase.Outbound;
        int shelter = Array.FindIndex(shelters, s => s.Name == destination);
        if (outbound && shelters[shelter].BlockedUntil >= Tick)
            return new(fleet[index].Id, destination, 0, "RouteWait", "Shelter closure pauses outbound service; node and road progress retained.");

        string? reason = null;
        if (journey.RoadId == null && journey.Node != journey.Target)
        {
            var map = Scenario.Geography!;
            var route = RoadRouting.FindBetween(map, journey.Node, journey.Target, destination,
                roadClosures.Where(r => r.Value >= Tick).Select(r => r.Key));
            state = state with { Route = route };
            if (route == null)
            {
                fleet[index] = fleet[index] with { State = state };
                return new(fleet[index].Id, destination, 0, outbound ? "RoadWait" : "ReturnRoadWait",
                    $"No open path from node {journey.Node}; position and cargo retained.");
            }
            var road = map.Roads.Single(r => r.Id == route.RoadIds[0]);
            bool forward = road.From == journey.Node;
            journey = new(journey.Node, journey.Target, road.Id, forward ? road.To : road.From,
                road.Meters, road.Meters, (forward ? road.Points : road.Points.Reverse()).ToArray());
            reason = $"Junction path: {string.Join(",", route.RoadIds)}; entering {road.Id} from {journey.Node}. Closed edges excluded.";
        }
        if (journey.RoadId != null)
        {
            // The remainder of this tick's budget is discarded at the next node.
            int left = Math.Max(0, journey.MetersRemaining - Scenario.Geography!.MetersPerTick);
            if (left == 0) journey = AtNode(journey.NextNode!, journey.Target);
            else journey = journey with { MetersRemaining = left };
        }
        state = state with { Journey = journey, TicksRemaining = journey.RoadId == null ? 0 :
            (journey.MetersRemaining + Scenario.Geography!.MetersPerTick - 1) / Scenario.Geography.MetersPerTick,
            Duration = journey.RoadId == null ? 0 : (journey.RoadMeters + Scenario.Geography!.MetersPerTick - 1) / Scenario.Geography.MetersPerTick };
        int amount = 0;
        string kind = outbound ? "Travelling" : "Returning";
        if (journey.RoadId == null && journey.Node == journey.Target)
        {
            if (outbound)
            {
                amount = state.Load;
                shelters[shelter] = shelters[shelter] with { Remaining = shelters[shelter].Remaining - amount };
                DeliveredTotal += amount;
                state = new(VehiclePhase.Returning, destination, 0, 0, 0, null,
                    AtNode(journey.Node, Scenario.Geography!.DepotId));
                kind = "Delivery";
            }
            else { state = new(VehiclePhase.AtDepot, null, 0, 0, 0); kind = "Returned"; }
        }
        fleet[index] = fleet[index] with { State = state };
        return new(fleet[index].Id, destination, amount, kind, reason);
    }
}

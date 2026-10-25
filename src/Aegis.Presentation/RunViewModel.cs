using System.Collections.ObjectModel;
using System.ComponentModel;
using Aegis.Simulation;

namespace Aegis.Presentation;

public sealed class RunViewModel : INotifyPropertyChanged
{
    private SimulationSession session;
    private RunDocument document;
    private RunDocument? replaySource;
    private readonly ObservableCollection<DeliveryEvent> events = new();
    private string? selectedShelterName;
    private bool faulted, busy, remote, connected = true, observer;
    private int replayTick;
    public event PropertyChangedEventHandler? PropertyChanged;
    public Action<RunDocument>? Persist { get; set; }
    public Func<ExerciseCommand, Task>? RemoteSubmit { get; set; }
    public RunDocument Document => document;
    public ReadOnlyObservableCollection<DeliveryEvent> Events { get; }
    public IReadOnlyList<ShelterState> Shelters => session.Shelters;
    public IReadOnlyList<string> ShelterNames { get; private set; }
    public int Tick => session.Tick;
    public int DeliveredTotal => session.DeliveredTotal;
    public int Remaining => session.InitialDemand - session.DeliveredTotal;
    public bool UsesLogistics => session.UsesLogistics;
    public bool UsesFleet => session.UsesFleet;
    public IReadOnlyList<ExerciseEventRow> EventRows => events.SelectMany(e => e.Actions == null ?
        new[] { new ExerciseEventRow(e.Tick, "Vehicle-01", e.Shelter, e.Amount, e.Kind) } :
        e.Actions.Select(a => new ExerciseEventRow(e.Tick, a.VehicleId, a.Shelter, a.Amount, a.Kind, a.Reason))).ToArray();
    public IReadOnlyList<MapVehicle> MapVehicles => UsesFleet ? session.Vehicles.Select(v =>
        new MapVehicle(v.Id, PositionOf(v.State), v.State.Destination, v.State.Phase.ToString(), v.State.Load)).ToArray() : Array.Empty<MapVehicle>();
    public string? VehicleDestination => session.Vehicle.Destination;
    public string StockLabel => UsesLogistics ? $"Depot: {session.DepotStockRemaining} packets | In transit: {session.InTransit} | Added supply: {session.RestockedTotal}" : "Schema 1: legacy instant visits / unlimited reload supply";
    public string VehicleLabel => UsesFleet ? $"Fleet: {session.Vehicles.Count} | Outbound: {session.Vehicles.Count(v => v.State.Phase == VehiclePhase.Outbound)} | Returning: {session.Vehicles.Count(v => v.State.Phase == VehiclePhase.Returning)} | At depot: {session.Vehicles.Count(v => v.State.Phase == VehiclePhase.AtDepot)}" : !UsesLogistics ? "Vehicle: instantaneous lesson model" : session.Vehicle.Phase == VehiclePhase.AtDepot ?
        session.Status == RunStatus.SupplyExhausted ? "Vehicle at depot / supply exhausted. Restock to resume." : "Vehicle at depot / ready" :
        $"Vehicle {session.Vehicle.Phase}: {session.Vehicle.Destination} | Load {session.Vehicle.Load} | {session.Vehicle.TicksRemaining} travel ticks left";
    public MapPoint VehiclePosition => PositionOf(session.Vehicle);
    private MapPoint PositionOf(VehicleState vehicle)
    {
        var origin = new MapPoint(170, 410);
        if (!UsesLogistics || vehicle.Phase == VehiclePhase.AtDepot) return origin;
        var target = document.Scenario.Shelters.First(s => s.Name == vehicle.Destination);
        double progress = 1.0 - (double)vehicle.TicksRemaining / vehicle.Duration;
        if (vehicle.Phase == VehiclePhase.Returning) progress = 1 - progress;
        return new(origin.X + (target.X - origin.X) * progress, origin.Y + (target.Y - origin.Y) * progress);
    }
    public string Status => faulted ? "Storage error" : IsReplay ? "Replay" : session.Status.ToString();
    public bool IsRunning => !remote && !IsReplay && !faulted && session.Status == RunStatus.Running;
    public bool IsLocal => !remote;
    public bool IsReplay => replaySource != null;
    public string ConnectionLabel { get; private set; } = "LOCAL / AUTOSAVE";
    public string Message { get; private set; } = "Choose a shelter, then inject a closure or advance the exercise.";
    public string RunLabel => $"{document.Scenario.Name} | run {document.Id.ToString()[..8]} | sequence {document.Commands.Length}";
    public string StartLabel => session.Status == RunStatus.Paused ? "Resume" : "Start";
    public string Summary => $"{DeliveredTotal} of {session.InitialDemand} packets delivered";
    public double CompletionPercent => session.InitialDemand == 0 ? 100 : 100.0 * DeliveredTotal / session.InitialDemand;
    public string? LastDeliveryShelter => events.LastOrDefault(e => e.Kind == "Delivery")?.Shelter;
    public IReadOnlyList<MapNode> MapNodes => Shelters.Select(s =>
    {
        var position = document.Scenario.Shelters.First(d => d.Name == s.Name);
        return new MapNode(s.Name, position.X, position.Y, s.InitialDemand, s.Remaining, s.BlockedUntil > session.Tick, s.Priority);
    }).ToArray();
    public string? SelectedShelterName
    {
        get => selectedShelterName;
        set
        {
            if (value != null && !Shelters.Any(s => s.Name == value)) return;
            if (selectedShelterName == value) return;
            selectedShelterName = value; Refresh();
        }
    }
    public string SelectedDetail
    {
        get
        {
            var shelter = Shelters.FirstOrDefault(s => s.Name == selectedShelterName);
            return shelter == null ? "Choose a shelter on the map or in the list." :
                $"{shelter.InitialDemand - shelter.Remaining} delivered / {shelter.InitialDemand} requested · {shelter.Remaining} remaining" +
                (UsesFleet ? $" · priority {shelter.Priority}" : "") +
                (shelter.BlockedUntil > Tick ? $" · route closed through tick {shelter.BlockedUntil}" : "");
        }
    }
    public int MaxReplayTick { get; private set; }
    public int ReplayTick
    {
        get => replayTick;
        set
        {
            if (!IsReplay) return;
            replayTick = Math.Clamp(value, 0, MaxReplayTick);
            var result = RunHistory.Replay(replaySource!, replayTick);
            session = result.Session; ReplaceEvents(result.Events); Refresh();
        }
    }
    public RelayCommand StartCommand { get; }
    public RelayCommand PauseCommand { get; }
    public RelayCommand StepCommand { get; }
    public RelayCommand ResetCommand { get; }
    public RelayCommand CloseRouteCommand { get; }
    public RelayCommand RestockCommand { get; }
    private bool CanControl => !busy && !faulted && !IsReplay && (!remote || connected && !observer);

    public RunViewModel(SimulationSession initialSession)
    {
        session = initialSession ?? throw new ArgumentNullException(nameof(initialSession));
        document = RunHistory.New(session.Scenario);
        ShelterNames = session.Shelters.Select(s => s.Name).ToArray();
        Events = new(events);
        StartCommand = new(() => Submit("Start"), () => CanControl && session.Status is RunStatus.Ready or RunStatus.Paused);
        PauseCommand = new(() => Submit("Pause"), () => CanControl && session.Status == RunStatus.Running);
        StepCommand = new(() => Submit("Step"), () => CanControl && session.Status is RunStatus.Ready or RunStatus.Paused);
        ResetCommand = new(() => { if (remote) Submit("Reset"); else NewScenario(document.Scenario); }, () => !busy && (!remote || connected && !observer));
        CloseRouteCommand = new(() => Submit("CloseRoute", SelectedShelterName, 5),
            () => CanControl && session.Status != RunStatus.Completed && SelectedShelterName != null);
        RestockCommand = new(() => Submit("Restock", amount: 20),
            () => CanControl && UsesLogistics && session.Status != RunStatus.Completed && session.Scenario.DepotStock!.Value + session.RestockedTotal + 20 <= 10000);
    }
    public void Advance() { if (IsRunning && !busy) Submit("Step"); }
    private void Submit(string type, string? target = null, int duration = 0, int amount = 0)
    {
        var command = new ExerciseCommand(Guid.NewGuid(), type, target, duration, amount);
        if (remote) { _ = SendRemote(command); return; }
        try
        {
            var candidate = RunHistory.Append(document, command);
            Persist?.Invoke(candidate); // Commit first: a failed save must never publish a successful state.
            Replace(candidate);
            Message = type == "CloseRoute" ? $"Route to {target} closed for {duration} ticks. Incident saved." : "Command committed; replay checksum verified.";
            Refresh();
        }
        catch (Exception error) { faulted = true; Message = error.Message; Refresh(); }
    }
    private async Task SendRemote(ExerciseCommand command)
    {
        busy = true; Refresh();
        try { await (RemoteSubmit ?? throw new InvalidOperationException("Host is disconnected."))(command); }
        catch (Exception error) { Message = error.Message; }
        finally { busy = false; Refresh(); }
    }
    public void NewScenario(ScenarioDefinition scenario)
    {
        if (remote) throw new InvalidOperationException("Disconnect before creating a local run.");
        try
        {
            var candidate = RunHistory.New(scenario); Persist?.Invoke(candidate);
            replaySource = null; faulted = false; Replace(candidate); Message = "New run created; earlier runs remain in saved history."; Refresh();
        }
        catch (Exception error) { Message = error.Message; Refresh(); }
    }
    public void LoadReplay(RunDocument saved)
    {
        if (remote) throw new InvalidOperationException("Replay is local; open a separate local window.");
        var result = RunHistory.Replay(saved);
        replaySource = saved; document = saved; MaxReplayTick = result.Session.Tick; replayTick = MaxReplayTick;
        session = result.Session; ShelterNames = session.Shelters.Select(s => s.Name).ToArray();
        ReplaceEvents(result.Events); faulted = false; Message = "READ-ONLY REPLAY / checksum verified. Use the timeline. Reset starts a new run."; Refresh();
    }
    public void SetRemote(bool isObserver)
    {
        remote = true; observer = isObserver; connected = false; ConnectionLabel = "CONNECTING"; Refresh();
    }
    public void SetConnection(bool available, string label) { connected = available; ConnectionLabel = label; Refresh(); }
    public void ApplyRemote(RunDocument incoming)
    {
        if (incoming.Id == document.Id && incoming.Commands.Length < document.Commands.Length) return;
        Replace(incoming); Message = "Authoritative host state / checksum verified."; Refresh();
    }
    public void Notify(string message) { Message = message; Refresh(); }
    private void Replace(RunDocument incoming)
    {
        var replay = RunHistory.Replay(incoming);
        document = incoming; session = replay.Session; ReplaceEvents(replay.Events);
        if (!ShelterNames.SequenceEqual(session.Shelters.Select(s => s.Name))) ShelterNames = session.Shelters.Select(s => s.Name).ToArray();
        if (selectedShelterName != null && !ShelterNames.Contains(selectedShelterName)) selectedShelterName = null;
    }
    private void ReplaceEvents(IEnumerable<DeliveryEvent> replacement) { events.Clear(); foreach (var item in replacement) events.Add(item); }
    private void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        StartCommand.Refresh(); PauseCommand.Refresh(); StepCommand.Refresh(); ResetCommand.Refresh(); CloseRouteCommand.Refresh(); RestockCommand.Refresh();
    }
}

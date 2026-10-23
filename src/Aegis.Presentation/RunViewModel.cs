using System.Collections.ObjectModel;
using System.ComponentModel;
using Aegis.Simulation;

namespace Aegis.Presentation;

public sealed class RunViewModel : INotifyPropertyChanged
{
    private readonly SimulationSession session;
    private readonly ObservableCollection<DeliveryEvent> events = new();
    private string? selectedShelterName;
    public event PropertyChangedEventHandler? PropertyChanged;
    public ReadOnlyObservableCollection<DeliveryEvent> Events { get; }
    public IReadOnlyList<ShelterState> Shelters => session.Shelters;
    public IReadOnlyList<string> ShelterNames { get; }
    public int Tick => session.Tick;
    public int DeliveredTotal => session.DeliveredTotal;
    public int Remaining => session.InitialDemand - session.DeliveredTotal;
    public string Status => session.Status.ToString();
    public bool IsRunning => session.Status == RunStatus.Running;
    public string StartLabel => session.Status == RunStatus.Paused ? "Resume" : "Start";
    public string Summary => $"{DeliveredTotal} of {session.InitialDemand} packets delivered";
    public double CompletionPercent => session.InitialDemand == 0 ? 100 : 100.0 * DeliveredTotal / session.InitialDemand;
    public string? LastDeliveryShelter => events.LastOrDefault()?.Shelter;
    public IReadOnlyList<MapNode> MapNodes => Shelters.Select((s, i) => new MapNode(
        s.Name, 480 + i % 2 * 430, 180 + i / 2 * 310, s.InitialDemand, s.Remaining)).ToArray();
    public string? SelectedShelterName
    {
        get => selectedShelterName;
        set
        {
            if (value != null && !Shelters.Any(s => s.Name == value)) return;
            if (selectedShelterName == value) return;
            selectedShelterName = value;
            Refresh();
        }
    }
    public string SelectedDetail
    {
        get
        {
            var shelter = Shelters.FirstOrDefault(s => s.Name == selectedShelterName);
            return shelter == null ? "Choose a shelter on the map or in the list." :
                $"{shelter.InitialDemand - shelter.Remaining} delivered / {shelter.InitialDemand} requested · {shelter.Remaining} remaining";
        }
    }
    public RelayCommand StartCommand { get; }
    public RelayCommand PauseCommand { get; }
    public RelayCommand StepCommand { get; }
    public RelayCommand ResetCommand { get; }

    public RunViewModel(SimulationSession session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        ShelterNames = session.Shelters.Select(s => s.Name).ToArray();
        Events = new(events);
        StartCommand = new(() => { session.Start(); Refresh(); },
            () => session.Status is RunStatus.Ready or RunStatus.Paused);
        PauseCommand = new(() => { session.Pause(); Refresh(); }, () => IsRunning);
        StepCommand = new(Deliver, () => session.Status is RunStatus.Ready or RunStatus.Paused);
        ResetCommand = new(() => { session.Reset(); events.Clear(); Refresh(); }, () => true);
    }

    // Timer callbacks cannot advance a paused, reset or completed run.
    public void Advance()
    {
        if (IsRunning) Deliver();
    }

    private void Deliver()
    {
        events.Add(session.Step());
        Refresh();
    }

    private void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        StartCommand.Refresh();
        PauseCommand.Refresh();
        StepCommand.Refresh();
        ResetCommand.Refresh();
    }
}

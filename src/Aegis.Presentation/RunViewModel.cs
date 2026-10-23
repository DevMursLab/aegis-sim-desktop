using System.Collections.ObjectModel;
using System.ComponentModel;
using Aegis.Simulation;

namespace Aegis.Presentation;

public sealed class RunViewModel : INotifyPropertyChanged
{
    private readonly SimulationSession session;
    private readonly ObservableCollection<DeliveryEvent> events = new();
    public event PropertyChangedEventHandler? PropertyChanged;
    public ReadOnlyObservableCollection<DeliveryEvent> Events { get; }
    public IReadOnlyList<ShelterState> Shelters => session.Shelters;
    public int Tick => session.Tick;
    public int DeliveredTotal => session.DeliveredTotal;
    public int Remaining => session.InitialDemand - session.DeliveredTotal;
    public string Status => session.Status.ToString();
    public bool IsRunning => session.Status == RunStatus.Running;
    public string StartLabel => session.Status == RunStatus.Paused ? "Resume" : "Start";
    public string Summary => $"{DeliveredTotal} of {session.InitialDemand} packets delivered";
    public RelayCommand StartCommand { get; }
    public RelayCommand PauseCommand { get; }
    public RelayCommand StepCommand { get; }
    public RelayCommand ResetCommand { get; }

    public RunViewModel(SimulationSession session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
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

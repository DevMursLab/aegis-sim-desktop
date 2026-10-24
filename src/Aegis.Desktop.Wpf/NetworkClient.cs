using System.Text.Json;
using System.IO;
using System.Windows.Threading;
using Aegis.Presentation;
using Aegis.Simulation;
using Microsoft.AspNetCore.SignalR.Client;

namespace Aegis.Desktop.Wpf;

public sealed class NetworkClient : IAsyncDisposable
{
    private readonly HubConnection connection;
    private readonly RunViewModel model;
    private readonly Dispatcher dispatcher;
    private readonly bool observer;
    private long revision = -1;

    public NetworkClient(string server, string secretsFile, bool observer, RunViewModel model, Dispatcher dispatcher)
    {
        var uri = new Uri(server);
        if (!uri.IsLoopback || uri.Scheme != "http") throw new ArgumentException("This demo connects only to a local http://127.0.0.1 host.");
        this.model = model; this.dispatcher = dispatcher; this.observer = observer;
        using var secrets = JsonDocument.Parse(File.ReadAllText(secretsFile));
        string token = secrets.RootElement.GetProperty(observer ? "Observer" : "Controller").GetString()!;
        connection = new HubConnectionBuilder().WithUrl(server.TrimEnd('/') + "/exercise", options => options.AccessTokenProvider = () => Task.FromResult<string?>(token))
            .WithAutomaticReconnect().Build();
        connection.On<RoomSnapshot>("State", snapshot => dispatcher.BeginInvoke(() => Apply(snapshot)));
        connection.Reconnecting += _ => { dispatcher.BeginInvoke(() => model.SetConnection(false, "STALE / RECONNECTING")); return Task.CompletedTask; };
        connection.Reconnected += async _ =>
        {
            await dispatcher.InvokeAsync(() => revision = -1);
            var snapshot = await connection.InvokeAsync<RoomSnapshot>("Snapshot");
            await dispatcher.InvokeAsync(() => { Apply(snapshot); model.SetConnection(true, observer ? "HOST / OBSERVER (READ ONLY)" : "HOST / CONTROLLER"); });
        };
        connection.Closed += _ => { dispatcher.BeginInvoke(() => model.SetConnection(false, "DISCONNECTED / restart window to retry")); return Task.CompletedTask; };
        model.SetRemote(observer);
        model.RemoteSubmit = async command =>
        {
            var current = model.Document;
            var snapshot = await connection.InvokeAsync<RoomSnapshot>("Submit", new HostCommandRequest(current.Id, current.Commands.Length, command));
            await dispatcher.InvokeAsync(() => Apply(snapshot));
        };
    }
    public async Task Connect()
    {
        await connection.StartAsync();
        var snapshot = await connection.InvokeAsync<RoomSnapshot>("Snapshot");
        await dispatcher.InvokeAsync(() => { Apply(snapshot); model.SetConnection(true, observer ? "HOST / OBSERVER (READ ONLY)" : "HOST / CONTROLLER"); });
    }
    private void Apply(RoomSnapshot snapshot)
    {
        if (snapshot.Revision <= revision) return;
        model.ApplyRemote(snapshot.Run); revision = snapshot.Revision;
    }
    public ValueTask DisposeAsync() => connection.DisposeAsync();
}

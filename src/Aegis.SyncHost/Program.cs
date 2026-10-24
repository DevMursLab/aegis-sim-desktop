using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aegis.Infrastructure;
using Aegis.Simulation;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
if (string.IsNullOrEmpty(builder.Configuration["urls"])) builder.WebHost.UseUrls("http://127.0.0.1:5099");
string directory = Path.GetFullPath(builder.Configuration["data-dir"] ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AEGIS-SIM", "host"));
Directory.CreateDirectory(directory);
string secretPath = Path.Combine(directory, "session-secrets.json");
var secrets = File.Exists(secretPath) ? JsonSerializer.Deserialize<SessionSecrets>(File.ReadAllText(secretPath))! :
    new SessionSecrets(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
if (!File.Exists(secretPath)) File.WriteAllText(secretPath, JsonSerializer.Serialize(secrets));
builder.Services.AddSignalR(options => { options.MaximumReceiveMessageSize = 32768; options.EnableDetailedErrors = false; });
builder.Services.AddSingleton(new RunRepository(Path.Combine(directory, "runs.db")));
builder.Services.AddSingleton(builder.Configuration["scenario"] is { } scenarioFile ? ScenarioFiles.Load(scenarioFile) : ScenarioDefinition.FloodDemo());
builder.Services.AddSingleton<ExerciseRoom>();
builder.Services.AddHostedService<ExerciseClock>();
var app = builder.Build();
app.Use(async (context, next) =>
{
    if (context.Connection.RemoteIpAddress is { } address && !System.Net.IPAddress.IsLoopback(address))
    { context.Response.StatusCode = 403; return; }
    if (context.Request.Path == "/health") { await next(); return; }
    string supplied = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.Ordinal);
    string? role = Matches(supplied, secrets.Controller) ? "Controller" : Matches(supplied, secrets.Observer) ? "Observer" : null;
    if (role == null) { context.Response.StatusCode = 401; return; }
    context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "LocalExerciseToken"));
    await next();
});
app.MapGet("/health", () => Results.Ok(new { service = "AEGIS-SIM local exercise host", engine = RunHistory.EngineVersion }));
app.MapGet("/snapshot", async (ExerciseRoom room) => await room.Read());
app.MapHub<ExerciseHub>("/exercise");
app.Run();

static bool Matches(string supplied, string expected) => supplied.Length == expected.Length &&
    CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(expected));
public sealed record SessionSecrets(string Controller, string Observer);

public sealed class ExerciseRoom
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly RunRepository repository;
    private RunDocument current;
    private long revision;
    public ExerciseRoom(RunRepository repository, ScenarioDefinition initialScenario)
    {
        this.repository = repository;
        var saved = repository.List().FirstOrDefault();
        current = saved == null ? RunHistory.New(initialScenario) : repository.Load(saved.Id);
        repository.Save(current);
    }
    public async Task<RoomSnapshot> Read()
    {
        await gate.WaitAsync();
        try { return new(revision, current); } finally { gate.Release(); }
    }
    public async Task<RoomSnapshot> Submit(HostCommandRequest request, IHubContext<ExerciseHub> clients, bool timer = false)
    {
        await gate.WaitAsync();
        try
        {
            if (request.Command.Id == Guid.Empty || request.Command.Type == "Reset" && (request.Command.Target != null || request.Command.Duration != 0 || request.Command.Amount != 0))
                throw new ArgumentException("Invalid command identity or fields.");
            if (request.Command.Type == "Reset" && current.CreationCommandId == request.Command.Id) return new(revision, current);
            var duplicate = current.Commands.FirstOrDefault(c => c.Command.Id == request.Command.Id);
            if (duplicate != null)
            {
                if (duplicate.Command != request.Command) throw new ArgumentException("Command ID reused for a different action.");
                return new(revision, current);
            }
            if (request.RunId != current.Id || request.ExpectedVersion != current.Commands.Length)
                throw new InvalidOperationException("Run version changed. Resync and retry your action.");
            if (timer && RunHistory.Replay(current).Session.Status != RunStatus.Running) return new(revision, current);
            var candidate = request.Command.Type == "Reset" ? RunHistory.New(current.Scenario) with { CreationCommandId = request.Command.Id } : RunHistory.Append(current, request.Command);
            repository.Save(candidate);
            current = candidate;
            var snapshot = new RoomSnapshot(++revision, current);
            await clients.Clients.All.SendAsync("State", snapshot);
            return snapshot;
        }
        finally { gate.Release(); }
    }
}
public sealed class ExerciseHub(ExerciseRoom room, IHubContext<ExerciseHub> clients) : Hub
{
    public Task<RoomSnapshot> Snapshot() => room.Read();
    public async Task<RoomSnapshot> Submit(HostCommandRequest request)
    {
        if (!Context.User!.IsInRole("Controller")) throw new HubException("Observer is read-only.");
        try { return await room.Submit(request, clients); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { throw new HubException(error.Message); }
    }
}
public sealed class ExerciseClock(ExerciseRoom room, IHubContext<ExerciseHub> clients, ILogger<ExerciseClock> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var snapshot = await room.Read();
                if (RunHistory.Replay(snapshot.Run).Session.Status == RunStatus.Running)
                    await room.Submit(new(snapshot.Run.Id, snapshot.Run.Commands.Length, new(Guid.NewGuid(), "Step")), clients, true);
            }
            catch (InvalidOperationException) { /* A concurrent controller action won; retry next tick. */ }
            catch (Exception error) { logger.LogError(error, "Exercise tick failed; persisted state was not advanced."); }
        }
    }
}

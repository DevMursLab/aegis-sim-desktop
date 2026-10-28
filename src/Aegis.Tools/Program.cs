using Aegis.Infrastructure;
using Aegis.Simulation;

if (args.Length == 2 && args[0] == "--forecast-eval")
{
    var experiment = ForecastLab.TrainAndEvaluate();
    File.WriteAllText(args[1], System.Text.Json.JsonSerializer.Serialize(experiment, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(experiment.Evaluation)); return;
}
if (args.Length == 2 && args[0] == "--profile") { PerformanceProfile.Write(args[1]); return; }
if (args.Length != 2 || args[0] != "--seed")
{
    Console.WriteLine("Usage: Aegis.Tools --seed <data-directory> | --profile <output.json> | --forecast-eval <output.json>"); Environment.ExitCode = 1; return;
}
string directory = Path.GetFullPath(args[1]); Directory.CreateDirectory(directory);
var repository = new RunRepository(Path.Combine(directory, "runs.db"));
var scenario = ScenarioDefinition.FloodDemo();
ScenarioFiles.Save(Path.Combine(directory, "flood-relief.json"), scenario);
ScenarioFiles.Save(Path.Combine(directory, "flood-relief.xml"), scenario);
var baseline = Finish(RunHistory.New(scenario));
var changed = Finish(RunHistory.Append(RunHistory.New(scenario), new(Guid.NewGuid(), "CloseRoute", "Shelter-A", 5)));
repository.Save(baseline); repository.Save(changed);
Reports.Export(Path.Combine(directory, "reports"), baseline);
Reports.Export(Path.Combine(directory, "reports"), changed);
Console.WriteLine(Reports.Compare(baseline, changed));
Console.WriteLine("Seeded two saved exercises, JSON/XML scenarios and verified PDF/HTML/CSV reports: " + directory);

static RunDocument Finish(RunDocument document)
{
    while (RunHistory.Replay(document).Session.Status != RunStatus.Completed)
        document = RunHistory.Append(document, new(Guid.NewGuid(), "Step"));
    return document;
}

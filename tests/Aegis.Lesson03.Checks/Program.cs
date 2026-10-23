// Dependency-free executable checks. Run with dotnet run, not dotnet test.
Check("One vehicle cannot serve multiple shelters in one tick", () =>
{
    var shelters = CreateShelters();
    var engine = new SimulationEngine();
    int delivered = engine.Step(shelters, new Vehicle { Capacity = 10 });
    Require(delivered == 10 && engine.Tick == 1 && engine.DeliveredTotal == 10);
    Require(shelters[0].DemandLeft == 15 && shelters[1].DemandLeft == 5 && shelters[2].DemandLeft == 18);
});

Check("Delivery does not exceed demand", () =>
{
    var shelters = new List<Shelter> { new Shelter { DemandLeft = 5 } };
    var engine = new SimulationEngine();
    Require(engine.Step(shelters, new Vehicle { Capacity = 10 }) == 5);
    Require(shelters[0].DemandLeft == 0 && engine.DeliveredTotal == 5);
});

Check("Same inputs reproduce all tick outputs", () =>
{
    string first = RunToCompletion();
    string second = RunToCompletion();
    Require(first == second);
    Require(first == "10,10,5,5,10,8|6|48");
});

Check("Completed run does not advance", () =>
{
    var engine = new SimulationEngine();
    var shelters = new List<Shelter> { new Shelter { DemandLeft = 5 } };
    var vehicle = new Vehicle { Capacity = 10 };
    engine.Step(shelters, vehicle);
    Require(engine.Step(shelters, vehicle) == 0);
    Require(engine.Tick == 1 && engine.DeliveredTotal == 5);
});

Check("Invalid capacity leaves state unchanged", () =>
{
    foreach (int capacity in new[] { 0, -1 })
    {
        var shelters = CreateShelters();
        var engine = new SimulationEngine();
        ExpectInvalid(() => engine.Step(shelters, new Vehicle { Capacity = capacity }));
        Require(engine.Tick == 0 && engine.DeliveredTotal == 0 && shelters[0].DemandLeft == 25);
    }
});

Check("Invalid later shelter prevents partial delivery", () =>
{
    var shelters = new List<Shelter> { new Shelter { DemandLeft = 25 }, new Shelter { DemandLeft = -1 } };
    var engine = new SimulationEngine();
    ExpectInvalid(() => engine.Step(shelters, new Vehicle { Capacity = 10 }));
    Require(engine.Tick == 0 && engine.DeliveredTotal == 0 && shelters[0].DemandLeft == 25);
});

Check("Empty or zero demand requires no ticks", () =>
{
    foreach (var shelters in new[] { new List<Shelter>(), new List<Shelter> { new Shelter { DemandLeft = 0 } } })
    {
        var engine = new SimulationEngine();
        Require(engine.Step(shelters, new Vehicle { Capacity = 10 }) == 0);
        Require(engine.Tick == 0 && engine.DeliveredTotal == 0);
    }
});

Console.WriteLine("All 7 checks passed.");

static List<Shelter> CreateShelters() => new List<Shelter>
{
    new Shelter { DemandLeft = 25 }, new Shelter { DemandLeft = 5 }, new Shelter { DemandLeft = 18 }
};

static string RunToCompletion()
{
    var shelters = CreateShelters();
    var vehicle = new Vehicle { Capacity = 10 };
    var engine = new SimulationEngine();
    var deliveries = new List<int>();
    for (int attempt = 0; attempt < 100; attempt++)
    {
        int moved = engine.Step(shelters, vehicle);
        if (moved == 0)
        {
            Require(shelters.All(s => s.DemandLeft == 0));
            return $"{string.Join(",", deliveries)}|{engine.Tick}|{engine.DeliveredTotal}";
        }
        deliveries.Add(moved);
    }
    throw new Exception("Simulation did not finish within the check limit.");
}

static void Require(bool condition)
{
    if (!condition) throw new Exception("Check failed.");
}

static void ExpectInvalid(Action action)
{
    try { action(); }
    catch (ArgumentException) { return; }
    throw new Exception("Expected an ArgumentException.");
}

static void Check(string name, Action action)
{
    action();
    Console.WriteLine($"PASS: {name}");
}

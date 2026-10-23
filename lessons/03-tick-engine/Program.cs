Vehicle vehicle = new Vehicle { Name = "Vehicle-01", Capacity = 10 };

List<Shelter> shelters = new List<Shelter>
{
    new Shelter { Name = "Shelter-A", DemandLeft = 25 },
    new Shelter { Name = "Shelter-B", DemandLeft = 5 },
    new Shelter { Name = "Shelter-C", DemandLeft = 18 }
};

SimulationEngine engine = new SimulationEngine();

Console.WriteLine("Lesson 03: one vehicle visit per tick");

while (true)
{
    int deliveredNow = engine.Step(shelters, vehicle);

    if (deliveredNow == 0)
    {
        break;
    }

    Console.WriteLine($"Tick {engine.Tick}: delivered={deliveredNow}, total={engine.DeliveredTotal}");

    foreach (Shelter shelter in shelters)
    {
        Console.WriteLine($"  {shelter.Name}: remaining={shelter.DemandLeft}");
    }
}

Console.WriteLine($"Completed in {engine.Tick} ticks. Total delivered: {engine.DeliveredTotal}.");

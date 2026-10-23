Vehicle vehicle = new Vehicle { Name = "Vehicle-01", Capacity = 10 };

List<Shelter> shelters = new List<Shelter>
{
    new Shelter { Name = "Shelter-A", DemandLeft = 25 },
    new Shelter { Name = "Shelter-B", DemandLeft = 5 },
    new Shelter { Name = "Shelter-C", DemandLeft = 18 }
};

if (vehicle.Capacity <= 0)
{
    Console.WriteLine("Vehicle capacity must be positive.");
    return;
}

foreach (Shelter shelter in shelters)
{
    if (shelter.DemandLeft < 0)
    {
        Console.WriteLine("Shelter demand must be nonnegative.");
        return;
    }
}

Console.WriteLine("Lesson 02: one delivery visit to each shelter");

int deliveredTotal = 0;

// Each visit starts with a fresh load; depot stock and travel are not modeled yet.
foreach (Shelter shelter in shelters)
{
    int deliveredNow = Math.Min(shelter.DemandLeft, vehicle.Capacity);
    shelter.DemandLeft = shelter.DemandLeft - deliveredNow;
    deliveredTotal = deliveredTotal + deliveredNow;

    Console.WriteLine($"{vehicle.Name} -> {shelter.Name}: delivered={deliveredNow}, remaining={shelter.DemandLeft}");
}

Console.WriteLine($"Round finished. Total delivered: {deliveredTotal}.");
Console.WriteLine("This was one round; some shelters may still need supplies.");

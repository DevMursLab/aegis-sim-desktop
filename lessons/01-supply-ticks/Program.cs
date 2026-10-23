// Lesson 01: one shelter, one vehicle, fixed delivery capacity per tick.
int demandLeft = 25;
int vehicleCapacity = 10;
int deliveredTotal = 0;
int tick = 0;

if (demandLeft < 0 || vehicleCapacity <= 0)
{
    Console.WriteLine("Demand must be nonnegative and capacity must be positive.");
    return;
}

Console.WriteLine("AEGIS-SIM | Lesson 01 | Synthetic supply simulation");

while (demandLeft > 0)
{
    tick = tick + 1;
    int deliveredNow = Math.Min(demandLeft, vehicleCapacity);
    demandLeft = demandLeft - deliveredNow;
    deliveredTotal = deliveredTotal + deliveredNow;

    Console.WriteLine($"Tick {tick}: delivered={deliveredNow}, remaining={demandLeft}, total={deliveredTotal}");
}

Console.WriteLine($"Completed in {tick} ticks. Total delivered: {deliveredTotal}.");

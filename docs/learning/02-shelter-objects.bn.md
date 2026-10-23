# Lesson 02: class, object ও List

আগের lesson-এ একটি আশ্রয়কেন্দ্র ছিল। এবার তিনটি কেন্দ্রের নাম ও বাকি চাহিদা একসঙ্গে রাখব। আলাদা delivery method নেই; `Math.Min` সরাসরি loop-এ আছে।

## ১. Class মানে ছাঁচ

[Shelter.cs](../../lessons/02-shelter-objects/Shelter.cs) খোলো:

```csharp
public class Shelter
{
    public string Name { get; set; } = "";
    public int DemandLeft { get; set; }
}
```

এটা বলে, প্রতিটি shelter-এর একটি নাম আর একটি বাকি চাহিদা থাকবে।

- `class Shelter`: Shelter নামে একটি ছাঁচ বানালাম।
- `public`: অন্য project code এই class বা property ব্যবহার করতে পারে।
- `string`: লেখা রাখার type; `int`: পূর্ণসংখ্যা রাখার type।
- `Name` ও `DemandLeft`: object-এর property, অর্থাৎ তার তথ্য।
- `get; set;`: তথ্য পড়া ও পরিবর্তন করা যায়।
- `= ""`: নামের শুরুর মান খালি লেখা। int property-র default মান ০।

এখন শুধু ছাঁচ তৈরি হয়েছে; নির্দিষ্ট কোনো কেন্দ্র এখনো বানাইনি।

## ২. Object মানে ওই ছাঁচ থেকে বানানো একটি জিনিস

```csharp
new Shelter { Name = "Shelter-A", DemandLeft = 25 }
```

`new Shelter` একটি object বানায়। `{ ... }` দিয়ে সেই object-এর শুরুর তথ্য বসাচ্ছি। `Shelter-A` আর `Shelter-B` একই class থেকে তৈরি হলেও আলাদা object; তাদের চাহিদাও আলাদা।

[Vehicle.cs](../../lessons/02-shelter-objects/Vehicle.cs)-এ একইভাবে গাড়ির নাম ও capacity আছে।

```csharp
Vehicle vehicle = new Vehicle { Name = "Vehicle-01", Capacity = 10 };
```

বাঁ পাশে `Vehicle` হলো type, `vehicle` হলো variable-এর নাম। ডান পাশে নতুন গাড়ির object তৈরি হচ্ছে।

## ৩. List মানে একই ধরনের জিনিসের তালিকা

```csharp
List<Shelter> shelters = new List<Shelter>
{
    new Shelter { Name = "Shelter-A", DemandLeft = 25 },
    new Shelter { Name = "Shelter-B", DemandLeft = 5 },
    new Shelter { Name = "Shelter-C", DemandLeft = 18 }
};
```

`List<Shelter>` মানে Shelter object রাখার তালিকা। `shelters` তার নাম। তালিকায় তিনটি আলাদা object রেখেছি।

## ৪. foreach মানে তালিকা থেকে একবারে একটি নাও

```csharp
foreach (Shelter shelter in shelters)
```

মানে: `shelters` তালিকার প্রতিটি object একে একে `shelter` নামে নিয়ে block-এর কাজ করো। প্রথমবার A, তারপর B, তারপর C।

```csharp
int deliveredNow = Math.Min(shelter.DemandLeft, vehicle.Capacity);
shelter.DemandLeft = shelter.DemandLeft - deliveredNow;
```

ডট `.` দিয়ে object-এর property ধরি। `shelter.DemandLeft` হলো এখন যে কেন্দ্রটি নিয়ে কাজ করছি তার বাকি চাহিদা। A-এর চাহিদা কমালে B-এর চাহিদা বদলায় না।

Program-এর প্রথম `foreach` শুধু সব চাহিদা valid কি না দেখে। দ্বিতীয়টি delivery করে। আগে পুরো input যাচাই করায় পরে কোনো invalid কেন্দ্র পেয়ে অর্ধেক delivery করে থেমে যাওয়ার সমস্যা হয় না।

## ৫. এবার এক round, সব কাজ শেষ নয়

এই lesson-এ প্রতিটি কেন্দ্রে একবার delivery visit হচ্ছে। প্রতিবার গাড়ি নতুন load নিয়ে যাচ্ছে ধরে নিচ্ছি। তাই ১০ capacity-র গাড়ি তিনবার গিয়ে মোট ২৫ দিতে পারে। একবারে ২৫ বহন করছে না। stock, যাত্রার সময় ও reload-এর নিয়ম এখনো যোগ করিনি।

Lesson 01-এ `while` দিয়ে একটি কেন্দ্রের চাহিদা শেষ করেছিলাম। এখানে `foreach` দিয়ে প্রতিটি কেন্দ্র একবার দেখছি। তাই round শেষ মানে সব চাহিদা শেষ নয়।

## চালাও

Repository root-এর terminal থেকে:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-lesson.ps1 -Lesson 02
```

Expected output:

```text
Lesson 02: one delivery visit to each shelter
Vehicle-01 -> Shelter-A: delivered=10, remaining=15
Vehicle-01 -> Shelter-B: delivered=5, remaining=0
Vehicle-01 -> Shelter-C: delivered=10, remaining=8
Round finished. Total delivered: 25.
This was one round; some shelters may still need supplies.
```

## তোমার কাজ

1. শুধু `Vehicle-01` নামটি বদলে নিজের দেওয়া নাম রাখো।
2. তালিকায় `Shelter-D` যোগ করো, চাহিদা ৭। আগে অনুমান করো মোট delivery কত হবে, তারপর চালাও।
3. গাড়ির capacity ৪ করো। প্রতিটি কেন্দ্রে কত যাবে আর কত বাকি থাকবে কাগজে লিখে চালিয়ে মেলাও।
4. মূল A/B/C এবং capacity ১০ ফিরিয়ে রাখো।

নিজের ভাষায় বলবে: `Shelter` আর `shelter` কীভাবে আলাদা? `class` আর `object` কী? কেন A-এর তথ্য বদলালে B বদলায় না? কেন round শেষেও কিছু চাহিদা বাকি?

এই তিনটি file একই folder/project-এ থাকায় C# compiler একসঙ্গে নেয়। নিজে `Shelter.cs` খুলে দেখবে, তারপর `Vehicle.cs`, সবশেষে [Program.cs](../../lessons/02-shelter-objects/Program.cs)। পরের ধাপে object-এর তথ্য valid রাখার নিয়ম এবং tick engine তৈরি করব।

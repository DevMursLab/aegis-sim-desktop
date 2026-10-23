# Lesson 03: Step দিয়ে এক ধাপ চালাও

Lesson 02-এ প্রতিটি কেন্দ্রে একবার গিয়েছিলাম। এবার একটি গাড়ি এক tick-এ সর্বোচ্চ একটি কেন্দ্রে যাবে। A-এর চাহিদা শেষ হলে B, তারপর C। সব চাহিদা শেষ হলে program থামবে। প্রতি visit-এ নতুন load পাওয়া যায় ধরে নিচ্ছি; depot stock, ফেরার সময় ও দূরত্ব এখনো নেই।

## আগে এই তিনটি কথা

1. `Shelter` class কেন্দ্রের তথ্য রাখে।
2. `Vehicle` class গাড়ির তথ্য রাখে।
3. নতুন `SimulationEngine` class প্রতি ধাপে কী হবে সেই নিয়ম চালায়।

আগের lesson দুটো বদলায়নি। এই lesson আলাদা project; একই নামের class-গুলো এখানে শেখার জন্য রাখা আছে। মূল app-এর shared Domain project পরে তৈরি হবে।

## ১. Engine-এর object বানাই

```csharp
SimulationEngine engine = new SimulationEngine();
```

আগে `new Vehicle` দিয়ে গাড়ি বানিয়েছিলে। এবার `new SimulationEngine` দিয়ে engine বানাচ্ছি। `engine` variable দিয়ে তার তথ্য ও কাজ ব্যবহার করব।

## ২. Step একটি method: engine-কে একবার কাজ করতে বলা

```csharp
int deliveredNow = engine.Step(shelters, vehicle);
```

এই লাইন বলে: এই কেন্দ্রের তালিকা ও গাড়ি নিয়ে এক ধাপ কাজ করো, কত প্যাকেট পৌঁছালে তা ফেরত দাও। ফেরত আসা সংখ্যাটি `deliveredNow`-এ রাখি।

এই ছোট model-এ valid input দিয়ে `Step` শূন্য ফেরত দিলে সব চাহিদা শেষ। ভবিষ্যতে stock বা গাড়ির অনুপস্থিতি যোগ হলে শূন্য delivery মানেই completed হবে না; তখন আলাদা status দরকার হবে।

## ৩. নতুন bool

```csharp
bool hasDemand = false;
```

`bool`-এ শুধু `true` বা `false` রাখা যায়। এখানে মানে: কোনো কেন্দ্রে চাহিদা বাকি আছে কি? একটি কেন্দ্রেও চাহিদা থাকলে `true` করি। পুরো তালিকা যাচাই শেষ না করে delivery শুরু করি না।

## ৪. Engine-এর Step কী করে?

এই ক্রমে [SimulationEngine.cs](../../lessons/03-tick-engine/SimulationEngine.cs) পড়ো:

1. capacity শূন্য বা ঋণাত্মক হলে error দেয়।
2. প্রতিটি চাহিদা যাচাই করে; ঋণাত্মক হলে error দেয়।
3. কোনো চাহিদা না থাকলে ০ ফেরত দেয়; tick বাড়ায় না।
4. চাহিদা থাকলে tick এক বাড়ায়।
5. তালিকার প্রথম চাহিদাযুক্ত কেন্দ্রে `Math.Min` দিয়ে delivery করে।
6. delivery-র পরই `return deliveredNow` করে method থেকে বের হয়। তাই একই tick-এ পরের কেন্দ্রেও যায় না।

`throw new ArgumentException(...)` মানে ভুল input-এর কারণে কাজ চালানো যাচ্ছে না। এখন এই ভুলে console program থামবে ও error দেখাবে; পরে UI-তে সুন্দর error message দেখাব।

## ৫. private set কেন?

```csharp
public int Tick { get; private set; }
```

বাইরের কোড `engine.Tick` পড়তে পারে। কিন্তু `engine.Tick = 100` লিখে বদলাতে পারে না। এই property-র মান engine class-এর ভেতরেই বদলাবে। `DeliveredTotal`-এর ক্ষেত্রেও একই নিয়ম।

এটা সম্পূর্ণ domain protection নয়: আগের মতো Shelter-এর public property বাইরে থেকে বদলানো যায়। এই শিক্ষামূলক version-এ প্রতি Step-এর আগে input যাচাই করছি।

## ৬. while আর break

```csharp
while (true)
{
    int deliveredNow = engine.Step(shelters, vehicle);
    if (deliveredNow == 0)
    {
        break;
    }
}
```

`while (true)` বারবার কাজ করে। `break` এই loop থেকে বের করে। `return` আর `break` এক নয়: engine-এর `return` method শেষ করে caller-এর কাছে ফেরে; Program-এর `break` তার loop শেষ করে।

## চালিয়ে দেখো

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-lesson.ps1 -Lesson 03
```

প্রতি tick-এ কেন্দ্রগুলোর বাকি চাহিদাও দেখাবে। মূল হিসাব:

| Tick | কেন্দ্র | পৌঁছাল | ওই কেন্দ্রে বাকি | মোট পৌঁছাল |
|---|---|---:|---:|---:|
| 1 | A | 10 | 15 | 10 |
| 2 | A | 10 | 5 | 20 |
| 3 | A | 5 | 0 | 25 |
| 4 | B | 5 | 0 | 30 |
| 5 | C | 10 | 8 | 40 |
| 6 | C | 8 | 0 | 48 |

এখানে tick মানে এক visit-এর logical ধাপ, এক সেকেন্ড নয়। একই input ও তালিকার একই ক্রমে একই ফল হয়। এখন random কাজ নেই, তাই seed-ও লাগছে না।

## নিয়ম ঠিক আছে কি না যাচাই

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-lesson.ps1 -Lesson 03 -Check
```

সাতটি executable check আছে: প্রতি tick-এ একটি delivery, অতিরিক্ত delivery না হওয়া, একই input-এ একই ফল, completion-এর পরে tick না বাড়া, ভুল capacity, পরে থাকা ভুল shelter-এর কারণে আগেরটিতে partial delivery না হওয়া, এবং শূন্য/খালি চাহিদা।

এগুলো কোনো test framework ছাড়া console checks; `dotnet test` দিয়ে চালানোর xUnit suite নয়। ভুল পেলে exception দিয়ে command fail করে। Test helper code আজ মুখস্থ করার প্রয়োজন নেই; কী নিয়ম পরীক্ষা হচ্ছে তা আগে বোঝো।

## তোমার অনুশীলন

1. শুধু A-এর চাহিদা ২৫ থেকে ১২ করো। চালানোর আগে নতুন tick সংখ্যা অনুমান করো।
2. B-কে তালিকার প্রথমে রাখলে delivery-র ক্রম কীভাবে বদলায়? মোট প্যাকেট কি বদলায়?
3. সব চাহিদা ০ করলে কয় tick হয়?
4. মূল মান ফিরিয়ে `-Check` আবার চালাও।

নিজের ভাষায় বলো: `engine.Step(...)` কেন একবারে সব কেন্দ্রের কাজ শেষ করে না? `break` ও `return` কোথা থেকে বের হয়? `private set` কী আটকায়?

পড়ার ক্রম: প্রথমে [Program.cs](../../lessons/03-tick-engine/Program.cs)-এর Step call; তারপর engine-এর Step method। প্রথমে পুরো file একসঙ্গে বুঝতে হবে না।

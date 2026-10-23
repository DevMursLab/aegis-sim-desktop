# Lesson 01: ত্রাণ পৌঁছানো দিয়ে C# শুরু

## আজ কী বানাচ্ছ?

এক আশ্রয়কেন্দ্রে ২৫ প্যাকেট ত্রাণ লাগবে। গাড়ি প্রতি ধাপে সর্বোচ্চ ১০ প্যাকেট পৌঁছাতে পারে। এক ধাপকে বলছি tick। এই সরল lesson-এ যাত্রার দূরত্ব, depot stock বা ফেরার সময় নেই; এগুলো পরের নিয়ম।

কোড: [Program.cs](../../lessons/01-supply-ticks/Program.cs)।

## ১. Variable: নাম দিয়ে মান রাখা

```csharp
int demandLeft = 25;
int vehicleCapacity = 10;
int deliveredTotal = 0;
int tick = 0;
```

`int` পূর্ণসংখ্যা রাখে। `demandLeft` বাকি চাহিদা, `vehicleCapacity` প্রতি tick-এর সর্বোচ্চ সরবরাহ, `deliveredTotal` মোট পৌঁছানো ত্রাণ। `=` ডান পাশের মান বাঁ পাশের variable-এ রাখে। `;` statement শেষ করে।

## ২. Condition: ভুল মান আগে ধরো

```csharp
if (demandLeft < 0 || vehicleCapacity <= 0)
```

`if` শর্ত সত্য হলে তার block চলে। `||` মানে দুই শর্তের যেকোনো একটি সত্য। ঋণাত্মক চাহিদা এই model-এ অর্থপূর্ণ নয়। শূন্য capacity দিয়ে loop চললে চাহিদা কখনো কমবে না, তাই আগেই থামাচ্ছি। এখানে `return` পুরো top-level program থেকে বের হয়।

## ৩. Method: একটি হিসাবের নাম দাও

```csharp
static int CalculateDelivery(int demand, int capacity)
{
    return Math.Min(demand, capacity);
}
```

Method দুটি সংখ্যা নেয় এবং একটি পূর্ণসংখ্যা ফেরত দেয়। `Math.Min` ছোট সংখ্যাটি নেয়। চাহিদা ৫ আর capacity ১০ হলে পৌঁছাবে ৫। Method-এর `return` শুধু ওই method-এর ফল caller-কে ফেরত দেয়। `static` এখানে method-টিকে বাইরের local variable সরাসরি ব্যবহার করতে দেয় না; প্রয়োজনীয় মান parameter দিয়ে পাঠাচ্ছি।

## ৪. Loop: যতক্ষণ কাজ বাকি

```csharp
while (demandLeft > 0)
```

চাহিদা শূন্যের বেশি থাকলে block আবার চলে। প্রতি বার:

1. `tick` এক বাড়ে।
2. `deliveredNow` হিসাব হয়।
3. বাকি চাহিদা কমে।
4. মোট delivery বাড়ে।

`deliveredTotal = deliveredTotal + deliveredNow` মানে আগের মোটের সঙ্গে নতুন পরিমাণ যোগ করে আবার মোটে রাখো। এটি গণিতের সমীকরণ নয়, মান update করার নির্দেশ।

## ৫. Output পড়ো

`Console.WriteLine` terminal-এ লেখা দেখায়। `$"Tick {tick}"`-এর `{tick}` জায়গায় variable-এর মান বসে; একে string interpolation বলে।

চালানোর পরে simulation-এর অংশে পাবে:

```text
AEGIS-SIM | Lesson 01 | Synthetic supply simulation
Tick 1: delivered=10, remaining=15, total=10
Tick 2: delivered=10, remaining=5, total=20
Tick 3: delivered=5, remaining=0, total=25
Completed in 3 ticks. Total delivered: 25.
```

প্রথমবার SDK welcome/build message-ও আসতে পারে।

## এখন তোমার কাজ

প্রতিটি পরিবর্তনের আগে ফল অনুমান করবে, পরে চালিয়ে যাচাই করবে। একবারে শুধু একটি case চালাও।

| অনুশীলন | demandLeft | vehicleCapacity | নিজে কী বের করবে |
|---|---:|---:|---|
| A | 37 | 8 | কয় tick, শেষ tick-এ কত পৌঁছাবে |
| B | 5 | 10 | প্রয়োজনের বেশি যায় কি না |
| C | 0 | 10 | loop একবারও চলে কি না |
| D | 25 | 0 | কেন validation message আসে |

শেষে মূল ২৫/১০ মান ফিরিয়ে রাখো। `Program.cs`-এ নিজে একটি output line যোগ করো, যাতে শুরুতেই initial demand দেখায়।

## নিজের ভাষায় পাঁচটি উত্তর

1. `demandLeft` আর `deliveredTotal` কেন আলাদা?
2. সরাসরি capacity বিয়োগ না করে `Math.Min` কেন?
3. শূন্য capacity-র validation সরিয়ে দিলে কী হতে পারে?
4. প্রতি tick-এ remaining + total কত থাকে? কেন?
5. এই lesson-এ একটি tick কি বাস্তবের এক সেকেন্ড? কোথায় সে নিয়ম লেখা আছে?

## শেষ হওয়ার শর্ত

কোড না দেখে delivery rule বলতে পারবে, A–D-এর ফল ব্যাখ্যা করতে পারবে এবং নিজের output line যোগ করতে পারবে। আজ class, map, database বা multiplayer শেখার প্রয়োজন নেই। পরের lesson-এ এই একই ধারণাকে `Shelter` ও `Vehicle` object-এ নেব।

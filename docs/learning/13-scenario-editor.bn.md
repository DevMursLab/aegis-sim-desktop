# Lesson 13 — নিজের scenario বানাও

Source থেকে desktop খুলে **Tools → Scenario editor** চাপো। এখন খোলা scenario-এর একটি আলাদা draft পাবে।

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-lesson.ps1 -Lesson 05
```

1. Scenario name দাও। Vehicle capacity ১২ করো।
2. Shelter-এর নাম, demand, X/Y বদলাও। **Add shelter** দিয়ে row যোগ করো; row বেছে **Remove selected** দিয়ে সরাও।
3. **Finite stock + travel** চালু করলে stock, outbound ticks ও return ticks দাও। Shelter-এর travel override ফাঁকা থাকলে সাধারণ সময় ব্যবহার হবে।
4. **Validate** চাপো। Capacity-তে `abc` লিখে error দেখো, তারপর ঠিক করো।
5. **Save JSON/XML** file রাখে; run শুরু করে না।
6. **Start as new run** নতুন ID-তে exercise তৈরি করে। আগের history Saved runs-এ থাকে।

**Close draft** save না করা edit বাদ দেয়। শুধু editor খুললে মূল run বদলায় না। Running exercise চলতে থাকে; নতুন run শুরু করলে Ready অবস্থায় খুলবে। Multiplayer window-তে editor disabled।

## Draft-এ সংখ্যা string কেন?

লেখার মাঝখানে ঘর ফাঁকা বা শুধু `-` থাকতে পারে। Draft সেই অসম্পূর্ণ input রাখে। `TryBuild()` সংখ্যা পড়বে, তারপর domain-এর `Validate()` চালাবে। সব ঠিক হলে নতুন `ScenarioDefinition` দেবে; ভুল হলে আংশিক scenario দেবে না।

Draft বদলালে original scenario বদলায় না। প্রতিবার validation নতুন shelter array তৈরি করে। Database save ব্যর্থ হলে editor খোলা থাকে, যাতে input না হারায়।

Coordinate স্থানীয় X/Y, GPS নয়। Decimal-এর জন্য dot ব্যবহার করো। Schema 1 বেছে নিলে stock ও travel বাদ দিয়ে পুরোনো instant-delivery নিয়মে নতুন scenario হয়।

## নিজের অনুশীলন

- Demand ২৫, capacity ১২, শুধু একটি shelter: schema 1-এ কত tick?
- একই নাম দুই shelter-এ দিলে কেন validation বন্ধ হয়?
- Stock ৩০ কিন্তু demand ৪৮ রাখা ভুল input নয় কেন?
- Save আর Start কেন আলাদা কাজ?

Editor-এর ১২টি executable check আছে। পুরো repository পরীক্ষা করতে `scripts/verify.ps1` চালাও। নতুন editor **2.3 source**-এ আছে; আগের **2.2 ZIP-তে নেই**।

# Lesson 10 — মজুত, যাত্রা আর ফেরার সময়

এবার গাড়ি রওনা দিলেই delivery হবে না। গাড়ির মাল কেন্দ্রে পৌঁছালে delivery হবে।

তিনটা সংখ্যা আলাদা করে দেখো:

- **Depot stock:** গুদামে কত প্যাকেট আছে।
- **Cargo:** গাড়িতে কত প্যাকেট আছে।
- **Delivered:** কেন্দ্রে কত প্যাকেট পৌঁছেছে।

শুরুতে stock ৩০, cargo ০, delivered ০। প্রথম Step-এ গাড়িতে ১০ উঠবে: stock ২০, cargo ১০, delivered এখনো ০। পরের দুই tick যাত্রা। পৌঁছালে cargo ০ আর delivered ১০ হবে। তারপর এক tick-এ গাড়ি ফেরে।

## নিজে চালাও

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-lesson.ps1 -Lesson 05
```

1. **Stock + travel demo** চাপো।
2. **Step** দিয়ে গাড়ির অবস্থান, stock আর event দেখো।
3. Tick ১৬-তে ৩০ প্যাকেট দেওয়া শেষ; আরও ১৮ দরকার। Status হবে `SupplyExhausted`।
4. **Restock +20** চাপো। গুদামে ২০ যোগ হবে; এই command নিজে tick বাড়ায় না।
5. আবার Step দাও। Tick ২৪-এ ৪৮ delivery, stock ২, cargo ০; গাড়িও ফিরে এসেছে।

প্রতি trip = মাল তোলার ১ tick + যাওয়ার ২ tick + ফেরার ১ tick। মোট ছয় trip, তাই ২৪ tick। ছোট load-ও একই সময় নেয়।

## কোডে কী হলো

`ScenarioDefinition`-এ schema 2-এর stock, travel এবং return settings আছে। `VehicleState` গাড়ির phase, destination, cargo ও বাকি tick ধরে। `SimulationSession.Step()` এক ধাপের নিয়ম চালায়। একই command history দিয়ে SQLite reload, replay এবং multiplayer-এ একই ফল পাওয়া যায়।

যাওয়ার সময় route বন্ধ হলে গাড়ি অপেক্ষা করবে; cargo হারাবে না। ফেরার পথে closure বাধা দেয় না—এটি এই lesson-এর নির্দিষ্ট নিয়ম। Map-এর অবস্থান tick থেকে আঁকা হয়; বাস্তব রাস্তা বা গতিবেগ হিসাব হচ্ছে না।

পুরোনো schema 1 lesson-এ এক tick-এ delivery আগের মতোই হয়। নতুন app পুরোনো 2.0 saved run পড়তে পারে; পুরোনো app দিয়ে নতুন 2.1 run খোলা যাবে না।

## বোঝা যাচাই করো

- গাড়ি ছাড়ার সময় delivered কেন বাড়ে না?
- Stock শেষ হলেও cargo থাকলে গাড়ি কেন চলতে পারে?
- Restock ২০ দেওয়ার পর শেষ পর্যন্ত ২ বাকি থাকে কেন?
- `finite-stock-travel.json` কপি করে travelTicks ৩ করো। চালানোর আগে মোট tick অনুমান করো।

হিসাব মেলাও: **শুরুর stock + restock = গুদামের stock + cargo + delivered**। শুধু stock শূন্য দেখেই exercise completed বলা যায় না।

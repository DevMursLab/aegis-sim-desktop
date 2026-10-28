# Lesson 16 — রাস্তা থেকে যাত্রার tick

আগে আমরা নিজেরা বলতাম গাড়ি পৌঁছাবে ২ tick পরে। এখন রাস্তার দৈর্ঘ্য থেকে সময় বের হবে। **Tools → Road network demo** খোলো।

একটি route ৮৯০ মিটার। প্রতি tick-এ গাড়ি ৩০০ মিটার যেতে পারে। `890 / 300` প্রায় ২.৯৭। পুরো যাত্রা শেষ করতে তাই **৩ tick** লাগে:

```csharp
int travelTicks = (int)Math.Ceiling(routeMeters / (double)metersPerTick);
```

`double` না হলে integer division দশমিক বাদ দেবে। `Ceiling` পরের পূর্ণসংখ্যা নেয়। এই মডেলে ফেরার সময়ও একই ৩ tick। Dispatch-এর tick আলাদা; তাই শুধু দূরত্ব ভাগ করে পুরো exercise-এর শেষ tick পাওয়া যায় না।

`assets/maps/road-exercise.geojson`-এ depot, junction ও shelter হলো Point; road হলো LineString। প্রতিটি road-এর `from` ও `to` দিয়ে বোঝানো হয়েছে কোন দুই node জুড়েছে। Dijkstra algorithm depot থেকে সবচেয়ে কম মোট road-meter-এর পথ বেছে নেয়। গাড়ির marker সেই পথের বাঁক ধরে চলে।

দুটি গাড়ি, মোট ৩০ প্যাকেট। B-এর priority ৯, C-এর ৫, A-এর ০। প্রথমে B ও C-এর জন্য cargo reserve হয়। A/B-এর পথ ৮৯০ মিটার, C-এর ১৩৩৫ মিটার। সবাই পেয়ে গাড়িগুলো ফিরে এলে **tick 14**-এ শেষ হয়। Step দিয়ে দেখে তারপর replay-তে tick 2-এ ফিরে marker মিলিয়ে দেখো।

এই ছোট মডেলে রাস্তা দুই দিকেই ব্যবহার করা যায়, পথ import-এর সময় স্থির হয়। Close route একটি shelter-এর outbound trip থামায়; নতুন source build-এ shared road বন্ধ ও নতুন dispatch-এর বিকল্প পথ খোঁজা [Lesson 19](19-dynamic-roads.bn.md)-এ আছে। Distance পৃথিবীকে গোলক ধরে হিসাব করা approximation; বাস্তব গাড়ির সময় দাবি করছি না।

Generic scenario editor এখানে বন্ধ। GeoJSON-এর `exercise.metersPerTick` ৩০০ থেকে ৪৫০ করে আলাদা ফাইলে save ও import করো। পুরোনো saved run বদলাবে না। নতুন completion tick দেখে কারণ নিজের ভাষায় লিখো। Geography-এর snapshot ও hash থাকায় replay মূল ফাইল ছাড়াই একই পথ দেখাতে পারে।

কোড: `GeoExercise.cs` import ও shortest path, `GeographyDefinition.cs` validation, `FleetSession.cs` চলাচল, `RunViewModel.cs` marker position। এরপর [Lesson 17](17-database-release.bn.md)।

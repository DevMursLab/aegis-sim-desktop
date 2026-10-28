# কাজের খাতা: কী বানানো হয়েছে, কীভাবে শিখবে

শেষ হালনাগাদ: ২৮ অক্টোবর ২০২৬। Desktop 3.3.0 / engine 3.2.0।

এটি repository ও এই session-এ যাচাই করা কাজের feature-ভিত্তিক সারাংশ। এটি প্রতিটি পুরোনো edit-এর পূর্ণ ইতিহাস নয়; exact পরিবর্তন জানতে Git history দেখবে। **কোড তৈরি হয়েছে মানেই তুমি শিখে ফেলেছ—এমন নয়।** শেখার ঘরগুলো নিজের অনুশীলন শেষে পূরণ করবে।

## অ্যাপের কাজের ক্রম

```text
Scenario → Session → Command → Rules/state change
                         ↓
                 Verified history → SQLite commit
                         ↓
                 ViewModel → WPF screen

Saved commands → Replay → Reconstructed state → Comparison/report
```

এটি ধারণা বোঝানোর সরল চিত্র। Local command ও host/client path-এর বিস্তারিত [architecture](../architecture.md)-এ আছে।

## কী করা হয়েছে এবং কোথায় পড়বে

| কাজ | কেন দরকার | কোড / পাঠ | নিজে যা পরীক্ষা করবে |
|---|---|---|---|
| Tick engine, Start/Pause/Step/Reset | প্রতিটি logical step-এ নির্দিষ্ট নিয়মে state বদলানো | [SimulationSession.cs](../../src/Aegis.Simulation/SimulationSession.cs), [পাঠ 03](03-tick-engine.bn.md) | একবার Step দিলে একটি tick বাড়ে; completed run আর এগোয় না |
| OOP ও MVVM, bound controls | UI ও simulation-এর দায়িত্ব আলাদা রাখা | [RunViewModel.cs](../../src/Aegis.Presentation/RunViewModel.cs), [পাঠ 04](04-wpf-mvvm.bn.md) | Step-এর পরে property থেকে screen কীভাবে বদলায় অনুসরণ করো |
| Offline map, pan/zoom, selection | গাড়ি/shelter-এর state দেখা | [SyntheticMap.cs](../../src/Aegis.Desktop.Wpf/SyntheticMap.cs), [পাঠ 05](05-offline-map.bn.md) | Zoom করে shelter select করো; coordinates ও viewport আলাদা করো |
| SQL save ও transaction | app বন্ধ হলেও history রাখা | [RunDatabase.cs](../../src/Aegis.Infrastructure/RunDatabase.cs), [পাঠ 06](06-sql-history.bn.md) | Run save করে app বন্ধ/খুলে একই history দেখো |
| Replay ও checksum | commands থেকে একই state ফিরে পাওয়া ও inconsistent history ধরা | [RunHistory.cs](../../src/Aegis.Simulation/RunHistory.cs), [পাঠ 07](07-replay.bn.md) | Replay tick বদলাও; original run edit হয় না বুঝিয়ে বলো |
| Local controller/observer | host-এর state দুই client-এ দেখানো | [NetworkClient.cs](../../src/Aegis.Desktop.Wpf/NetworkClient.cs), [পাঠ 08](08-multiplayer.bn.md) | Observer কেন command দিতে পারে না; disconnect হলে কী হয় দেখো |
| PDF/HTML/CSV, comparison | একই verified run-এর ফল export করা | [Reports.cs](../../src/Aegis.Infrastructure/Reports.cs), [পাঠ 09](09-release.bn.md) | Baseline 6 ticks ও closure 8 ticks তুলনা করো; দুইটিতেই 48 packets |
| Finite stock, cargo, return, restock | supply সীমিত রাখা ও delivery/travel আলাদা করা | [পাঠ 10](10-stock-travel.bn.md) | Stock + travel demo-তে tick 16 shortage, Restock +20-এর পরে tick 24 completion |
| GeoJSON viewer | bounded geography import ও inspect করা | [পাঠ 11](11-geojson.bn.md) | Viewer-এর layer load কেন simulation route নিজে বদলায় না বোঝাও |
| Background preview ও diagnostics | মূল history না বদলে continuation হিসাব ও profiling | [পাঠ 12](12-preview-diagnostics.bn.md) | Preview শেষে মূল run-এর tick/history একই থাকে |
| Scenario editor | validated draft থেকে আলাদা run বানানো | [পাঠ 13](13-scenario-editor.bn.md) | Invalid capacity reject করো, valid scenario save/reload করো |
| একাধিক গাড়ি ও reservation | shared demand/stock দুবার allocate না করা | [FleetSession.cs](../../src/Aegis.Simulation/FleetSession.cs), [পাঠ 14](14-multiple-vehicles.bn.md) | Three-vehicle demo-তে cargo + depot stock + delivered হিসাব মেলাও |
| Priority dispatch | eligible higher-priority demand আগে serve করা | [পাঠ 15](15-priority-dispatch.bn.md) | Limited-stock demo-তে C কেন আগে supply পায় দেখাও |
| Road-derived travel | imported road distance থেকে path ও logical duration বের করা | [পাঠ 16](16-road-network.bn.md) | Road demo default mode-এ 30 packets, সব গাড়ি ফিরে tick 14 completion |
| Database upgrade ও packaging | পুরোনো history রাখা, SDK ছাড়া চালানো | [পাঠ 17](17-database-release.bn.md) | Package-এর manifest, checksum, launchers ও demo-data খুঁজে দেখো |
| Background replay | দ্রুত timeline seek-এ পুরোনো result যেন নতুনটি ঢেকে না দেয় | [পাঠ 18](18-background-replay.bn.md) | কয়েকবার seek করে শেষ requested tick দেখাচ্ছে কি না পরীক্ষা করো |
| Shared road closure | বন্ধ edge বাদ দিয়ে নতুন dispatch path বেছে নেওয়া | [RoadRouting.cs](../../src/Aegis.Simulation/RoadRouting.cs), [পাঠ 19](19-dynamic-roads.bn.md) | road-01 বন্ধ করে road-99 detour দেখো |
| Junction routing | চলন্ত গাড়িকে reached node থেকে open path বেছে নিতে দেওয়া | [JunctionRouting.cs](../../src/Aegis.Simulation/JunctionRouting.cs), [পাঠ 20](20-junction-routing.bn.md) | Mode enable → 3 steps → road-03 close; diversion ও cargo conservation দেখো |
| Synthetic forecast lab | training/holdout আলাদা রেখে ছোট learning experiment | [ForecastLab.cs](../../src/Aegis.Simulation/ForecastLab.cs) | Tools থেকে lab খুলে model ও baseline MAE পড়ো; বাস্তব accuracy দাবি করবে না |
| Report cancellation ও playback speed | export থামানো এবং history না বদলে pacing বদলানো | [DesktopWorkspace.cs](../../src/Aegis.Desktop.Wpf/DesktopWorkspace.cs), [RunViewModel.cs](../../src/Aegis.Presentation/RunViewModel.cs) | 1x/4x-এ একই outcome; cancelled export-এর পুরোনো files অক্ষত |

## এই session-এর delivery কাজ

- [x] রঙিন README banner, actual screenshot gallery, badges ও architecture diagram।
- [x] [Reviewer guide](../REVIEWER-GUIDE.md), [interview preparation](../INTERVIEW-PREP.bn.md), [project status](../PROJECT-STATUS.md)।
- [x] নতুন 3.3.0 package-এ source commit/version manifest ও SHA-256 sidecar যোগ।
- [x] [package.ps1](../../scripts/package.ps1)-এ nonempty output folder ও existing archive reject করা, যাতে stale file মিশে না যায়।
- [x] [verify-package.py](../../scripts/verify-package.py): ZIP/checksum, isolated extraction, fresh/seeded startup, packaged tools, custom-directory install ও host health যাচাই।
- [x] সম্পূর্ণ local verification: zero-warning/error Release build, 212 console checks, 3 lessons ও WPF/reconnect checks; binding error নেই।
- [x] GitHub Actions পাস; source commit `7dd3129`-এর [3.3.0 Release](https://github.com/DevMursLab/aegis-sim-desktop/releases/tag/v3.3.0) প্রকাশ, সঙ্গে ZIP/checksum/verification JSON।

Release-টি বানানোর পরে শুধু এই শেখার নোট যোগ করা হয়েছে। ভবিষ্যতের documentation edit দিয়ে পুরোনো ZIP-এর contents বদলে গেছে ধরে নেবে না।

Repository visibility আলাদা setting। এই নোট লেখার সময়ে repository Private; Public করতে তোমার উত্তর চাওয়া হয়েছে, visibility বদলানো হয়নি। বর্তমান setting GitHub-এ যাচাই করবে।

## তুমি কোন ক্রমে শিখবে

1. **C# basics ও OOP:** পাঠ 01–03। Code চালানোর আগে output অনুমান করো।
2. **একটি সম্পূর্ণ flow:** পাঠ 04 → 06 → 07; Step button থেকে SQL ও replay পর্যন্ত অনুসরণ করো।
3. **নিজস্ব simulation logic:** পাঠ 10 → 14 → 15; stock, reserved cargo, delivery, return আঁকো।
4. **Routing:** পাঠ 16 → 19 → 20; assigned-trip mode ও junction mode-এর পার্থক্য বোঝো।
5. **UI responsiveness ও validation:** পাঠ 12 → 13 → 18।
6. **Networking ও release:** পাঠ 08 → 09 → 17; তারপর package verifier পড়ো।
7. **ঐচ্ছিক forecast:** core বোঝার পরে training, holdout, baseline, MAE ও limitations শেখো।

দিন গুনে চাপ নেওয়ার প্রয়োজন নেই। প্রতিটি ধাপে একটি feature নিজে চালাবে, কোডে খুঁজবে, একটি ছোট exercise করবে এবং নিজের ভাষায় ব্যাখ্যা লিখবে।

## নিজের শেখার checklist

- [ ] Variable, loop, class/object এবং method বুঝিয়ে বলতে পারি।
- [ ] Step-এর আগে/পরে state কাগজে লিখে মিলাতে পারি।
- [ ] MVVM ও binding-এর একটি বাস্তব example দেখাতে পারি।
- [ ] Depot stock, reserved cargo ও delivered-এর পার্থক্য বুঝি।
- [ ] SQL save এবং replay reconstruction আলাদা করে ব্যাখ্যা করতে পারি।
- [ ] Dijkstra ও junction rerouting ছোট graph-এ বুঝিয়ে বলতে পারি।
- [ ] Controller/observer এবং duplicate command handling বুঝি।
- [ ] একটি failing/invalid case চালিয়ে কেন reject হলো বলতে পারি।
- [ ] নিজের CV-র প্রতিটি project claim কোড বা demo দিয়ে সমর্থন করতে পারি।

এই ঘরগুলো তোমার হয়ে পূরণ করা হয়নি।

## পরের কাজের নোটের নিয়ম

পরবর্তী implementation-এর সময় এই খাতায় নিচের তথ্য যোগ করতে হবে। কাজের সারাংশ release notes-এ থাকবে; beginner ব্যাখ্যা এখানে থাকবে।

```text
তারিখ / version:
সমস্যা বা চাহিদা:
কী বদলেছে এবং কেন:
কোন file/class/method:
আগে বনাম এখন ছোট example:
কীভাবে পরীক্ষা করা হয়েছে:
নিজে শেখার exercise:
সীমাবদ্ধতা / বাকি কাজ:
```

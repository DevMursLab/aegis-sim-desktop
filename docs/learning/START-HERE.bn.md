# এখান থেকে শুরু করো

এই repository-তে শেখার কাজ ও মূল অ্যাপের অগ্রগতি আলাদা করে লেখা থাকবে। তিনটি console lesson ও একটি portfolio MVP প্রস্তুত: desktop map, incident, SQLite history, replay, local multiplayer ও report। Full GIS/AI/production deployment এই version-এর scope নয়।

## প্রতিদিনের পদ্ধতি

1. আজকের ছোট ধারণা পড়ো।
2. কোড চালানোর আগে ফল কাগজে লেখো।
3. কোড চালাও এবং নিজের অনুমানের সঙ্গে মেলাও।
4. একটি জিনিস বদলে আবার চালাও।
5. কেন ফল বদলেছে নিজের ভাষায় লেখো।
6. ভুল হলে error-এর প্রথম বার্তা থেকে কারণ খোঁজো।

শুধু কপি করে চালানো শেষ করা নয়। প্রতিটি lesson-এর অনুশীলন পারলে পরের ধাপে যাবে। নির্দিষ্ট দিনের মধ্যে সব শেষ করার চাপ নেই।

## শেখার পথ

| ধাপ | কী শিখবে | হাতে কী থাকবে | অবস্থা |
|---|---|---|---|
| 01 | variable, condition, loop, Math.Min | এক আশ্রয়কেন্দ্রে ত্রাণ পৌঁছানোর console lesson | প্রস্তুত; নিজের বোঝা যাচাই করবে |
| 02 | class, object, List, foreach | এক গাড়ি দিয়ে একাধিক shelter-এ এক round delivery | প্রস্তুত; তোমার অনুশীলন বাকি |
| 03 | Step method, bool, private set, executable checks | একই input-এ একই ফলের mini-engine | প্রস্তুত; তোমার অনুশীলন বাকি |
| 04 | XAML, binding, commands, MVVM | Start/Pause/Resume/Step/Reset সহ WPF window | প্রস্তুত; তোমার অনুশীলন বাকি |
| 05 | local coordinates, zoom/pan, selection | native WPF offline synthetic map | প্রথম সংস্করণ প্রস্তুত; GIS import পরে |
| 06 | SQL, persistence, transactions | সংরক্ষিত run history | code ও পাঠ প্রস্তুত |
| 07 | events, snapshots, replay | আগের state পুনরায় তৈরি | code ও পাঠ প্রস্তুত |
| 08 | SignalR, sequence, reconnect | দুই client-এর একই run | code ও পাঠ প্রস্তুত |
| 09 | metrics, reporting, packaging | demo ও Windows package | code ও পাঠ প্রস্তুত |
| 10 | stock, cargo, vehicle phase, travel ticks | সীমিত মজুত ও যাত্রার simulation | code ও পাঠ প্রস্তুত |
| 11 | GeoJSON, geometry, coordinate system | offline layer viewer ও feature inspection | code ও পাঠ প্রস্তুত |
| 12 | background task, continuation, diagnostics | আলাদা copy-তে preview ও measured profiling | code ও পাঠ প্রস্তুত |
| 13 | draft model, validation, input binding | app-এর ভেতরে scenario editor | code ও পাঠ প্রস্তুত |
| 14 | shared stock, reserved demand, deterministic scheduling | একসঙ্গে ১–৮টি গাড়ি | code ও পাঠ প্রস্তুত |

Testing ধাপ 03 থেকে নিয়মিত থাকবে; একদম শেষে যোগ করার বিষয় নয়। AI forecast মূল অ্যাপ স্থিতিশীল হলে ঐচ্ছিক।

নতুন ধাপ: [Lesson 10 — মজুত ও যাত্রা](10-stock-travel.bn.md)। Desktop-এর **Stock + travel demo** দিয়ে শিখবে। আগে stock, cargo আর delivered-এর পার্থক্য বুঝবে; তারপর Step দিয়ে ফল মিলাবে।

তারপর [Lesson 11 — GeoJSON](11-geojson.bn.md) এবং [Lesson 12 — Preview ও diagnostics](12-preview-diagnostics.bn.md)। Tools menu থেকে নতুন screen খুলবে।

নিজের exercise বানাতে [Lesson 13 — Scenario editor](13-scenario-editor.bn.md) পড়ো।

তারপর [Lesson 14 — একাধিক গাড়ি](14-multiple-vehicles.bn.md)। Tools থেকে Three-vehicle demo চালাও।

## আজকের কাজ

VS Code-এ এই project folder খুলে Terminal → New Terminal নাও। repository root থেকে চালাও:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-lesson.ps1
```

এটি শুধু এই script চালানোর process-এ execution policy নির্ধারণ করে; স্থায়ী system setting বদলায় না।

তারপর [প্রথম lesson](01-supply-ticks.bn.md) পড়ো। অনুশীলনের উত্তর `docs/learning/my-notes/lesson-01.md` ফাইলে রাখতে পারো। নিজের লেখা কোড ও উত্তর review হওয়ার পরে শেখার অগ্রগতি update হবে।

পরের পাঠ: [Lesson 02 — class, object ও List](02-shelter-objects.bn.md)। চালাতে একই command-এর শেষে `-Lesson 02` যোগ করো।

তার পরের পাঠ: [Lesson 03 — tick engine](03-tick-engine.bn.md)। চালাতে `-Lesson 03`; নিয়ম যাচাই করতে সঙ্গে `-Check` যোগ করো।

নতুন পাঠ: [Lesson 04 — WPF ও MVVM](04-wpf-mvvm.bn.md)। window খুলতে `-Lesson 04`; পুরো repository যাচাই করতে `powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1` চালাও।

মানচিত্রের পাঠ: [Lesson 05 — offline map](05-offline-map.bn.md)। `-Lesson 05` দিয়ে একই desktop app খুলে map selection ও zoom/pan শেখো।

তারপর ক্রমে পড়ো: [06 — SQL history](06-sql-history.bn.md), [07 — Replay](07-replay.bn.md), [08 — Multiplayer](08-multiplayer.bn.md), [09 — Report ও release](09-release.bn.md)। প্রথম তিনটি lesson স্বাধীন console project; পরের পাঠগুলো একই updated app-এর অংশ ধরে বোঝায়।

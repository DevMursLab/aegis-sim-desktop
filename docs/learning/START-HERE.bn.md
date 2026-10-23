# এখান থেকে শুরু করো

এই repository-তে শেখার কাজ ও মূল অ্যাপের অগ্রগতি আলাদা করে লেখা থাকবে। এখন তিনটি console lesson প্রস্তুত; মূল WPF অ্যাপ এখনো তৈরি হয়নি।

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
| 04 | XAML, binding, commands, MVVM | Start/Pause সহ WPF window | পরিকল্পিত |
| 05 | coordinates, layers, selection | offline synthetic map | পরিকল্পিত |
| 06 | SQL, persistence, transactions | সংরক্ষিত run history | পরিকল্পিত |
| 07 | events, snapshots, replay | আগের state পুনরায় তৈরি | পরিকল্পিত |
| 08 | SignalR, sequence, reconnect | দুই client-এর একই run | পরিকল্পিত |
| 09 | metrics, reporting, packaging | demo ও release | পরিকল্পিত |

Testing ধাপ 03 থেকে নিয়মিত থাকবে; একদম শেষে যোগ করার বিষয় নয়। AI forecast মূল অ্যাপ স্থিতিশীল হলে ঐচ্ছিক।

## আজকের কাজ

VS Code-এ এই project folder খুলে Terminal → New Terminal নাও। repository root থেকে চালাও:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-lesson.ps1
```

এটি শুধু এই script চালানোর process-এ execution policy নির্ধারণ করে; স্থায়ী system setting বদলায় না।

তারপর [প্রথম lesson](01-supply-ticks.bn.md) পড়ো। অনুশীলনের উত্তর `docs/learning/my-notes/lesson-01.md` ফাইলে রাখতে পারো। নিজের লেখা কোড ও উত্তর review হওয়ার পরে শেখার অগ্রগতি update হবে।

পরের পাঠ: [Lesson 02 — class, object ও List](02-shelter-objects.bn.md)। চালাতে একই command-এর শেষে `-Lesson 02` যোগ করো।

তার পরের পাঠ: [Lesson 03 — tick engine](03-tick-engine.bn.md)। চালাতে `-Lesson 03`; নিয়ম যাচাই করতে সঙ্গে `-Check` যোগ করো।

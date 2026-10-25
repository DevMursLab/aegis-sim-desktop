# Lesson 18 — app আটকে না রেখে replay

WPF-এর UI thread button, map ও window আঁকে। একই thread-এ বড় history যাচাই করলে ওই সময়ে click বা redraw দেরি হয়। তাই হিসাব background worker-এ চালাচ্ছি:

```csharp
var result = await Task.Run(() => RunHistory.Replay(saved, tick, token));
```

`Task.Run` কাজ worker-এ পাঠায়। `await` অপেক্ষার সময়ে UI thread ছেড়ে দেয়; WPF context থেকে শুরু করলে পরে UI update আবার সেই context-এ হয়। Worker থেকে সরাসরি control বা bound collection বদলাবে না।

কিন্তু তুমি tick 3 বেছে তারপর সঙ্গে সঙ্গে tick 5 চাইলে? আগের কাজ পরে শেষ হয়ে tick 3 দেখাতে পারে। তাই নতুন request আগের cancellation token cancel করে এবং publish করার আগে কোন request সর্বশেষ তা মেলায়। Engine প্রতিটি command-এর আগে cancellation পরীক্ষা করে। Reset বা নতুন scenario-ও pending replay বাতিল করে।

Saved history window-এ list, SQL validation ও comparison worker-এ হয়। Window বন্ধ হলে ফল discard হয়; ইতিমধ্যে শুরু হওয়া SQL কাজ শেষ হতে পারে। Corrupt history হলে আগের ভালো display রাখা হয়।

পরীক্ষা করো: একটি completed run replay-তে খোলো, slider দ্রুত ১, ৩, ৫-এ নাও। শেষে tick 5 দেখাবে, Step বন্ধ থাকবে। Reset করলে নতুন live run থাকবে। কোড দেখো `ReplayInBackground`, `CancelReplayWork` এবং `RunHistory.Replay`।

এতে সব কাজ asynchronous হয়ে যায়নি। Live command save, report export ও final map/event update এখনও UI thread-এ হয়। Responsiveness আর throughput আলাদা বিষয়—এখনকার পরিবর্তনটি history খোলা ও seek করার responsiveness নিয়ে।

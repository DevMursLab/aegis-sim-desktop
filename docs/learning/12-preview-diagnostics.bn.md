# Lesson 12 — আগে ফল দেখা, তারপর নিজের run চালানো

**Tools → Completion preview** চাপো। নতুন incident বা restock না দিলে বর্তমান নিয়মে run কোথায় গিয়ে থামবে দেখাবে।

এটি AI নয়। তোমার run-এর একটি আলাদা copy নিয়ে একই engine চালানো হয়। তাই অনুমানগুলো একই থাকলে ফল সরাসরি মিলিয়ে যাচাই করা যায়। মূল history-তে কোনো command যোগ হয় না। Background task-এ হিসাব হয়; window বন্ধ করলে cancellation চাওয়া হয়।

Stock demo-তে শুরুতেই preview দেখো: tick ১৬, SupplyExhausted, আরও ১৮ packet দরকার। এরপর নিজের run-এ এগিয়ে ওই হিসাব মিলাও। Restock +20 দেওয়ার পর preview-তে tick ২৪ আর stock ২ দেখাবে।

Replay-তে preview খুললে slider-এর tick থেকে continuation দেখবে। ওই tick-এর পরের পুরোনো commandগুলো ভবিষ্যৎ intervention হিসেবে যোগ হবে না। কোন command slider-এর boundary-তে পড়ে, তা আগের replay lesson-এর একই নিয়ম মেনে হয়।

## Diagnostics কী

**Tools → Export diagnostics** থেকে JSON save করো। Engine/runtime version, run ID, tick, command count, verified state hash, replay-এর একবারের সময় ও process memory পাওয়া যায়। Token, scenario name, command payload বা database path দেওয়া হয় না। এটি full selected run-এর snapshot; slider-এর আগের অবস্থার snapshot নয়।

একবারের timing মানেই benchmark নয়। নিজে profile করতে:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\profile.ps1
```

এটি তিনবার warmup করে ১৫টি sample নেয়। প্রতি sample-এ ১০০০ engine tick অথবা ১০০০ command replay হয়। `artifacts/performance.json`-এ নিজের machine-এর ফল থাকে। UI, SQL বা multiplayer-এর speed এই সংখ্যা দিয়ে দাবি করবে না।

নিজে বোঝাও: preview কেন exact model calculation হলেও বাস্তব relief operation-এর নিশ্চয়তা নয়? নতুন closure দিলে পুরোনো preview-এর ফল কেন বদলাতে পারে?

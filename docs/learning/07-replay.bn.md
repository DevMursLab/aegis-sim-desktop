# Lesson 07: পুরোনো হিসাব আবার বানানো

Replay মানে আগের screen-এর ছবি চালানো নয়। Saved scenario ও ordered command আবার প্রয়োগ করে সেই tick-এর state তৈরি করা।

`RunHistory.Replay` শুরু থেকে command পড়ে। প্রতিটি command-এর পরে state-এর SHA-256 hash আগের সংরক্ষিত hash-এর সঙ্গে মেলায়। অমিল হলে run-কে সঠিক বলে দেখায় না। Database load-এর সময় SQL command, typed event ও checkpoint-ও মিলিয়ে দেখা হয়।

## Screen-এ দেখো

1. Saved runs / A/B খুলে একটি run বেছে Open replay চাপো।
2. নিচের timeline slider tick ০ থেকে শেষ পর্যন্ত সরাও।
3. Map, table, inspector ও event history একই tick দেখছে কি না দেখো।
4. Start/Step/incident বন্ধ থাকবে। Reset করলে একই scenario থেকে নতুন writable run তৈরি হবে।

Stored checkpoint আছে, তবে এই ছোট version-এ seek command prefix replay করেই হয়; checkpoint দিয়ে দ্রুত seek করার optimization এখনো নেই।

## Incident পরীক্ষা

A নির্বাচন করে Close selected route চাপলে পাঁচ tick ওই কেন্দ্রে delivery যাবে না। অন্য কেন্দ্র খোলা থাকলে সেখানে যাবে; সব pending route বন্ধ থাকলে Waiting event হবে। এটি geographic shortest-path হিসাব নয়, একটি পরিষ্কার availability rule।

Seeded baseline ৬ tick, closure run ৮ tick। দুটোতেই ৪৮ প্যাকেট যায়। Saved runs-এ Ctrl দিয়ে দুইটি একই scenario-র run select করে Compare করো। Baseline হিসেবে প্রথম selected run ব্যবহার হয়।

নিজের ভাষায় বলো: শুধু “গাড়ি চলল” লেখা log দিয়ে কি পুরো state বানানো যাবে? কেন command-এর order ও engine version রাখতে হয়? Reset কেন পুরোনো history বদলায় না?

পরের পাঠ: [Multiplayer](08-multiplayer.bn.md)।

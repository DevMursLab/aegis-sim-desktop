# Lesson 06: app বন্ধ হলেও তথ্য থাকবে

আগে তথ্য শুধু memory-তে ছিল। এখন প্রতিটি valid command-এর পরে SQLite file-এ save হয়। Default file: `%LOCALAPPDATA%\AEGIS-SIM\local\runs.db`। আলাদা database server লাগবে না।

## কীভাবে কাজ করে?

1. পুরোনো run থেকে command-এর সম্ভাব্য ফল তৈরি হয়।
2. input ও checksum যাচাই হয়।
3. একটি SQL transaction-এ run, command, event ও checkpoint লেখা হয়।
4. commit সফল হলে screen-এ নতুন state দেখায়।

Transaction মানে সম্পর্কিত লেখাগুলো একসঙ্গে সফল হবে অথবা rollback হবে। save ব্যর্থ হলেও screen-এ সফল delivery দেখানো যাবে না। Integration check-এ ইচ্ছা করে storage failure দিয়ে এটি পরীক্ষা করা আছে।

`Aegis.Infrastructure/RunDatabase.cs`-এ EF Core database access আছে। `RunRepository.Save` পুরো flow-টি চালায়। ViewModel EF Core জানে না; তার `Persist` callback App repository-র সঙ্গে যুক্ত করে।

## চারটি table

| Table | কী রাখে |
|---|---|
| Runs | scenario, run ID, version, status ও সম্পূর্ণ recorded document |
| Commands | ordered command, unique command ID ও checksum |
| Events | প্রতি tick-এর delivery/waiting ফল |
| Snapshots | নির্দিষ্ট sequence-এর state ও hash |

প্রথম সংস্করণে schema bootstrap এবং একটি additive development upgrade আছে। ভবিষ্যতের জটিল schema পরিবর্তনের জন্য migration ও backup plan দরকার হবে।

## নিজের কাজ

Step দুবার চাপো, app বন্ধ করো, আবার খোলো। সর্বশেষ run read-only replay-তে এসেছে কি? Reset করলে নতুন run ID হয় কি? Saved runs-এ আগের run আছে কি?

নিজের ভাষায় বলো: save-এর আগে screen বদলানো কেন ভুল হতে পারে? `RunId` আর `CommandId` কেন আলাদা?

পরের পাঠ: [Replay](07-replay.bn.md)।

# Lesson 09: পরীক্ষা, report ও reviewer-এর হাতে দেওয়া

## সব পরীক্ষা

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

৪৯টি console check, সব পুরোনো console lesson, Release build ও actual WPF flow চলে। দুই window/host restart-এর পরীক্ষাও আছে। CI-তে interactive screen বাদ যায়; SQL/SignalR process checks চলে। Test executable-গুলো xUnit suite নয়, তাই `dotnet test` মূল verification command নয়।

## Report

Export report দিয়ে folder নির্বাচন করলে PDF, HTML ও CSV তৈরি হয়। এগুলো selected saved document-এর সম্পূর্ণ run-এর হিসাব, slider-এর শুধু বর্তমান frame নয়। PDF English/ASCII labels দেখায়; Unicode নামের জন্য HTML/CSV দেখবে।

- Completion: delivered / requested।
- Mean service tick: প্রতিটি পৌঁছানো প্যাকেটের delivery tick-এর weighted average। সব demand tick ০-তে তৈরি ধরে নিয়েছি। এটি বাস্তব waiting minute নয়।
- Active ticks: delivery হয়েছে এমন tick / elapsed ticks।
- Waiting ticks: কিছু চাহিদা ছিল কিন্তু কোনো pending route খোলা ছিল না।

## Package

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

Windows x64 runtime-সহ ZIP তৈরি হয়। reviewer extract করে `.cmd` launcher চালাতে পারে; SDK লাগে না। Seeded demo-তে দুইটি completed run, scenario JSON/XML ও sample report থাকে। Optional Install.ps1 current user-এর Programs folder-এ copy করে shortcut বানায়; administrator লাগে না।

## তোমার দায়িত্ব

Demo চালানোর আগে [তিন মিনিটের script](../demo-script.md) অনুশীলন করো। প্রত্যেক গুরুত্বপূর্ণ class-এর দায়িত্ব বুঝে নিজের ভাষায় বলবে। নিজের বোঝা যাচাই না করে code প্রস্তুত হওয়াকে skill mastery ধরবে না।

Known limits বলবে: কাল্পনিক map, প্রতিবার fresh load, travel-time physics নেই, loopback-only network, ছোট history-র synchronous replay, signed installer/AI/full GIS নেই। সত্যি working feature ও পরীক্ষার প্রমাণ দেখানোই review-এর ভিত্তি।

# Lesson 08: দুই screen, এক authoritative engine

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\start-multiplayer.ps1
```

একটি local host এবং দুইটি desktop window খুলবে। Controller নির্দেশ দিতে পারে; Observer দেখে। Network mode-এ client-এর timer core simulation চালায় না; host-এর timer চালায়।

## একটি command-এর পথ

Controller → command ID + expected version → host role/version validation → candidate result → SQL commit → State broadcast → দুই screen update।

- **Command ID:** একই নির্দেশ আবার পৌঁছালেও দুবার প্রয়োগ ঠেকায়।
- **Run version:** যে পুরোনো state দেখে command দেওয়া হয়েছিল সেটি এখনো বর্তমান কি না যাচাই করে।
- **Room revision:** client যেন পুরোনো broadcast দিয়ে নতুন screen state ঢেকে না ফেলে।

SignalR realtime communication দেয়; correctness-এর এই নিয়মগুলো application-এ নিজেরা লিখেছি।

## Connection বন্ধ হলে

STALE / RECONNECTING দেখায়, controller write বন্ধ থাকে। Connection ফিরলে client পুরো authoritative snapshot নেয়। Host restart হলে SQLite থেকে saved run ফিরে আসে। অনেকক্ষণ host বন্ধ থাকলে retry শেষ হতে পারে; তখন client window আবার খুলবে।

এই demo একটি same-machine room। Host loopback ছাড়া connection নেয় না। Local secrets file-এ আলাদা controller/observer token থাকে। Observer button disabled হওয়ার পাশাপাশি server-ও write reject করে। Secrets GitHub-এ দেবে না। Internet authentication/TLS/multi-tenant deployment এই version-এর কাজ নয়।

## নিজে পরীক্ষা

Controller-এ A-এর route বন্ধ করো। Observer-এ একই লাল marker দেখো। Observer দিয়ে Step/Reset করা যাচ্ছে কি? Controller Reset দিলে দুই screen-এ নতুন run এসেছে কি?

Automated WPF check সত্যিকারের দুই window খুলে host kill/restart করে automatic reconnect পরীক্ষা করে। Source script শেষের host PID দেখায়; demo শেষে সেই host process বন্ধ করবে। Packaged launcher-এ Enter চাপলে নিজের শুরু করা host বন্ধ হয়।

পরের পাঠ: [Report ও release](09-release.bn.md)।

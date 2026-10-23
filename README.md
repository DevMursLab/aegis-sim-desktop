# AEGIS-SIM 2.0

কাল্পনিক দুর্যোগে ত্রাণ বিতরণের Windows desktop simulator শেখা ও তৈরির প্রজেক্ট।

## বর্তমান অবস্থা

- দুটি বাংলা পরিকল্পনার PDF আছে।
- প্রথম runnable C# console lesson প্রস্তুত; মূল WPF অ্যাপ এখনো তৈরি হয়নি।
- মূল নির্দেশনা: `main/AEGIS_SIM_2_Desktop_GIS_WPF_MVVM_Blueprint_Bangla (1).pdf`।
- `partial/`-এর PDF আগের web/Blazor পরিকল্পনা; desktop পরিকল্পনাই অনুসরণ করা হবে।

## পরিকল্পিত প্রযুক্তি

C#, .NET, WPF, MVVM, offline synthetic maps, EF Core/SQLite এবং পরবর্তী ধাপে SignalR।

## শেখা ও তৈরির ক্রম

1. C# basics ও ছোট console exercise।
2. OOP, domain models ও simulation rules।
3. Deterministic tick engine ও automated tests।
4. WPF, MVVM ও মানচিত্র।
5. Database persistence ও replay।
6. দুই client-এর synchronization, analytics ও report।

প্রতিটি ধাপে ধারণা বোঝা, ছোট পরিবর্তন লেখা, চালানো, যাচাই এবং ব্যাখ্যা করার অনুশীলন থাকবে।

## Development setup

.NET 10 SDK লাগবে। এই কম্পিউটারের user-local SDK থাকলে lesson script সেটি ব্যবহার করবে; অন্য কম্পিউটারে .NET 10 SDK install করতে হবে।

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-lesson.ps1
```

শুরু করো: [বাংলা learning guide](docs/learning/START-HERE.bn.md)।

প্রথম পাঠ: [ত্রাণ বিতরণ দিয়ে C#](docs/learning/01-supply-ticks.bn.md)।

## Scope

শুধু কাল্পনিক civilian disaster-response training ও portfolio learning project। কোনো প্রতিষ্ঠানের অভ্যন্তরীণ সামরিক সফটওয়্যারের specification বা বাস্তবায়ন দাবি করা হচ্ছে না।

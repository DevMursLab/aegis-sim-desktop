# AEGIS-SIM 2.0

কাল্পনিক দুর্যোগে ত্রাণ বিতরণের Windows desktop simulator শেখা ও তৈরির প্রজেক্ট।

## বর্তমান অবস্থা

- দুটি বাংলা পরিকল্পনার PDF আছে।
- অ্যাপ্লিকেশনের কোড এখনো তৈরি হয়নি।
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

.NET SDK লাগবে। প্রাথমিক পরীক্ষায় কম্পিউটারে .NET runtime পাওয়া গেছে, কিন্তু SDK পাওয়া যায়নি। SDK স্থাপনের পরে `dotnet --list-sdks` দিয়ে যাচাই করতে হবে। অ্যাপের build/run নির্দেশনা কোড তৈরির সঙ্গে যোগ হবে।

## Scope

শুধু কাল্পনিক civilian disaster-response training ও portfolio learning project। কোনো প্রতিষ্ঠানের অভ্যন্তরীণ সামরিক সফটওয়্যারের specification বা বাস্তবায়ন দাবি করা হচ্ছে না।

# Lesson 05: হিসাবকে মানচিত্রে দেখাও

এবার shelter-এর একই তথ্য map ও table-এ দেখবে। Map-এর জন্য আলাদা fake delivery হিসাব নেই। মূল simulation বদলালে দুই জায়গার তথ্য একসঙ্গে বদলায়।

## চালাও

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-lesson.ps1 -Lesson 05
```

Lesson 04-ও এই একই updated desktop app খোলে; console lesson-গুলো অপরিবর্তিত।

1. Map-এর Shelter-A-তে click করো; ডান পাশে ২৫ remaining দেখবে।
2. **Step** চাপো। map, inspector ও table—সবখানে ১৫ remaining হবে।
3. Mouse wheel দিয়ে zoom, drag করে pan করো। **Fit map** চাপলে মূল view ফিরে আসবে।
4. **Start → Pause → Resume** করে delivery দেখো। সব চাহিদা পূরণ হলে সব shelter সবুজ হবে।
5. **Reset** করলে নতুন run-এ চাহিদা ফিরবে এবং তার event feed খালি হবে; আগের saved run অক্ষত থাকে। map-এর zoom আলাদা visual setting; Fit map দিয়ে সেটি reset করবে।

## Map-এর জিনিসগুলো কী?

- গাঢ় গোল marker: কিছু চাহিদা বাকি।
- সবুজ গোল marker: ওই shelter-এর চাহিদা পূরণ হয়েছে।
- ring: বর্তমানে নির্বাচিত shelter।
- depot-এর square: কাল্পনিক reload point।
- amber square ও link: শেষ delivery কোন shelter-এ হয়েছে তার চিহ্ন। এটি চলন্ত গাড়ির animation নয়।
- নদী ও সংযোগগুলো শুধু আঁকা synthetic background; এগুলোর ওপর এখন কোনো route calculation বা delivery delay নেই।

## Coordinates: x আর y

Map একটি কাল্পনিক ১২০০ × ৮০০ meter-এর local plane। `x` ডান দিকে বাড়ে, `y` নিচের দিকে বাড়ে। এগুলো GPS বা বাস্তব এলাকার latitude/longitude নয়। Shelter-এর position এখন validated scenario JSON/XML থেকে ViewModel-এ আসে।

```csharp
new MapNode(s.Name, x, y, s.InitialDemand, s.Remaining)
```

এখানে আগের shelter-এর তথ্যের সঙ্গে position যোগ হয়েছে। `record` এই ছোট data-র একটি ছাঁচ; এই lesson-এ মূলত position আর remaining কীভাবে একসঙ্গে যাচ্ছে তা দেখো।

## World ও screen আলাদা

World position হলো map-এর meter-এর হিসাব। Screen position হলো window-র pixel/DIP-এর হিসাব। Zoom করলে shelter-এর world position বদলায় না; screen-এ কোথায় দেখাবে সেই হিসাব বদলায়।

`MapViewport.ToScreen` world থেকে screen বানায়। `ToWorld` উল্টো কাজ করে। Mouse-এর নিচের জায়গা স্থির রেখে zoom করার জন্য এই উল্টো হিসাব দরকার। `HitTest` দেখে click কোনো marker-এর ২০ screen unit-এর মধ্যে পড়েছে কি না।

## কোন file কী করে?

| File | দায়িত্ব |
|---|---|
| `src/Aegis.Presentation/MapViewport.cs` | position conversion, zoom/pan, click hit-testing |
| `src/Aegis.Presentation/RunViewModel.cs` | simulation থেকে map node ও নির্বাচিত shelter-এর তথ্য |
| `src/Aegis.Desktop.Wpf/SyntheticMap.cs` | WPF drawing ও visual gesture handling |
| `src/Aegis.Desktop.Wpf/MainWindow.xaml` | map, inspector, controls ও table layout |

View-এর code-behind-এ Zoom/Fit visual wiring আছে। ত্রাণের নিয়ম বা database কাজ সেখানে নেই। Mapping library বা GIS import এখনো যোগ হয়নি; এটি native WPF-এ প্রথম offline planar-map lesson। GeoJSON, projection ও external layer validation পরে যোগ হবে।

## Keyboard দিয়ে ব্যবহার

Map-এ click করে focus দিলে arrow keys দিয়ে pan, `+`/`-` দিয়ে zoom, `Home` দিয়ে fit করা যায়। Marker নির্বাচন করতে পাশের shelter dropdown keyboard দিয়েও ব্যবহার করা যায়। শুধুমাত্র রঙ দেখে ফল বুঝতে হয় না; remaining-এর সংখ্যাও লেখা আছে। সম্পূর্ণ accessibility audit এখনো করা হয়নি।

## নিজের অনুশীলন

1. একটি shelter select করে Step চাপলে তার remaining কীভাবে বদলায় দেখো।
2. Zoom করেও কেন delivery-র পরিমাণ বদলায় না, নিজের ভাষায় বলো।
3. sample scenario JSON-এর প্রথম x-coordinate সামান্য বদলে Import করো। শুধু position বদলায় কি না পরীক্ষা করো; পরে মূল sample আবার Import করো।
4. Map-এর zoom করা অবস্থায় marker select করে Fit চাপো। selection আর view position-এর পার্থক্য বোঝো।

## যাচাই

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

মোট ৪৯টি console check এবং actual WPF window smoke check চলে। Geometry checks coordinate round-trip, zoom anchor/limit, pan/fit ও hit-testing দেখে। Window smoke check map selection binding, inspector update, zoom/fit buttons, completion ও reset দেখে। Rendering default ও minimum window size-এ পরিদর্শন করা হয়েছে; সব DPI বা monitor পরীক্ষা করা হয়নি।

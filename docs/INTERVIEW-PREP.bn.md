# AEGIS-SIM: নিজের কাজ বুঝিয়ে বলার প্রস্তুতি

## ৩০ সেকেন্ডে পরিচয়

“আমি C#/.NET ও WPF দিয়ে একটি synthetic supply-delivery simulator তৈরি করেছি। এতে একাধিক গাড়ি shared stock থেকে supply নিয়ে priority অনুযায়ী shelter-এ পৌঁছায়। Road বন্ধ করলে route ও completion time বদলায়। SQLite-এ history save হয়, পরে replay করে একই state যাচাই করা যায়। এটি আমার নিজস্ব portfolio project; scenario-গুলো কাল্পনিক।”

নিজে বুঝে এই কথাগুলো বলবে। কোনো প্রতিষ্ঠানের বাস্তব system বানানোর অভিজ্ঞতা হিসেবে পরিচয় দেবে না। AI সহায়তা নিয়ে থাকলে নিজের ভূমিকা, শেখা, যাচাই এবং কোন অংশে সহায়তা নিয়েছ—সত্যভাবে ব্যাখ্যা করবে।

## প্রথমে যে ছয়টি বিষয় বোঝা দরকার

| প্রশ্ন | সংক্ষিপ্ত ব্যাখ্যা | কোড |
|---|---|---|
| OOP কোথায়? | Session state ও rules ধরে; scenario, vehicle, command ও event-এর আলাদা দায়িত্ব আছে। একটি class খুলে তার fields, methods ও validation দেখাও। | `src/Aegis.Simulation/SimulationSession.cs`, `FleetSession.cs` |
| MVVM কেন? | XAML view দেখায়, ViewModel property/command প্রকাশ করে, simulation rules আলাদা project-এ থাকে। | `src/Aegis.Presentation/RunViewModel.cs` |
| Stock দুবার দেওয়া ঠেকাও কীভাবে? | Dispatch-এর সময় cargo reserve হয়; arrival-এ delivery হয়; একাধিক গাড়ি একই demand পূরণের জন্য অতিরিক্ত cargo নেয় না। | `src/Aegis.Simulation/FleetSession.cs` |
| Route কীভাবে বের হয়? | Positive road length দিয়ে deterministic Dijkstra shortest path। Junction mode-এ reached node থেকে open path বেছে নেয়; entered road আগে শেষ করে। | `RoadRouting.cs`, `JunctionRouting.cs` |
| Save/replay কী? | Commands-এর ক্রম থেকে state reconstruct হয়; events/checkpoints/hash মেলানো হয়। Transactional persistence সফল হওয়ার পরে পরিবর্তন publish হয়। | `RunHistory.cs`, `src/Aegis.Infrastructure/RunDatabase.cs` |
| UI freeze কমিয়েছ কীভাবে? | Saved-history reconstruction, preview ও report export worker task-এ চলে। পুরোনো replay request-এর result নতুন state ঢেকে দিতে পারে না। Live command persistence এখনও synchronous। | `RunViewModel.cs`, `src/Aegis.Desktop.Wpf/DesktopWorkspace.cs` |

## তিন মিনিটের demo

1. Seeded demo খুলে baseline ও closure run-এর 6 বনাম 8 ticks দেখাও; দুই run-এ 48 packets delivered।
2. Tools → Three-vehicle demo: Step করে reserved cargo ও per-vehicle event দেখাও।
3. Saved runs থেকে replay খুলে timeline seek করো; report export দেখাও।
4. সময় থাকলে Road network demo-তে junction mode ও road closure দেখাও। Network/forecast শুধু বোঝাতে পারলে দেখাবে।

চালানোর পূর্ণ নির্দেশনা: [demo script](demo-script.md)। কোনো video বা rehearsal-কে live execution বলে পরিচয় দেবে না।

## কঠিন follow-up

- **একসঙ্গে দুই command এলে?** Local host authoritative; role, expected version ও command ID পরীক্ষা করে। Observer write করতে পারে না; duplicate command আবার apply হয় না।
- **Hash থাকলেই data নিরাপদ?** না। এটি inconsistent/corrupted history ধরতে সাহায্য করে; privileged attacker পুরো history নতুন করে লিখলে security guarantee দেয় না।
- **Road distance কি বাস্তব travel time?** না। Spherical distance ও configured meters-per-tick থেকে logical movement; traffic, collision বা congestion model নেই।
- **Forecast কি বাস্তব AI prediction?** এটি generated schema-2 data-তে 7-nearest-neighbour learning experiment। Training/holdout আলাদা; field-data accuracy দাবি নেই। Known run-এর জন্য exact completion preview আছে।
- **Tests কীভাবে চলে?** Console executable checks; xUnit suite নয়। `scripts/verify.ps1` locked restore, warning-free Release build, 212 checks, lessons এবং interactive desktop থাকলে WPF flows চালায়।
- **কোন bug ঠিক করেছ?** Repository থেকে সত্যিকারের একটি example বেছে নাও: stale asynchronous replay result, report publication rollback বা cargo reservation। Trigger → ভুল হওয়ার সম্ভাবনা → fix → regression check নিজের ভাষায় দেখাও।

## নিজের প্রস্তুতি যাচাই

কোড না দেখে dispatch → movement → arrival → return-এর ক্রম আঁকতে পারো? একটি run database save/load বোঝাতে পারো? Scenario import ভুল হলে কী হয় দেখাতে পারো? এগুলো পারলে CV-র মূল project description যথাযথভাবে সমর্থন করতে পারবে। মুখস্থ শব্দের বদলে ছোট example দিয়ে বোঝাবে।

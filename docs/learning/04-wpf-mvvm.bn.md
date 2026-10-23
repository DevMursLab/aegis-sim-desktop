# Lesson 04: Console থেকে Windows window

এবার একই ত্রাণের হিসাব একটি window-তে দেখা যায়। শুরুতে A=২৫, B=৫, C=১৮; গাড়ির capacity ১০। এক tick-এ এক visit, প্রতিবার নতুন load। মোট ছয় tick-এ ৪৮ প্যাকেট যায়।

## চালাও

Windows ও .NET 10 SDK লাগবে। repository root-এর terminal-এ:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-lesson.ps1 -Lesson 04
```

1. প্রথমে **Step** চাপো: tick ১, A-এর বাকি চাহিদা ১৫।
2. **Start** চাপো: প্রায় প্রতি সেকেন্ডে এক tick এগোবে।
3. **Pause** চাপো: হিসাব থামবে। ওই অবস্থায় Step দিয়ে শুধু একবার এগোতে পারো।
4. **Resume** চাপলে সেখান থেকেই চলবে।
5. Completed হলে আর এগোনো যাবে না। **Reset** চাপলে পুরো শুরুর অবস্থায় ফিরবে; history মুছে যাবে।

Start করার আগেও Step চাপতে পারো। Reset চলন্ত অবস্থাতেও করা যায়। এই version-এ save নেই; window বন্ধ হলে history হারাবে।

## আজ শুধু তিনটি দায়িত্ব বোঝো

| অংশ | file | কাজ |
|---|---|---|
| Model | `src/Aegis.Simulation/SimulationSession.cs` | হিসাব ও valid অবস্থার নিয়ম |
| ViewModel | `src/Aegis.Presentation/RunViewModel.cs` | screen-এর তথ্য ও button-এর command |
| View | `src/Aegis.Desktop.Wpf/MainWindow.xaml` | screen-এর layout ও লেখা |

এই দায়িত্ব ভাগ করার পদ্ধতিকে **MVVM** বলে। MainWindow-এর code-behind window তৈরি করে এবং Lesson 05 থেকে visual Zoom/Fit action যুক্ত করে; delivery হিসাব সেখানে নেই। App.xaml.cs model ও ViewModel তৈরি করে window-র সঙ্গে যুক্ত করে। ছোট lesson-এ composition সরাসরি আছে; DI container বা MVVM package এখনো লাগানো হয়নি।

## ১. Binding: screen-কে তথ্যের সঙ্গে যুক্ত করা

```xml
<TextBlock Text="{Binding Tick}" />
```

মানে: ViewModel-এর `Tick` property-র মান এখানে দেখাও। আগের `Console.WriteLine`-এর মতো নিজে প্রতিবার print করছি না।

```csharp
var window = new MainWindow { DataContext = viewModel };
```

`DataContext` screen-কে বলে এই `viewModel` object থেকেই Binding-এর তথ্য খুঁজতে হবে।

## ২. PropertyChanged: তথ্য বদলেছে জানানো

ViewModel `INotifyPropertyChanged` ব্যবহার করে। delivery-র পরে সে screen-কে জানায় তথ্য আবার পড়তে। তাই Tick, Remaining এবং shelter-এর তালিকা screen-এ বদলায়। আজ interface-এর পুরো syntax মুখস্থ করতে হবে না; তথ্য বদলানো ও screen-কে খবর দেওয়া আলাদা কাজ, এটুকু আগে বোঝো।

## ৩. Command: button-এর কাজ

```xml
<Button Content="Step" Command="{Binding StepCommand}" />
```

মানে: Step button চাপলে ViewModel-এর StepCommand চালাও। command-এর `CanExecute` বলে এখন কাজটি করা যাবে কি না। Running অবস্থায় manual Step বন্ধ থাকে; Completed অবস্থায় Start/Pause/Step বন্ধ থাকে। `RelayCommand` এখানে ছোট নিজস্ব ICommand implementation, কোনো package নয়।

## ৪. Timer আর simulation tick এক জিনিস নয়

App-এর `DispatcherTimer` প্রায় এক সেকেন্ড পরপর `viewModel.Advance()` ডাকে। UI ব্যস্ত থাকলে callback দেরি হতে পারে। core simulation বাস্তব ঘড়ি পড়ে না: প্রতিবার valid Step-এ এক logical tick এগোয়। তাই manual ও automatic mode-এ delivery-র হিসাব একই।

এই ছোট ৩-কেন্দ্রের demo-তে কাজ অল্প, তাই timer ও update UI thread-এই হয়। বড় scenario হলে background scheduling ও UI update throttling দরকার হবে। এখন background-performance দাবি করা হচ্ছে না।

## পুরোনো lesson-এর সঙ্গে পার্থক্য

আগের তিনটি lesson স্বাধীনভাবে আগের মতোই চলবে। নতুন app-এর shared model `src/`-এ। এখানে engine নিজের data ধরে, readonly shelter state প্রকাশ করে, input পুরো validate করে, এবং Ready/Running/Paused/Completed status রাখে। তাই বাইরে থেকে shelter-এর remaining বদলে app-এর হিসাব নষ্ট করা যায় না।

Reset এখন একই local exercise-এর তথ্য মুছে শুরু করে। database-backed run ID, durable history, SQL replay, map, networking এখনো নেই।

## পরীক্ষা চালাও

```powershell
# Window না খুলে model, command ও map-এর ২০টি check
powershell -ExecutionPolicy Bypass -File .\scripts\run-lesson.ps1 -Lesson 04 -Check

# সব lesson, Release build, ২৭টি console check এবং real-window smoke check
powershell -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

এগুলো executable console checks; xUnit suite নয়, `dotnet test` দিয়ে চালাবে না। Desktop check-এ এখন Lesson 05-এর map check-ও আছে (মোট ২০টি)। WPF smoke check আসল window খুলে button invoke করে, binding, map, timer, pause/resume, reset ও close দেখে। এটি interactive Windows desktop চায়। CI-তে `-SkipUi` দিয়ে window check বাদ থাকে; UI check স্থানীয়ভাবে চালাতে হবে।

## তোমার কাজ

1. window চালিয়ে Step → Start → Pause → Step → Resume → Completed → Reset দেখো।
2. `MainWindow.xaml`-এ শুধু heading-এর লেখা বদলে চালাও। delivery ফল বদলায় কি?
3. `RunViewModel.cs`-এ `Tick` property খুঁজে দেখো; এটি তথ্য কোথা থেকে নিচ্ছে?
4. নিজের ভাষায় বলো: View, ViewModel ও Model-এর দায়িত্ব কী?

সব file একদিনে বোঝার দরকার নেই। প্রথমে শুধু `Text="{Binding Tick}"` আর `Command="{Binding StepCommand}"` বোঝো। map-এর আগে এই screen-control flow বোঝা জরুরি।

Official references: [WPF data binding](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/data-binding-overview), [property change notification](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/how-to-implement-property-change-notification)।

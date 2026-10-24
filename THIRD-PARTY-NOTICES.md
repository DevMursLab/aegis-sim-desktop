# Third-party components

This application uses .NET/WPF/ASP.NET Core, Entity Framework Core SQLite and the SignalR .NET client from Microsoft (MIT; see https://github.com/dotnet/runtime, https://github.com/dotnet/wpf, https://github.com/dotnet/aspnetcore and https://github.com/dotnet/efcore).

PDFsharp is pinned to **6.2.4**, whose NuGet metadata declares **MIT** (https://www.nuget.org/packages/PDFsharp/6.2.4 and https://github.com/empira/PDFsharp). The Windows font resolver reads system fonts; no Windows font file is bundled as an application asset.

SQLite is public domain (https://www.sqlite.org/copyright.html). SQLitePCLRaw is Apache-2.0 (https://github.com/ericsink/SQLitePCL.raw). Transitive packages and exact resolved versions are recorded in `packages.lock.json` files. Included runtime distributions retain their own notice/license files.

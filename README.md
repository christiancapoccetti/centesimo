# Centesimo

Centesimo is a simple, privacy-first Android expense tracker built with .NET MAUI. It focuses on making manual expense entry fast while keeping all financial data on the device.

## Base MVP

- Create, edit, and archive expense categories with optional monthly budgets.
- Create and archive tags within a category.
- Add expenses with an amount, date, category, optional tag, and optional note.
- Browse monthly expense history, then edit or delete an expense.
- Review current-month spending and category budget progress from the today dashboard.
- Store application data locally in SQLite. Centesimo does not require an account or send financial data to a remote service.

Recurring payments, receipt photos, voice control, localization, backups, and additional insights are intentionally deferred. See [FUTURE.md](FUTURE.md) for the current roadmap.

## Architecture

The solution follows a pragmatic Clean Architecture approach with MVVM and the Result Pattern:

- `Centesimo.Domain`: entities, value objects, and domain rules.
- `Centesimo.Application`: use cases, repository contracts, and application services.
- `Centesimo.Infrastructure`: EF Core and SQLite persistence.
- `Centesimo.App`: .NET MAUI Android UI and view models.
- `tests`: domain, application, and infrastructure test projects.

## Prerequisites

- .NET 10 SDK.
- .NET MAUI Android workload: `dotnet workload install maui-android`.
- Android SDK and a compatible JDK.
- An Android device with USB debugging enabled, or an Android emulator.

## Build and run on Android

Restore the solution and run the tests:

```powershell
dotnet restore Centesimo.slnx
dotnet test Centesimo.slnx --no-restore
```

If the Android SDK and JDK are already discoverable by the .NET workload, build and run the app on the connected device with:

```powershell
dotnet build src/Centesimo.App/Centesimo.App.csproj -t:Run -f net10.0-android
```

Otherwise, provide their locations explicitly:

```powershell
dotnet build src/Centesimo.App/Centesimo.App.csproj -t:Run -f net10.0-android `
  -p:AndroidSdkDirectory="<ANDROID_SDK_PATH>" `
  -p:JavaSdkDirectory="<JDK_PATH>"
```

Confirm that the target device is available with `adb devices` before running the app. The minimum supported Android API level is 24.

## License

Centesimo is licensed under the [GNU General Public License v3.0](LICENSE).

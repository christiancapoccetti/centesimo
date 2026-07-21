# Centesimo

Centesimo is a fully vibe-coded app created to explore and test the capabilities of Codex. It is an experimental project, built end to end through an AI-assisted development workflow.

It is also a simple, privacy-first Android expense tracker built with .NET MAUI. Its goal is to make manual expense entry quick while keeping financial data on the device.

## Features

- Create, edit, archive, and restore expense categories with optional monthly budgets. Archived categories are unavailable for new expenses, while existing expenses retain their category.
- Create and archive tags within a category.
- Add expenses with an amount, date, category, optional tag, and optional note.
- Browse monthly expense history, then edit or delete an expense.
- Review current-month spending and category budget progress from the today dashboard.
- Configure weekly, monthly, or yearly recurring payments, with optional end dates. Due expenses are created when the app is opened, and upcoming payments generate Android notifications.
- Create an expense draft by voice on Android, with a multilingual Whisper model downloaded once and kept local, plus explicit confirmation in the expense editor.
- Review local spending trends, category comparisons, and history-based insights for the current month or year.
- Store application data locally in SQLite. Centesimo does not require an account or send financial data to a remote service.

Receipt photos, localization, backups, and configurable budget alerts are not part of the current version. See [FUTURE.md](FUTURE.md) for the roadmap.

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

## Offline voice expense entry

The microphone action records up to 20 seconds and transcribes only on the Android device with Whisper. When Home opens and no valid local model is available, the app asks whether to download the multilingual model (about 181 MiB). If accepted, it downloads, verifies, stores, and loads the model into memory asynchronously. The microphone remains unavailable until preparation completes; if download is declined, it stays disabled and tapping it asks again. After the model is ready, audio is kept in memory and discarded after recognition; no speech audio is uploaded or processed remotely. The loaded model remains available while the app process is running to reduce the delay for later commands.

Offline voice entry currently requires an arm64 Android device.

Recognized commands use a predictable grammar with a numeric amount and an explicit category, for example: `Aggiungi spesa di 50 eur alla categoria spese auto, sotto tag tagliando`. A tag and a note (`con nota ...`) are optional. When the amount is valid but the category or tag does not resolve uniquely to an active item, the app opens a new expense editor with the amount, date, and note prefilled, leaving both category and tag unselected. The app never saves an expense automatically, and saving still requires a category.

## License

Centesimo is licensed under the [GNU General Public License v3.0](LICENSE).

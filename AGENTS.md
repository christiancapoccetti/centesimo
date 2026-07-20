# Agent Guidelines — T-Shirt Factory projects

Shared coding guidelines for Inkee, Teetaly, and Core. Project-local `CLAUDE.md` files override or extend these rules.

## Language

- All code, comments, READMEs, and commit messages must be in English.
- User-facing content (UI text and errors shown to end users) must be in Italian, e.g. `new InvalidResult("Formato email non valido")`. Log and guard messages stay in English.

## General

- Follow SOLID principles with clear separation of concerns.
- Keep code clean; comment only non-obvious logic: explain the why, never the what.
- Apply the boy-scout rule to code already being touched: make behavior-preserving improvements covered by tests, such as simplifying nested conditions with early returns or extracted conditions. Flag unrelated refactors rather than making them.

## Coding style (C#)

- Prefer `var` for local variables.
- Always use early returns to reduce nesting. For an `if` with exactly one statement, omit braces and put the statement on the following line. Use braces for multi-statement bodies.
- Use `=>` when a method body contains a single return.
- Do not use `async`/`await` when directly returning a `Task`. Do use them when an early-return guard precedes the return; never use `Task.FromResult` as a workaround.
- Do not append `Async` to method names unless a non-async variant exists.
- Use `Task.WhenAll` for independent operations; await sequentially only for real dependencies.
- Prefer `""` to `string.Empty`; prefer `string` with a default `""` to `string?`. Exception: Core, where nullable reference types are enabled and multi-targeting may require nullable types.
- In modern projects, use `HasValue()` / `IsEmpty()` extensions for string checks and prefer C# 12 collection expressions (`[]`). In legacy WebForms/Pressto (`net481`) code, use only standard .NET APIs; do not add modern dependencies.
- Format chained calls vertically, with one call per line. Never align columns with multiple spaces.
- Place an interface in the same file as its implementation, with the interface first. Name the file after the implementation class.

## Architecture

- Controllers handle HTTP mapping only; services contain business logic; repositories perform data access only.
- Request and response DTOs belong in `Inkee.Common`, never in controller files.
- Repositories return only DTOs (`Inkee.Common.Models`, `Inkee.Sql.Models.Etl`), never EF entities or external-library types.
- Name resource identifiers `{Resource}Id` (for example, `OrderId`), never `Id{Resource}`.
- Use the Result Pattern for expected errors instead of exceptions.
- Register library services inline in `Program.cs`; do not add thin wrappers around library APIs.
- Use TF.Core only in SDK-style projects (`netstandard2.0+` / `net10.0`). Legacy projects must not consume it, and helpers must not be promoted to it without justification.

## EF Core

- Use EF Core for database operations, LINQ instead of raw SQL, and no stored procedures unless requested.
- Use `.AsNoTracking()` for read-only queries. For updates, fetch tracked entities, modify them, then call `SaveChangesAsync()`.

## Testing

- Create one test class per source class, named `{ClassName}_should_expected_behavior`.
- Move private helpers with testable logic into dedicated testable classes (for example, `Inkee.Services/Tools/`).

## After every change

- Run `dotnet build` and relevant tests; skip tests only for legacy code.
- Before pushing, run the full test suite with zero failures.
- Clean up in cascade: remove redundant code, parameters, methods, and classes created by the change.
- Check whether existing documentation needs alignment. Add new documentation only for business- or architecture-critical changes, and keep it concise.

## Git

- Never push to `master`; all changes must go through the project's normal branch and review workflow. Pushing is done by the user.

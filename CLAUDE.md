# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Test Commands

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run a specific test by name filter
dotnet test --filter "TestMethodName"

# Run tests with verbose output
dotnet test --logger "console;verbosity=detailed"
```

### Reproducing SonarCloud warnings locally

CI analyses this repository with the SonarCloud scanner, which injects the Sonar analyzers into the
compilation. A plain `dotnet build` does **not** run them, so Sonar findings are invisible locally
and only surface after a push — and the bot comment links to a dashboard rather than naming them.
To run the same analyzers:

```bash
dotnet build -p:CustomAfterMicrosoftCommonProps=$PWD/.sonarlint/sonar-local.props
```

```powershell
dotnet build -p:CustomAfterMicrosoftCommonProps=$PWD\.sonarlint\sonar-local.props
```

Note **`After`**, not `Before`. Every project here declares its SDK with `<Sdk Name="..." />`
elements rather than the `<Project Sdk="...">` attribute, and `CustomBeforeMicrosoftCommonProps`
does not reach that form.

The opt-in lives in `.sonarlint/sonar-local.props` (the analyzer package) and
`.sonarlint/sonar-local.globalconfig` (rule severities — it raises the rules CI reports that the
analyzer package ships disabled). Nothing imports these automatically, so normal builds, the CI
pipeline, and packaging are unaffected.

**Known gap:** SonarCloud reported one new issue on PR #87 that this configuration does not
reproduce, and sonarcloud.io is not reachable from the agent sandbox to identify it. The rule
behind it is either absent from the analyzer package or shipped disabled and not listed in the
globalconfig. If you have dashboard access, add it — the calibration is only as good as the rules
it names.

## Project Structure

- **CodeBlocker/**: Main library - an `IndentedTextWriter` wrapper for generating code blocks with automatic indentation
- **CodeBlocker.Test/**: MSTest-based unit and integration tests

## Architecture

The library is built around these types:

1. **`CodeBlocker`** (`CodeBlocker/CodeBlocker.cs`): Wraps `System.CodeDom.Compiler.IndentedTextWriter` to provide simplified code generation with:
   - Factory methods (`Create()`, `Create(string indentString)`) that manage `StringWriter` lifecycle
   - Constructors over any `TextWriter`, for streaming straight to a file — such a writer stays the
     caller's to dispose, and `IsBuffered`/`ToString()` only work over a `StringWriter`
   - Indentation control via `Indent()`, `Outdent()`, and `CurrentIndent` property
   - Output methods: `Write()`, `WriteLine()`, `NewLine()`
   - Implements `IDisposable` with proper resource cleanup

   **Line endings.** `IndentedTextWriter` terminates lines with `Environment.NewLine`;
   `CodeBlocker` deliberately does not. `DefaultNewLineString` is `NewLines.Lf`, so output is
   byte-identical on every platform — generated code is committed, diffed and compared against
   golden files, all of which want reproducibility over the local convention. `NewLines.Host` is
   the opt-in for the platform terminator. Tests must therefore assert against
   `CodeBlocker.DefaultNewLineString`, never `Environment.NewLine`: the latter passes on Linux
   for the wrong reason and hides a Windows break.

2. **`Scope`** (`CodeBlocker/Scope.cs`): Extends `ktsu.ScopedAction` to provide automatic brace handling:
   - On creation: writes `{` and increases indent
   - On disposal: decreases indent and writes `}`
   - Used with C# `using` statements for clean nested code generation

3. **`ScopeWithTrailingSemicolon`** (`CodeBlocker/Scope.cs`): Variant of `Scope` that appends a semicolon after the closing brace:
   - On creation: writes `{` and increases indent
   - On disposal: decreases indent and writes `};`
   - Useful for C/C++ enum declarations, struct initializers, etc.

4. **Other scopes** (`CodeBlocker/Scopes.cs`): `DelimiterScope` and its `ParenScope`/`BracketScope`
   derivations, plus `IndentScope`, `RegionScope`, `DirectiveScope` and `PragmaScope`.

5. **Preamble helpers** (`CodeBlocker/CodeBlockerExtensions.cs`): one call each for the
   auto-generated marker, the nullable context, the file-scoped namespace and the using directives.

6. **Template object model** (`CodeBlocker/Templates/`): `SourceFileTemplate`, `ClassTemplate`,
   `MethodTemplate`, `PropertyTemplate`, `OperatorTemplate` and friends describe a source file as
   objects and own all the punctuation, spacing and indentation. Rendering lives in the internal
   `TemplateRendering`; a `BodyFactory` writes only the body, with no leading separator.

   When emitting a multi-line fragment inside a template, route it through
   `TemplateRendering.SpliceFragment` rather than `NewLine()`/`WriteLineNoTabs` —
   `IndentedTextWriter.WriteLineNoTabs` does not re-arm the pending-tab flag, so the next line
   silently lands at column 0.

## SDK and Dependencies

This project uses:
- **ktsu.Sdk**: Custom SDK for common project configuration (see `global.json`)
- **MSTest.Sdk**: For test project configuration
- **ktsu.ScopedAction**: For the `Scope` class implementation
- Central package management via `Directory.Packages.props`

Target frameworks: .NET 10.0, 9.0, 8.0, 7.0, 6.0, 5.0, .NET Standard 2.0/2.1

## Code Quality

Do not add global suppressions for warnings. Prefer explicit suppression attributes with justifications when available, with a fallback to preprocessor directives only if necessary. Include a comment justification for any suppressions. Only make the smallest, most targeted suppressions possible.

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

## Project Structure

- **CodeBlocker/**: Main library - an `IndentedTextWriter` wrapper for generating code blocks with automatic indentation
- **CodeBlocker.Test/**: MSTest-based unit and integration tests

## Architecture

The library consists of two main classes:

1. **`CodeBlocker`** (`CodeBlocker/CodeBlocker.cs`): Wraps `System.CodeDom.Compiler.IndentedTextWriter` to provide simplified code generation with:
   - Factory methods (`Create()`, `Create(string indentString)`) that manage `StringWriter` lifecycle
   - Indentation control via `Indent()`, `Outdent()`, and `CurrentIndent` property
   - Output methods: `Write()`, `WriteLine()`, `NewLine()`
   - Implements `IDisposable` with proper resource cleanup

2. **`Scope`** (`CodeBlocker/Scope.cs`): Extends `ktsu.ScopedAction` to provide automatic brace handling:
   - On creation: writes `{` and increases indent
   - On disposal: decreases indent and writes `};`
   - Used with C# `using` statements for clean nested code generation

## SDK and Dependencies

This project uses:
- **ktsu.Sdk**: Custom SDK for common project configuration (see `global.json`)
- **MSTest.Sdk**: For test project configuration
- **ktsu.ScopedAction**: For the `Scope` class implementation
- Central package management via `Directory.Packages.props`

Target frameworks: .NET 10.0, 9.0, 8.0, 7.0, 6.0, 5.0, .NET Standard 2.0/2.1

## Code Quality

Do not add global suppressions for warnings. Prefer explicit suppression attributes with justifications when available, with a fallback to preprocessor directives only if necessary. Include a comment justification for any suppressions. Only make the smallest, most targeted suppressions possible.

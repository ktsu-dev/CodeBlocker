# ktsu.CodeBlocker

> An IndentedTextWriter that makes generating code blocks easier.

[![License](https://img.shields.io/github/license/ktsu-dev/CodeBlocker.svg?label=License&logo=nuget)](LICENSE.md)
[![NuGet Version](https://img.shields.io/nuget/v/ktsu.CodeBlocker?label=Stable&logo=nuget)](https://nuget.org/packages/ktsu.CodeBlocker)
[![NuGet Version](https://img.shields.io/nuget/vpre/ktsu.CodeBlocker?label=Latest&logo=nuget)](https://nuget.org/packages/ktsu.CodeBlocker)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ktsu.CodeBlocker?label=Downloads&logo=nuget)](https://nuget.org/packages/ktsu.CodeBlocker)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/ktsu-dev/CodeBlocker?label=Commits&logo=github)](https://github.com/ktsu-dev/CodeBlocker/commits/main)
[![GitHub contributors](https://img.shields.io/github/contributors/ktsu-dev/CodeBlocker?label=Contributors&logo=github)](https://github.com/ktsu-dev/CodeBlocker/graphs/contributors)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/ktsu-dev/CodeBlocker/dotnet.yml?branch=main&label=Build&logo=github)](https://github.com/ktsu-dev/CodeBlocker/actions)

## Introduction

CodeBlocker is a specialized utility built on top of `IndentedTextWriter` that simplifies the process of programmatically generating structured code. It provides automatic indentation management and a fluent interface for creating code blocks with proper nesting, making it ideal for code generation tasks, template engines, and dynamic source code creation.

## Features

- **Automatic Indentation**: Properly manages indentation levels as you create nested code blocks
- **Configurable Indentation**: Support for custom indent strings (tabs, spaces, or any custom pattern)
- **Configurable Line Endings**: Pin the line terminator so the same calls produce byte-identical output on every platform
- **Scope Management**: Uses C# `using` statements for clean, readable scope creation with automatic brace handling powered by `ktsu.ScopedAction`, with optional trailing semicolons via `ScopeWithTrailingSemicolon`
- **More Than Braces**: Parenthesis, bracket, bare-indent, `#region`, `#if` and `#pragma warning` scopes, each balanced by disposal
- **Preamble Helpers**: One call each for the auto-generated marker, the nullable context, the namespace declaration, and the using directives
- **Template Object Model**: Describe a whole source file as objects — types, members, operators, generics, XML docs — and let the model own the punctuation, spacing and indentation
- **Flexible API**: Write individual lines or entire code blocks with proper formatting
- **Any TextWriter**: Buffer into a `StringWriter`, or stream straight to a file or any other `TextWriter`
- **Cross-Platform**: Supports .NET 10.0, 9.0, 8.0, 7.0, 6.0, 5.0, .NET Standard 2.0 and 2.1
- **Lightweight**: Minimal dependencies, built on top of `ktsu.ScopedAction` for robust scope management
- **Well-Tested**: Includes comprehensive unit and integration tests

## Installation

### Package Manager Console

```powershell
Install-Package ktsu.CodeBlocker
```

### .NET CLI

```bash
dotnet add package ktsu.CodeBlocker
```

### Package Reference

```xml
<PackageReference Include="ktsu.CodeBlocker" Version="1.1.5" />
```

## Usage Examples

### Basic Example

```csharp
namespace CodeBlockerExample;

using ktsu.CodeBlocker;

internal class Example
{
	public static void GenerateCode()
	{
		// Create a new CodeBlocker instance with default tab indentation
		using var codeBlocker = CodeBlocker.Create();

		// Write using statements and namespace
		codeBlocker.WriteLine("using System;");
		codeBlocker.NewLine(); // Add empty line without indentation
		codeBlocker.WriteLine("namespace Example");
		
		// Use Scope for automatic brace and indentation management
		using (new Scope(codeBlocker))
		{
			codeBlocker.WriteLine("public class Example");
			using (new Scope(codeBlocker))
			{
				codeBlocker.WriteLine("public static void Main()");
				using (new Scope(codeBlocker))
				{
					codeBlocker.WriteLine("Console.WriteLine(\"Hello, World!\");");
				} // Scope automatically writes closing brace and manages indentation
			}
		}

		// Output the generated code
		Console.WriteLine(codeBlocker.ToString());
	}
}
```

The above example generates the following code:

```csharp
using System;

namespace Example
{
	public class Example
	{
		public static void Main()
		{
			Console.WriteLine("Hello, World!");
		}
	}
}
```

> **Note**: The `Scope` class writes closing braces without semicolons (`}`). If you need trailing semicolons after closing braces (`};`), such as for enum declarations or struct initializers in C/C++, use `ScopeWithTrailingSemicolon` instead.

### Custom Indentation

CodeBlocker supports configurable indentation strings, allowing you to use spaces, tabs, or any custom pattern:

```csharp
// Using 2 spaces for indentation
using var codeBlocker2Space = CodeBlocker.Create("  ");

// Using 4 spaces for indentation
using var codeBlocker4Space = CodeBlocker.Create("    ");

// Using custom patterns (e.g., for markup generation)
using var customCodeBlocker = CodeBlocker.Create("-->");

// With existing StringWriter and custom indentation
using var stringWriter = new StringWriter();
using var codeBlocker = new CodeBlocker(stringWriter, "  ");

codeBlocker.WriteLine("function example() {");
codeBlocker.Indent();
codeBlocker.WriteLine("console.log('Hello with 2 spaces!');");
codeBlocker.Outdent();
codeBlocker.WriteLine("}");

Console.WriteLine(codeBlocker.ToString());
// Output:
// function example() {
//   console.log('Hello with 2 spaces!');
// }

// Check current indent configuration
Console.WriteLine($"Current indent: '{codeBlocker.IndentString}'"); // "  "

// Custom indentation works seamlessly with Scope
using var scopeCodeBlocker = CodeBlocker.Create("    "); // 4 spaces
scopeCodeBlocker.WriteLine("public class Example");
using (new Scope(scopeCodeBlocker))
{
    scopeCodeBlocker.WriteLine("public void Method()");
    using (new Scope(scopeCodeBlocker))
    {
        scopeCodeBlocker.WriteLine("// 4-space indented code");
    }
}
```

### Describing Code as Templates

Writing a generator against `WriteLine` means owning every brace, comma and blank line yourself, and re-deriving the same layout decisions in every generator you write. The `ktsu.CodeBlocker.Templates` namespace lets you describe the file instead:

```csharp
namespace CodeBlockerExample;

using ktsu.CodeBlocker;
using ktsu.CodeBlocker.Templates;

internal class TemplateExample
{
	public static string GenerateCode()
	{
		SourceFileTemplate file = new()
		{
			FileName = "Money.g.cs",
			Namespace = "Contoso.Billing",
			Usings = { "System" },
		};

		ClassTemplate money = new()
		{
			Kind = TypeKind.RecordStruct,
			Name = "Money",
			Keywords = { "public", "readonly" },
			PositionalParameters = { new ParameterTemplate { Type = "decimal", Name = "Amount" } },
			Documentation = new DocComment { Summary = "An amount of money." },
		};

		money.Members.Add(new OperatorTemplate
		{
			Type = "Money",
			Keywords = { "public", "static" },
			Symbol = "+",
			Parameters =
			{
				new ParameterTemplate { Type = "Money", Name = "left" },
				new ParameterTemplate { Type = "Money", Name = "right" },
			},
			BodyFactory = codeBlocker => codeBlocker.Write("=> new(left.Amount + right.Amount);"),
		});

		file.Classes.Add(money);

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.AddSourceFile(file);
		return codeBlocker.ToString();
	}
}
```

Produces:

```csharp
namespace Contoso.Billing;

using System;

/// <summary>An amount of money.</summary>
public readonly record struct Money(decimal Amount)
{
	public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);
}
```

Note what you did not have to decide: that the attribute goes on its own line, that the operator is indented one level, that members are separated by a blank line, that a positional record with a body still needs braces while one without gets a semicolon.

Collection properties are read-only, so use collection-initializer syntax — `Keywords = { "public" }` — rather than assignment.

#### Bodies

A member body is written by a callback into a nested `CodeBlocker` and then spliced in, **re-indented line by line** to wherever it lands. Nest it as deeply as you like:

```csharp
new MethodTemplate
{
	Type = "int",
	Name = "Add",
	Keywords = { "public" },
	Parameters =
	{
		new ParameterTemplate { Type = "int", Name = "a" },
		new ParameterTemplate { Type = "int", Name = "b" },
	},
	BodyFactory = codeBlocker =>
	{
		using Scope scope = new(codeBlocker);
		codeBlocker.WriteLine("return a + b;");
	},
}
```

A callback that writes a single line becomes an expression body on the declaration line; one that writes several becomes a braced body on the following lines; one that writes nothing becomes `{ }`; and a `null` `BodyFactory` declares the member with no body at all, for an abstract, partial or interface declaration.

#### XML documentation

`DocComment` models documentation as data rather than as pre-formatted comment lines, so the content is escaped, the tags come out in canonical order, and a multi-line description is prefixed correctly:

```csharp
Documentation = new DocComment
{
	Summary = "Clamps a ratio to the range <0, 1>.",
	Params = { new DocTag { Name = "value", Text = "The ratio." } },
	Returns = "The clamped ratio.",
}
```

```csharp
/// <summary>Clamps a ratio to the range &lt;0, 1&gt;.</summary>
/// <param name="value">The ratio.</param>
/// <returns>The clamped ratio.</returns>
```

Set `EscapeText = false` when the text deliberately embeds markup such as `<see cref="…"/>`. `Validate(parameterNames, typeParameterNames)` returns one message per mismatched or missing `<param>`/`<typeparam>` entry, so a generator can report them as build diagnostics instead of letting CS1572 and CS1573 surface inside the generated file.

### More Than Braces

`Scope` and `ScopeWithTrailingSemicolon` cover braces. The same pattern covers the other shapes that recur in generated code, and every one of them is balanced by disposal — so an unbalanced `#pragma warning disable` or a stray `#endregion` is not something you can leave behind.

| Scope | Opens with | Closes with | Indents the body |
|-------|-----------|-------------|------------------|
| `Scope` | `{` | `}` | Yes |
| `ScopeWithTrailingSemicolon` | `{` | `};` | Yes |
| `ParenScope` | `(` | `)` | Yes |
| `BracketScope` | `[` | `]` | Yes |
| `IndentScope` | — | — | Yes |
| `RegionScope` | `#region name` | `#endregion` | No |
| `DirectiveScope` | `#if condition` | `#endif` | No |
| `PragmaScope` | `#pragma warning disable …` | `#pragma warning restore …` | No |

The three directive scopes do not indent, because a directive does not nest code. `DelimiterScope` is the shared base if you need a pair of delimiters the library does not name.

```csharp
namespace CodeBlockerExample;

using ktsu.CodeBlocker;

internal class ScopesExample
{
	public static string GenerateCode()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		codeBlocker.WriteLine("public class Example");
		using (new Scope(codeBlocker))
		{
			using (new RegionScope(codeBlocker, "Constructors"))
			{
				codeBlocker.WriteLine("public Example");
				using (new ParenScope(codeBlocker))
				{
					codeBlocker.WriteLine("int first,");
					codeBlocker.WriteLine("int second");
				}

				codeBlocker.WriteLine("{ }");
			}

			using (new PragmaScope(codeBlocker, "CS1591"))
			{
				codeBlocker.WriteLine("public int Undocumented;");
			}
		}

		return codeBlocker.ToString();
	}
}
```

Produces:

```csharp
public class Example
{
	#region Constructors
	public Example
	(
		int first,
		int second
	)
	{ }
	#endregion
	#pragma warning disable CS1591
	public int Undocumented;
	#pragma warning restore CS1591
}
```

### File Preambles

The lines at the top of a generated file are the same every time, so they get one call each. Every helper that conventionally has a blank line after it writes that blank line, which keeps the spacing consistent no matter which parts a given generator emits.

```csharp
using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

codeBlocker
	.WriteAutoGeneratedHeader("Copyright (c) 2023-2026 ktsu-dev contributors")
	.WriteNullableEnable()
	.WriteFileScopedNamespace("Contoso.Widgets")
	.WriteUsings("System", "System.Collections.Generic");
```

Produces:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors
// <auto-generated />

#nullable enable
namespace Contoso.Widgets;

using System;
using System.Collections.Generic;

```

`WriteFileScopedNamespace` writes nothing for a null or empty namespace, and `WriteUsings` writes nothing — not even the blank line — for an empty sequence, so a generator can call them unconditionally.

### Writing Somewhere Other Than a String

`CodeBlocker.Create()` buffers into a `StringWriter` it owns, which is what makes `ToString()` able to hand the code back. You can instead give it any `TextWriter` — a file, a `TextWriter` handed to you by a build task, a test double:

```csharp
namespace CodeBlockerExample;

using ktsu.CodeBlocker;

internal class FileExample
{
	public static void GenerateToFile(string path)
	{
		using StreamWriter file = new(path);
		using CodeBlocker codeBlocker = new(file, CodeBlocker.DefaultIndentString, NewLines.Lf);

		codeBlocker.WriteLine("public class Example");
		using (new Scope(codeBlocker))
		{
			codeBlocker.WriteLine("public int Value { get; set; }");
		}
	}
}
```

Two things to know:

- **A writer you supply stays yours.** `CodeBlocker.Dispose()` disposes only the `StringWriter` that `Create()` made for itself; it never disposes a writer you passed in.
- **`ToString()` only works when buffered.** A `CodeBlocker` over a `StreamWriter` keeps no copy of what it wrote, so `ToString()` returns the type name rather than the code — check `IsBuffered` if you need to know which case you are in. Read the generated code from your own writer instead.

### Line Endings

`CodeBlocker` writes through `IndentedTextWriter`, which terminates lines with `Environment.NewLine`. That makes output depend on the machine that produced it — the same calls give you CRLF on Windows and LF everywhere else.

If your generated code is committed to a repository, or compared against a golden file, pin the terminator instead:

```csharp
namespace CodeBlockerExample;

using ktsu.CodeBlocker;

internal class DeterministicExample
{
	public static string GenerateCode()
	{
		// Byte-identical on every platform.
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		codeBlocker.WriteLine("public class Example");
		using (new Scope(codeBlocker))
		{
			codeBlocker.WriteLine("public int Value { get; set; }");
		}

		return codeBlocker.ToString();
	}
}
```

The `NewLines` class names the usual choices:

| Name | Value | Notes |
|------|-------|-------|
| `NewLines.Lf` | `"\n"` | The conventional choice for reproducible output |
| `NewLines.CrLf` | `"\r\n"` | Use when the target repository stores `.cs` files with CRLF |
| `NewLines.Host` | `Environment.NewLine` | The default, and the one that varies by platform |

Any other string works too — the terminator is written verbatim.

### Advanced Usage

```csharp
// Creating a CodeBlocker with a custom StringWriter
using var stringWriter = new StringWriter();
using var codeBlocker = new CodeBlocker(stringWriter);

// Generate a more complex structure
codeBlocker.WriteLine("public interface IExample");
using (new Scope(codeBlocker))
{
    // Define interface methods
    codeBlocker.WriteLine("void Method1();");
    codeBlocker.WriteLine("string Method2(int parameter);");
    
    // Define nested interface
    codeBlocker.NewLine();
    codeBlocker.WriteLine("public interface INestedExample");
    using (new Scope(codeBlocker))
    {
        codeBlocker.WriteLine("void NestedMethod();");
    }
}

// Add implementation
codeBlocker.NewLine();
codeBlocker.WriteLine("public class Implementation : IExample");
using (new Scope(codeBlocker))
{
    // Implement methods
    codeBlocker.WriteLine("public void Method1()");
    using (new Scope(codeBlocker))
    {
        codeBlocker.WriteLine("// Implementation here");
    }
    
    codeBlocker.NewLine();
    codeBlocker.WriteLine("public string Method2(int parameter)");
    using (new Scope(codeBlocker))
    {
        codeBlocker.WriteLine("return parameter.ToString();");
    }
}

// Get the result
string result = codeBlocker.ToString();
```

## API Reference

### `CodeBlocker` Class

The main class for building indented code blocks.

#### Constructors

| Name | Description |
|------|-------------|
| `CodeBlocker(StringWriter stringWriter)` | Creates a new CodeBlocker with the specified StringWriter using tab indentation |
| `CodeBlocker(StringWriter stringWriter, string indentString)` | Creates a new CodeBlocker with the specified StringWriter and custom indent string |
| `CodeBlocker(StringWriter stringWriter, string indentString, string newLineString)` | As above, and pins the line terminator written at the end of every line |
| `CodeBlocker(TextWriter writer)` | Creates a new CodeBlocker over any TextWriter using tab indentation |
| `CodeBlocker(TextWriter writer, string indentString)` | Creates a new CodeBlocker over any TextWriter with a custom indent string |
| `CodeBlocker(TextWriter writer, string indentString, string newLineString)` | As above, and pins the line terminator |

#### Properties

| Name | Type | Description |
|------|------|-------------|
| `CurrentIndent` | `int` | Gets or sets the current indentation level |
| `IndentString` | `string` | Gets the current indent string being used (e.g., "\t", "  ", "    ") |
| `NewLineString` | `string` | Gets the line terminator written at the end of every line |
| `IsBuffered` | `bool` | Whether `ToString()` can return the generated code, i.e. whether the underlying writer is a `StringWriter` |

#### Methods

| Name | Return Type | Description |
|------|-------------|-------------|
| `WriteLine(string line)` | `void` | Writes a line of text with appropriate indentation |
| `WriteLine()` | `void` | Writes an empty line with current indentation |
| `Write(string text)` | `void` | Writes text without adding a new line |
| `NewLine()` | `void` | Writes an empty line without indentation |
| `Indent()` | `void` | Increases the indent level |
| `Outdent()` | `void` | Decreases the indent level |
| `ToString()` | `string` | Returns the generated code as a string |
| `Create()` | `CodeBlocker` | Static factory method to create a new CodeBlocker instance with tab indentation |
| `Create(string indentString)` | `CodeBlocker` | Static factory method to create a new CodeBlocker instance with custom indentation |
| `Create(string indentString, string newLineString)` | `CodeBlocker` | Static factory method to create a new CodeBlocker instance with custom indentation and a pinned line terminator |
| `Dispose()` | `void` | Disposes of the CodeBlocker and underlying resources |

### `Scope` Class

Helper class for managing indentation scopes with automatic brace handling. Built on top of `ktsu.ScopedAction` for guaranteed resource cleanup and exception safety.

#### Constructor

| Name | Description |
|------|-------------|
| `Scope(CodeBlocker codeBlocker)` | Creates a new scope that automatically writes opening brace `{`, increases indentation, and handles cleanup on disposal |

#### Methods

| Name | Return Type | Description |
|------|-------------|-------------|
| `Dispose()` | `void` | Decreases indentation level and writes closing brace `}` when scope is exited |

#### Behavior

- **On Creation**: Writes `{` and increases indentation level
- **On Disposal**: Decreases indentation level and writes `}`
- **Exception Safety**: Guaranteed cleanup even if exceptions occur within the scope
- **Resource Management**: Built on `ktsu.ScopedAction` for reliable resource handling

### Template Object Model

`ktsu.CodeBlocker.Templates`. See [Describing Code as Templates](#describing-code-as-templates).

| Type | Describes |
|------|-----------|
| `SourceFileTemplate` | A whole file: namespace, usings, types |
| `ClassTemplate` | A type of any `TypeKind`, its generics, base list, constraints, members and nested types |
| `FieldTemplate` | A field, with an optional initializer |
| `PropertyTemplate` | A property: automatic, expression-bodied, or a full accessor list |
| `AccessorTemplate` | One accessor, as `AccessorKind.Auto`, `.Expression` or `.Block`, with an optional modifier |
| `MethodTemplate` | A method, its generics, parameters, constraints and body |
| `ConstructorTemplate` | A constructor, its parameters, its `base`/`this` initializer and body |
| `OperatorTemplate` | An operator or an `implicit`/`explicit` conversion |
| `EnumMemberTemplate` | One enum member |
| `ParameterTemplate` | One parameter, with an optional default |
| `DocComment`, `DocTag` | XML documentation as data |
| `TemplateBase` | What every template carries: name, type, modifiers, attributes, comments, documentation |

| Enum | Values |
|------|--------|
| `TypeKind` | `Class`, `Struct`, `Interface`, `Record`, `RecordStruct`, `Enum` |
| `AccessorKind` | `Auto`, `Expression`, `Block` |
| `OperatorKind` | `Normal`, `Implicit`, `Explicit` |

Rendering entry points: `codeBlocker.AddSourceFile(file)`, `codeBlocker.AddClass(type)`, or `template.WriteTo(codeBlocker)` for any single template.

### `CodeBlockerExtensions` Class

File-level preamble helpers. Each returns the same `CodeBlocker` so calls chain.

| Name | Description |
|------|-------------|
| `WriteAutoGeneratedHeader(string? copyright = null)` | Writes an optional copyright line, then `// <auto-generated />`, then a blank line |
| `WriteNullableEnable()` | Writes `#nullable enable` |
| `WriteNullableDisable()` | Writes `#nullable disable` |
| `WriteFileScopedNamespace(string? namespaceName)` | Writes `namespace X;` and a blank line, or nothing when the namespace is null or empty |
| `WriteUsings(IEnumerable<string> usings)` | Writes one `using X;` per entry and a blank line, or nothing when empty |
| `WriteUsings(params string[] usings)` | As above |

### `NewLines` Class

Named line terminators. See [Line Endings](#line-endings).

| Name | Value |
|------|-------|
| `Lf` | `"\n"` |
| `CrLf` | `"\r\n"` |
| `Host` | `Environment.NewLine` |

### `DelimiterScope` Class

Base class for a scope that writes an opening delimiter, indents the body, and writes a closing delimiter on disposal. `ParenScope` and `BracketScope` derive from it; derive your own for a delimiter pair the library does not name.

#### Constructor

| Name | Description |
|------|-------------|
| `DelimiterScope(CodeBlocker codeBlocker, string open, string close)` | Creates a scope writing `open` before the body and `close` after it |

### `ParenScope`, `BracketScope`, `IndentScope` Classes

| Name | Description |
|------|-------------|
| `ParenScope(CodeBlocker codeBlocker)` | Wraps the body in `(` and `)`, indenting it |
| `BracketScope(CodeBlocker codeBlocker)` | Wraps the body in `[` and `]`, indenting it |
| `IndentScope(CodeBlocker codeBlocker)` | Indents the body without writing any delimiters |

### `RegionScope`, `DirectiveScope`, `PragmaScope` Classes

Preprocessor directive pairs. None of them indents the body.

| Name | Description |
|------|-------------|
| `RegionScope(CodeBlocker codeBlocker, string name)` | Wraps the body in `#region name` and `#endregion`; the name may be empty |
| `DirectiveScope(CodeBlocker codeBlocker, string condition)` | Wraps the body in `#if condition` and `#endif` |
| `PragmaScope(CodeBlocker codeBlocker, string warnings)` | Wraps the body in `#pragma warning disable`/`restore` for the same warnings |
| `PragmaScope(CodeBlocker codeBlocker, IEnumerable<string> warnings)` | As above, joining the identifiers with `, ` |

### `ScopeWithTrailingSemicolon` Class

Variant of `Scope` that appends a semicolon after the closing brace. Useful for code generation scenarios like C/C++ enum or struct declarations where a trailing semicolon is required.

#### Constructor

| Name | Description |
|------|-------------|
| `ScopeWithTrailingSemicolon(CodeBlocker codeBlocker)` | Creates a new scope that automatically writes opening brace `{`, increases indentation, and handles cleanup on disposal |

#### Methods

| Name | Return Type | Description |
|------|-------------|-------------|
| `Dispose()` | `void` | Decreases indentation level and writes closing brace with semicolon `};` when scope is exited |

#### Behavior

- **On Creation**: Writes `{` and increases indentation level
- **On Disposal**: Decreases indentation level and writes `};`
- **Exception Safety**: Guaranteed cleanup even if exceptions occur within the scope
- **Resource Management**: Built on `ktsu.ScopedAction` for reliable resource handling

## Contributing

Contributions are welcome! Here's how you can help:

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

# ktsu.CodeBlocker

> An IndentedTextWriter that makes generating code blocks easier.

[![License](https://img.shields.io/github/license/ktsu-dev/CodeBlocker)](https://github.com/ktsu-dev/CodeBlocker/blob/main/LICENSE.md)
[![NuGet](https://img.shields.io/nuget/v/ktsu.CodeBlocker.svg)](https://www.nuget.org/packages/ktsu.CodeBlocker/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ktsu.CodeBlocker.svg)](https://www.nuget.org/packages/ktsu.CodeBlocker/)
[![Build Status](https://github.com/ktsu-dev/CodeBlocker/workflows/build/badge.svg)](https://github.com/ktsu-dev/CodeBlocker/actions)
[![GitHub Stars](https://img.shields.io/github/stars/ktsu-dev/CodeBlocker?style=social)](https://github.com/ktsu-dev/CodeBlocker/stargazers)

## Introduction

CodeBlocker is a specialized utility built on top of `IndentedTextWriter` that simplifies the process of programmatically generating structured code. It provides automatic indentation management and a fluent interface for creating code blocks with proper nesting, making it ideal for code generation tasks, template engines, and dynamic source code creation.

## Features

- **Automatic Indentation**: Properly manages indentation levels as you create nested code blocks
- **Configurable Indentation**: Support for custom indent strings (tabs, spaces, or any custom pattern)
- **Scope Management**: Uses C# `using` statements for clean, readable scope creation with automatic brace handling
- **Flexible API**: Write individual lines or entire code blocks with proper formatting
- **Standard Output Support**: Works with StringWriter for flexible output handling
- **Lightweight**: Minimal dependencies, built on top of `ktsu.ScopedAction`

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
<PackageReference Include="ktsu.CodeBlocker" Version="x.y.z" />
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
		using var codeBlocker = CodeBlocker.Create();

		codeBlocker.WriteLine("using System;");
		codeBlocker.NewLine();
		codeBlocker.WriteLine("namespace Example");
		using (new Scope(codeBlocker))
		{
			codeBlocker.WriteLine("public class Example");
			using (new Scope(codeBlocker))
			{
				codeBlocker.WriteLine("public static void Main()");
				using (new Scope(codeBlocker))
				{
					codeBlocker.WriteLine("Console.WriteLine(\"Hello, World!\");");
				}
			}
		}

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
		};
	};
};
```

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

#### Properties

| Name | Type | Description |
|------|------|-------------|
| `CurrentIndent` | `int` | Gets or sets the current indentation level |
| `IndentString` | `string` | Gets the current indent string being used (e.g., "\t", "  ", "    ") |

#### Methods

| Name | Return Type | Description |
|------|-------------|-------------|
| `WriteLine(string line)` | `void` | Writes a line of text with appropriate indentation |
| `NewLine()` | `void` | Writes an empty line |
| `Indent()` | `void` | Increases the indent level |
| `Outdent()` | `void` | Decreases the indent level |
| `ToString()` | `string` | Returns the generated code as a string |
| `Create()` | `CodeBlocker` | Static factory method to create a new CodeBlocker instance with tab indentation |
| `Create(string indentString)` | `CodeBlocker` | Static factory method to create a new CodeBlocker instance with custom indentation |
| `Dispose()` | `void` | Disposes of the CodeBlocker and underlying resources |

### `Scope` Class

Helper class for managing indentation scopes with automatic brace handling. Inherits from `ScopedAction`.

#### Constructor

| Name | Description |
|------|-------------|
| `Scope(CodeBlocker codeBlocker)` | Creates a new scope that automatically writes opening brace `{`, increases indentation, and handles cleanup on disposal |

#### Methods

| Name | Return Type | Description |
|------|-------------|-------------|
| `Dispose()` | `void` | Decreases indentation level and writes closing brace `};` when scope is exited |

#### Behavior

- **On Creation**: Writes `{` and increases indentation level
- **On Disposal**: Decreases indentation level and writes `};`

## Contributing

Contributions are welcome! Here's how you can help:

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

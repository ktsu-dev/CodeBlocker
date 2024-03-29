# CodeBlocker
An IndentedTextWriter that makes generating code blocks easier.

## Usage
```csharp
namespace CodeBlockerExample;

using ktsu.io.CodeBlocker;

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

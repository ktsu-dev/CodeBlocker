// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace CodeBlocker.Tests;

using ktsu.CodeBlocker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class IntegrationTests
{
	[TestMethod]
	public void ComplexCodeGenerationShouldFormatCorrectly()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act - Simulate generating a class with methods

		codeBlocker.WriteLine("public class TestClass");
		using (Scope classScope = new(codeBlocker))
		{
			codeBlocker.WriteLine("public void Method1()");
			using (Scope method1Scope = new(codeBlocker))
			{
				codeBlocker.WriteLine("var x = 1;");
				codeBlocker.WriteLine("Console.WriteLine(x);");
			}

			codeBlocker.NewLine();

			codeBlocker.WriteLine("public void Method2()");
			using Scope method2Scope = new(codeBlocker);
			codeBlocker.WriteLine("if (true)");
			using Scope ifScope = new(codeBlocker);
			codeBlocker.WriteLine("return;");
		}

		// Assert

		string result = codeBlocker.ToString();
		string expected = "public class TestClass\r\n" +
					   "{\r\n" +
					   "\tpublic void Method1()\r\n" +
					   "\t{\r\n" +
					   "\t\tvar x = 1;\r\n" +
					   "\t\tConsole.WriteLine(x);\r\n" +
					   "\t};\r\n" +
					   "\r\n" +
					   "\tpublic void Method2()\r\n" +
					   "\t{\r\n" +
					   "\t\tif (true)\r\n" +
					   "\t\t{\r\n" +
					   "\t\t\treturn;\r\n" +
					   "\t\t};\r\n" +
					   "\t};\r\n" +
					   "};\r\n";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void DeepNestingShouldMaintainCorrectIndentation()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();
		const int nestingLevels = 5;

		// Act

		codeBlocker.WriteLine("start");

		for (int i = 0; i < nestingLevels; i++)
		{
			using Scope scope = new(codeBlocker);
			codeBlocker.WriteLine($"level {i + 1}");
		}

		codeBlocker.WriteLine("end");

		// Assert

		string result = codeBlocker.ToString();

		// Verify it contains the expected structure

		Assert.IsTrue(result.Contains("start\r\n", StringComparison.Ordinal), "Result should contain 'start' line");
		Assert.IsTrue(result.Contains("level 1\r\n", StringComparison.Ordinal), "Result should contain 'level 1' line");
		Assert.IsTrue(result.Contains("level 5\r\n", StringComparison.Ordinal), "Result should contain 'level 5' line");
		Assert.IsTrue(result.Contains("end\r\n", StringComparison.Ordinal), "Result should contain 'end' line");

		// Count opening and closing braces to ensure they match

		int openBraces = result.Count(c => c == '{');
		int closeBraces = result.Count(c => c == '}');
		Assert.AreEqual(openBraces, closeBraces);
	}

	[TestMethod]
	public void MixedContentTypesShouldFormatCorrectly()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act

		codeBlocker.WriteLine("// Header comment");
		codeBlocker.WriteLine("namespace TestNamespace");
		using (Scope namespaceScope = new(codeBlocker))
		{
			codeBlocker.WriteLine("using System;");
			codeBlocker.NewLine();

			codeBlocker.WriteLine("public interface ITest");
			using (Scope interfaceScope = new(codeBlocker))
			{
				codeBlocker.WriteLine("void DoSomething();");
			}

			codeBlocker.NewLine();

			codeBlocker.WriteLine("public class Test : ITest");
			using Scope classScope = new(codeBlocker);
			codeBlocker.WriteLine("public void DoSomething()");
			using Scope methodScope = new(codeBlocker);
			codeBlocker.WriteLine("// Implementation");
		}

		// Assert

		string result = codeBlocker.ToString();

		// Verify structure

		Assert.IsTrue(result.StartsWith("// Header comment\r\n", StringComparison.Ordinal), "Result should start with header comment");
		Assert.IsTrue(result.Contains("namespace TestNamespace\r\n", StringComparison.Ordinal), "Result should contain namespace declaration");
		Assert.IsTrue(result.Contains("\tusing System;\r\n", StringComparison.Ordinal), "Result should contain indented using directive");
		Assert.IsTrue(result.Contains("\tpublic interface ITest\r\n", StringComparison.Ordinal), "Result should contain interface declaration");
		Assert.IsTrue(result.Contains("\t\tvoid DoSomething();\r\n", StringComparison.Ordinal), "Result should contain interface method with double indentation");
		Assert.IsTrue(result.Contains("\tpublic class Test : ITest\r\n", StringComparison.Ordinal), "Result should contain class declaration");
		Assert.IsTrue(result.Contains("\t\tpublic void DoSomething()\r\n", StringComparison.Ordinal), "Result should contain class method with double indentation");
		Assert.IsTrue(result.Contains("\t\t\t// Implementation\r\n", StringComparison.Ordinal), "Result should contain implementation comment with triple indentation");
	}

	[TestMethod]
	public void EmptyScopesShouldNotAffectOtherContent()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act

		codeBlocker.WriteLine("before");

		using (Scope emptyScope = new(codeBlocker))
		{
			// Empty scope

		}

		codeBlocker.WriteLine("after");

		// Assert

		string result = codeBlocker.ToString();
		string expected = "before\r\n{\r\n};\r\nafter\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void MultipleCodeBlockersShouldBeIndependent()
	{
		// Arrange

		using CodeBlocker codeBlocker1 = CodeBlocker.Create();
		using CodeBlocker codeBlocker2 = CodeBlocker.Create();

		// Act

		codeBlocker1.WriteLine("codeBlocker1 content");
		using (Scope scope1 = new(codeBlocker1))
		{
			codeBlocker1.WriteLine("inside scope1");
		}

		codeBlocker2.WriteLine("codeBlocker2 content");
		using (Scope scope2 = new(codeBlocker2))
		{
			codeBlocker2.WriteLine("inside scope2");
		}

		// Assert

		string result1 = codeBlocker1.ToString();
		string result2 = codeBlocker2.ToString();

		string expected1 = "codeBlocker1 content\r\n{\r\n\tinside scope1\r\n};\r\n";
		string expected2 = "codeBlocker2 content\r\n{\r\n\tinside scope2\r\n};\r\n";

		Assert.AreEqual(expected1, result1);
		Assert.AreEqual(expected2, result2);
		Assert.AreNotEqual(result1, result2);
	}

	[TestMethod]
	public void ComplexTemplateGenerationWithMultipleIndentTypesShouldWork()
	{
		// Arrange

		using CodeBlocker htmlBlocker = CodeBlocker.Create("  "); // 2 spaces for HTML

		using CodeBlocker jsBlocker = CodeBlocker.Create("\t"); // Tabs for JS

		// Act - Generate HTML structure

		htmlBlocker.WriteLine("<!DOCTYPE html>");
		htmlBlocker.WriteLine("<html>");
		using (Scope htmlScope = new(htmlBlocker))
		{
			htmlBlocker.WriteLine("<head>");
			using (Scope headScope = new(htmlBlocker))
			{
				htmlBlocker.WriteLine("<title>Test Page</title>");
			}
			htmlBlocker.WriteLine("<body>");
			using (Scope bodyScope = new(htmlBlocker))
			{
				htmlBlocker.WriteLine("<div id=\"content\">");
				using Scope divScope = new(htmlBlocker);
				htmlBlocker.WriteLine("<p>Content here</p>");
			}
		}

		// Generate JavaScript structure

		jsBlocker.WriteLine("function setupPage() ");
		using (Scope functionScope = new(jsBlocker))
		{
			jsBlocker.WriteLine("const content = document.getElementById('content');");
			jsBlocker.WriteLine("if (content) ");
			using (Scope ifScope = new(jsBlocker))
			{
				jsBlocker.WriteLine("content.addEventListener('click', handleClick);");
			}
		}

		// Assert

		string htmlResult = htmlBlocker.ToString();
		string jsResult = jsBlocker.ToString();

		// Verify HTML uses 2-space indentation

		Assert.IsTrue(htmlResult.Contains("  <head>\r\n", StringComparison.Ordinal), "HTML result should contain head tag with 2-space indentation");
		Assert.IsTrue(htmlResult.Contains("    <title>Test Page</title>\r\n", StringComparison.Ordinal), "HTML result should contain title tag with 4-space indentation");

		// Verify JS uses tab indentation

		Assert.IsTrue(jsResult.Contains("\tconst content = document.getElementById('content');\r\n", StringComparison.Ordinal), "JS result should contain const declaration with tab indentation");
		Assert.IsTrue(jsResult.Contains("\t\tcontent.addEventListener('click', handleClick);\r\n", StringComparison.Ordinal), "JS result should contain addEventListener with double tab indentation");
	}

	[TestMethod]
	public void MixedWriteOperationsWithComplexIndentationShouldFormatCorrectly()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act - Mix Write and WriteLine operations

		codeBlocker.Write("public class ");
		codeBlocker.Write("MyClass");
		codeBlocker.WriteLine(" : BaseClass");
		using (Scope classScope = new(codeBlocker))
		{
			codeBlocker.Write("private readonly ");
			codeBlocker.WriteLine("string _field;");
			codeBlocker.NewLine();

			codeBlocker.Write("public ");
			codeBlocker.Write("MyClass");
			codeBlocker.WriteLine("(string field)");
			using (Scope constructorScope = new(codeBlocker))
			{
				codeBlocker.Write("_field = ");
				codeBlocker.Write("field ?? ");
				codeBlocker.WriteLine("throw new ArgumentNullException(nameof(field));");
			}
		}

		// Assert

		string result = codeBlocker.ToString();
		string expected =
			"public class MyClass : BaseClass\r\n" +
			"{\r\n" +
			"\tprivate readonly string _field;\r\n" +
			"\r\n" +
			"\tpublic MyClass(string field)\r\n" +
			"\t{\r\n" +
			"\t\t_field = field ?? throw new ArgumentNullException(nameof(field));\r\n" +
			"\t};\r\n" +
			"};\r\n";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void LargeScaleCodeGenerationShouldPerformReasonably()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();
		const int classCount = 100;
		const int methodsPerClass = 10;

		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

		// Act - Generate large amount of code

		codeBlocker.WriteLine("namespace LargeTest");
		using (Scope namespaceScope = new(codeBlocker))
		{
			for (int classIndex = 0; classIndex < classCount; classIndex++)
			{
				codeBlocker.WriteLine($"public class Class{classIndex}");
				using (Scope classScope = new(codeBlocker))
				{
					for (int methodIndex = 0; methodIndex < methodsPerClass; methodIndex++)
					{
						codeBlocker.WriteLine($"public void Method{methodIndex}()");
						using Scope methodScope = new(codeBlocker);
						codeBlocker.WriteLine($"// Method {methodIndex} implementation");
						codeBlocker.WriteLine($"var result = {methodIndex} * 2;");
						codeBlocker.WriteLine("return result;");
					}
				}

				if (classIndex < classCount - 1)
				{
					codeBlocker.NewLine();
				}
			}
		}

		stopwatch.Stop();

		// Assert

		string result = codeBlocker.ToString();

		// Verify structure exists

		Assert.IsTrue(result.Contains("namespace LargeTest\r\n", StringComparison.Ordinal), "Result should contain namespace declaration");
		Assert.IsTrue(result.Contains("public class Class0\r\n", StringComparison.Ordinal), "Result should contain first class declaration");
		Assert.IsTrue(result.Contains($"public class Class{classCount - 1}\r\n", StringComparison.Ordinal), "Result should contain last class declaration");
		Assert.IsTrue(result.Contains("public void Method0()\r\n", StringComparison.Ordinal), "Result should contain first method declaration");
		Assert.IsTrue(result.Contains($"public void Method{methodsPerClass - 1}()\r\n", StringComparison.Ordinal), "Result should contain last method declaration");

		// Verify performance (should complete in reasonable time)

		Assert.IsLessThan(5000L, stopwatch.ElapsedMilliseconds, $"Code generation took too long: {stopwatch.ElapsedMilliseconds}ms");

		// Verify brace matching

		int openBraces = result.Count(c => c == '{');
		int closeBraces = result.Count(c => c == '}');
		Assert.AreEqual(openBraces, closeBraces);

		// Expected braces: 1 namespace + classCount classes + (classCount * methodsPerClass) methods

		int expectedBraces = 1 + classCount + (classCount * methodsPerClass);
		Assert.AreEqual(expectedBraces, openBraces);
	}

	[TestMethod]
	public void SharedStringWriterBetweenCodeBlockersShouldWork()
	{
		// Arrange

		using StringWriter sharedWriter = new();

		// Act

		using (CodeBlocker codeBlocker1 = new(sharedWriter))
		{
			codeBlocker1.WriteLine("// First CodeBlocker");
			using Scope scope1 = new(codeBlocker1);
			codeBlocker1.WriteLine("content from first");
		}

		using (CodeBlocker codeBlocker2 = new(sharedWriter, "  "))
		{
			codeBlocker2.WriteLine("// Second CodeBlocker with different indent");
			using Scope scope2 = new(codeBlocker2);
			codeBlocker2.WriteLine("content from second");
		}

		// Assert

		string result = sharedWriter.ToString();

		// Verify both CodeBlockers wrote to the same StringWriter

		Assert.IsTrue(result.Contains("// First CodeBlocker\r\n", StringComparison.Ordinal), "Result should contain first CodeBlocker comment");
		Assert.IsTrue(result.Contains("\tcontent from first\r\n", StringComparison.Ordinal), "Result should contain first CodeBlocker content with tab indent");

		Assert.IsTrue(result.Contains("// Second CodeBlocker with different indent\r\n", StringComparison.Ordinal), "Result should contain second CodeBlocker comment");
		Assert.IsTrue(result.Contains("  content from second\r\n", StringComparison.Ordinal), "Result should contain second CodeBlocker content with 2-space indent");

	}

	[TestMethod]
	public void ErrorRecoveryAfterExceptionShouldNotAffectFutureOperations()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act & Assert - Test error recovery
#pragma warning disable CA1031 // Do not catch general exception types - This test specifically needs to catch any potential exception
		try
		{
			// This might throw depending on implementation

			codeBlocker.CurrentIndent = -10;
		}
		catch (Exception)
		{
			// Ignore any exceptions for this resilience test
		}
#pragma warning restore CA1031 // Do not catch general exception types

		// Should still be able to continue normally

		codeBlocker.CurrentIndent = 1;
		codeBlocker.WriteLine("recovered content");

		using (Scope scope = new(codeBlocker))
		{
			codeBlocker.WriteLine("scope content");
		}

		string result = codeBlocker.ToString();
		Assert.IsTrue(result.Contains("recovered content\r\n", StringComparison.Ordinal), "Result should contain recovered content after error");
		Assert.IsTrue(result.Contains("scope content\r\n", StringComparison.Ordinal), "Result should contain scope content after recovery");
	}

	[TestMethod]
	public void UnicodeAndSpecialCharactersShouldBeHandledCorrectly()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create("→→"); // Unicode arrows as indent

		// Act

		codeBlocker.WriteLine("// Unicode test: αβγδε 中文 🚀");
		using (Scope scope = new(codeBlocker))
		{
			codeBlocker.WriteLine("string text = \"Hello 世界!\";");
			codeBlocker.WriteLine("char symbol = '€';");
			codeBlocker.WriteLine("// Special chars: \t\r\n\\\"");
		}

		// Assert

		string result = codeBlocker.ToString();

		Assert.IsTrue(result.Contains("// Unicode test: αβγδε 中文 🚀\r\n", StringComparison.Ordinal), "Result should contain Unicode comment with Greek, Chinese, and emoji characters");
		Assert.IsTrue(result.Contains("→→string text = \"Hello 世界!\";\r\n", StringComparison.Ordinal), "Result should contain string with Chinese characters and Unicode arrow indent");
		Assert.IsTrue(result.Contains("→→char symbol = '€';\r\n", StringComparison.Ordinal), "Result should contain Euro symbol with Unicode arrow indent");
		Assert.IsTrue(result.Contains("→→// Special chars: \t\r\n\\\"", StringComparison.Ordinal), "Result should contain special characters with Unicode arrow indent");
	}
}

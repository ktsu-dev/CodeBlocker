// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.CodeBlocker;

namespace CodeBlocker.Tests;

[TestClass]
public sealed class IntegrationTests
{
	[TestMethod]
	public void ComplexCodeGenerationShouldFormatCorrectly()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act - Simulate generating a class with methods
		codeBlocker.WriteLine("public class TestClass");
		using (var classScope = new Scope(codeBlocker))
		{
			codeBlocker.WriteLine("public void Method1()");
			using (var method1Scope = new Scope(codeBlocker))
			{
				codeBlocker.WriteLine("var x = 1;");
				codeBlocker.WriteLine("Console.WriteLine(x);");
			}

			codeBlocker.NewLine();

			codeBlocker.WriteLine("public void Method2()");
			using (var method2Scope = new Scope(codeBlocker))
			{
				codeBlocker.WriteLine("if (true)");
				using (var ifScope = new Scope(codeBlocker))
				{
					codeBlocker.WriteLine("return;");
				}
			}
		}

		// Assert
		var result = codeBlocker.ToString();
		var expected = "public class TestClass\r\n" +
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
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();
		const int nestingLevels = 5;

		// Act
		codeBlocker.WriteLine("start");

		for (int i = 0; i < nestingLevels; i++)
		{
			using var scope = new Scope(codeBlocker);
			codeBlocker.WriteLine($"level {i + 1}");
		}

		codeBlocker.WriteLine("end");

		// Assert
		var result = codeBlocker.ToString();

		// Verify it contains the expected structure
		Assert.IsTrue(result.Contains("start\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains("level 1\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains("level 5\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains("end\r\n", StringComparison.Ordinal));

		// Count opening and closing braces to ensure they match
		var openBraces = result.Count(c => c == '{');
		var closeBraces = result.Count(c => c == '}');
		Assert.AreEqual(openBraces, closeBraces);
	}

	[TestMethod]
	public void MixedContentTypesShouldFormatCorrectly()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act
		codeBlocker.WriteLine("// Header comment");
		codeBlocker.WriteLine("namespace TestNamespace");
		using (var namespaceScope = new Scope(codeBlocker))
		{
			codeBlocker.WriteLine("using System;");
			codeBlocker.NewLine();

			codeBlocker.WriteLine("public interface ITest");
			using (var interfaceScope = new Scope(codeBlocker))
			{
				codeBlocker.WriteLine("void DoSomething();");
			}

			codeBlocker.NewLine();

			codeBlocker.WriteLine("public class Test : ITest");
			using (var classScope = new Scope(codeBlocker))
			{
				codeBlocker.WriteLine("public void DoSomething()");
				using (var methodScope = new Scope(codeBlocker))
				{
					codeBlocker.WriteLine("// Implementation");
				}
			}
		}

		// Assert
		var result = codeBlocker.ToString();

		// Verify structure
		Assert.IsTrue(result.StartsWith("// Header comment\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains("namespace TestNamespace\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains("\tusing System;\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains("\tpublic interface ITest\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains("\t\tvoid DoSomething();\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains("\tpublic class Test : ITest\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains("\t\tpublic void DoSomething()\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains("\t\t\t// Implementation\r\n", StringComparison.Ordinal));
	}

	[TestMethod]
	public void EmptyScopesShouldNotAffectOtherContent()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act
		codeBlocker.WriteLine("before");

		using (var emptyScope = new Scope(codeBlocker))
		{
			// Empty scope
		}

		codeBlocker.WriteLine("after");

		// Assert
		var result = codeBlocker.ToString();
		var expected = "before\r\n{\r\n};\r\nafter\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void MultipleCodeBlockersShouldBeIndependent()
	{
		// Arrange
		using var codeBlocker1 = ktsu.CodeBlocker.CodeBlocker.Create();
		using var codeBlocker2 = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act
		codeBlocker1.WriteLine("codeBlocker1 content");
		using (var scope1 = new Scope(codeBlocker1))
		{
			codeBlocker1.WriteLine("inside scope1");
		}

		codeBlocker2.WriteLine("codeBlocker2 content");
		using (var scope2 = new Scope(codeBlocker2))
		{
			codeBlocker2.WriteLine("inside scope2");
		}

		// Assert
		var result1 = codeBlocker1.ToString();
		var result2 = codeBlocker2.ToString();

		var expected1 = "codeBlocker1 content\r\n{\r\n\tinside scope1\r\n};\r\n";
		var expected2 = "codeBlocker2 content\r\n{\r\n\tinside scope2\r\n};\r\n";

		Assert.AreEqual(expected1, result1);
		Assert.AreEqual(expected2, result2);
		Assert.AreNotEqual(result1, result2);
	}
}

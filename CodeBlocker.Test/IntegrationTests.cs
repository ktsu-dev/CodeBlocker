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
		Assert.IsTrue(result.Contains("start\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains("level 1\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains("level 5\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains("end\r\n", StringComparison.Ordinal));

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
}

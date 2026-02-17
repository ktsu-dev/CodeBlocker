// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace CodeBlocker.Tests;

using ktsu.CodeBlocker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ScopeWithTrailingSemicolonTests
{
	[TestMethod]
	public void ConstructorShouldOpenBraceAndIncreaseIndentation()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();
		int initialIndent = codeBlocker.CurrentIndent;

		// Act

		using ScopeWithTrailingSemicolon scope = new(codeBlocker);

		// Assert

		Assert.AreEqual(initialIndent + 1, codeBlocker.CurrentIndent);
		string result = codeBlocker.ToString();
		Assert.IsTrue(result.Contains("{\r\n", StringComparison.Ordinal), "Result should contain opening brace with newline");
	}

	[TestMethod]
	public void DisposeShouldCloseBraceWithSemicolonAndDecreaseIndentation()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();
		int initialIndent = codeBlocker.CurrentIndent;
		ScopeWithTrailingSemicolon scope = new(codeBlocker);

		// Act

		scope.Dispose();

		// Assert

		Assert.AreEqual(initialIndent, codeBlocker.CurrentIndent);
		string result = codeBlocker.ToString();
		Assert.IsTrue(result.EndsWith("};\r\n", StringComparison.Ordinal), "Result should end with closing brace, semicolon, and newline");
	}

	[TestMethod]
	public void UsingStatementShouldProperlyOpenAndCloseScope()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act

		using (ScopeWithTrailingSemicolon scope = new(codeBlocker))
		{
			codeBlocker.WriteLine("content inside scope");
		}

		// Assert

		string result = codeBlocker.ToString();
		string expected = "{\r\n\tcontent inside scope\r\n};\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void NestedScopesShouldMaintainProperIndentation()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act

		using (ScopeWithTrailingSemicolon scope1 = new(codeBlocker))
		{
			codeBlocker.WriteLine("level 1");
			using (ScopeWithTrailingSemicolon scope2 = new(codeBlocker))
			{
				codeBlocker.WriteLine("level 2");
			}
			codeBlocker.WriteLine("back to level 1");
		}

		// Assert

		string result = codeBlocker.ToString();
		string expected = "{\r\n\tlevel 1\r\n\t{\r\n\t\tlevel 2\r\n\t};\r\n\tback to level 1\r\n};\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void MultipleDisposeShouldNotThrowException()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();
		ScopeWithTrailingSemicolon scope = new(codeBlocker);

		// Act & Assert

		scope.Dispose();
		scope.Dispose(); // Second call should not throw
	}

	[TestMethod]
	public void ScopeWithoutContentShouldStillFormatCorrectly()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act

		using (ScopeWithTrailingSemicolon scope = new(codeBlocker))
		{
			// No content added
		}

		// Assert

		string result = codeBlocker.ToString();
		string expected = "{\r\n};\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ScopeWithCustomIndentStringShouldWork()
	{
		// Arrange

		const string customIndent = "  "; // Two spaces

		using CodeBlocker codeBlocker = CodeBlocker.Create(customIndent);

		// Act

		using (ScopeWithTrailingSemicolon scope = new(codeBlocker))
		{
			codeBlocker.WriteLine("custom indented content");
		}

		// Assert

		string result = codeBlocker.ToString();
		string expected = "{\r\n  custom indented content\r\n};\r\n";
		Assert.AreEqual(expected, result);
		Assert.AreEqual(customIndent, codeBlocker.IndentString);
	}

	[TestMethod]
	public void ConstructorWithNullCodeBlockerShouldThrowException()
	{
		// Act & Assert - Should throw some exception (likely NullReferenceException)

		Assert.ThrowsExactly<NullReferenceException>(() => new ScopeWithTrailingSemicolon(null!));
	}

	[TestMethod]
	public void ScopeWithDisposedCodeBlockerShouldThrowException()
	{
		// Arrange

		CodeBlocker codeBlocker = CodeBlocker.Create();
		codeBlocker.Dispose();

		// Act & Assert - Should throw when trying to use disposed CodeBlocker

		Assert.ThrowsExactly<ObjectDisposedException>(() => new ScopeWithTrailingSemicolon(codeBlocker));
	}

	[TestMethod]
	public void MixedWithRegularScopeShouldWork()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act - Mix Scope and ScopeWithTrailingSemicolon

		codeBlocker.WriteLine("namespace Test");
		using (Scope namespaceScope = new(codeBlocker))
		{
			codeBlocker.WriteLine("public class Example");
			using (Scope classScope = new(codeBlocker))
			{
				codeBlocker.WriteLine("public enum Color");
				using (ScopeWithTrailingSemicolon enumScope = new(codeBlocker))
				{
					codeBlocker.WriteLine("Red,");
					codeBlocker.WriteLine("Green,");
					codeBlocker.WriteLine("Blue");
				}
			}
		}

		// Assert

		string result = codeBlocker.ToString();
		string expected =
			"namespace Test\r\n" +
			"{\r\n" +
			"\tpublic class Example\r\n" +
			"\t{\r\n" +
			"\t\tpublic enum Color\r\n" +
			"\t\t{\r\n" +
			"\t\t\tRed,\r\n" +
			"\t\t\tGreen,\r\n" +
			"\t\t\tBlue\r\n" +
			"\t\t};\r\n" +
			"\t}\r\n" +
			"}\r\n";

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ScopeWithEmptyCustomIndentStringShouldWork()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create(string.Empty);

		// Act

		using (ScopeWithTrailingSemicolon scope = new(codeBlocker))
		{
			codeBlocker.WriteLine("no indent");
		}

		// Assert

		string result = codeBlocker.ToString();
		string expected = "{\r\nno indent\r\n};\r\n";
		Assert.AreEqual(expected, result);
	}
}

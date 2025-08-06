// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace CodeBlocker.Tests;
using ktsu.CodeBlocker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ScopeTests
{
	[TestMethod]
	public void ConstructorShouldOpenBraceAndIncreaseIndentation()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();
		int initialIndent = codeBlocker.CurrentIndent;

		// Act
		using Scope scope = new(codeBlocker);

		// Assert
		Assert.AreEqual(initialIndent + 1, codeBlocker.CurrentIndent);
		string result = codeBlocker.ToString();
		Assert.IsTrue(result.Contains("{\r\n", StringComparison.Ordinal));
	}

	[TestMethod]
	public void DisposeShouldCloseBraceAndDecreaseIndentation()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();
		int initialIndent = codeBlocker.CurrentIndent;
		Scope scope = new(codeBlocker);

		// Act
		scope.Dispose();

		// Assert
		Assert.AreEqual(initialIndent, codeBlocker.CurrentIndent);
		string result = codeBlocker.ToString();
		Assert.IsTrue(result.EndsWith("};\r\n", StringComparison.Ordinal));
	}

	[TestMethod]
	public void UsingStatementShouldProperlyOpenAndCloseScope()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act
		using (Scope scope = new(codeBlocker))
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
		using (Scope scope1 = new(codeBlocker))
		{
			codeBlocker.WriteLine("level 1");
			using (Scope scope2 = new(codeBlocker))
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
		Scope scope = new(codeBlocker);

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
		using (Scope scope = new(codeBlocker))
		{
			// No content added
		}

		// Assert
		string result = codeBlocker.ToString();
		string expected = "{\r\n};\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void MultipleSequentialScopesShouldFormatCorrectly()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act
		using (Scope scope1 = new(codeBlocker))
		{
			codeBlocker.WriteLine("scope 1 content");
		}

		using (Scope scope2 = new(codeBlocker))
		{
			codeBlocker.WriteLine("scope 2 content");
		}

		// Assert
		string result = codeBlocker.ToString();
		string expected = "{\r\n\tscope 1 content\r\n};\r\n{\r\n\tscope 2 content\r\n};\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ScopeWithCustomIndentStringShouldWork()
	{
		// Arrange
		const string customIndent = "  "; // Two spaces
		using CodeBlocker codeBlocker = CodeBlocker.Create(customIndent);

		// Act
		using (Scope scope = new(codeBlocker))
		{
			codeBlocker.WriteLine("custom indented content");
		}

		// Assert
		string result = codeBlocker.ToString();
		string expected = "{\r\n  custom indented content\r\n};\r\n";
		Assert.AreEqual(expected, result);
		Assert.AreEqual(customIndent, codeBlocker.IndentString);
	}
}

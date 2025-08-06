// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.CodeBlocker;

namespace CodeBlocker.Tests;

[TestClass]
public sealed class ScopeTests
{
	[TestMethod]
	public void ConstructorShouldOpenBraceAndIncreaseIndentation()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();
		var initialIndent = codeBlocker.Indent;

		// Act
		using var scope = new Scope(codeBlocker);

		// Assert
		Assert.AreEqual(initialIndent + 1, codeBlocker.Indent);
		var result = codeBlocker.ToString();
		Assert.IsTrue(result.Contains("{\r\n", StringComparison.Ordinal));
	}

	[TestMethod]
	public void DisposeShouldCloseBraceAndDecreaseIndentation()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();
		var initialIndent = codeBlocker.Indent;
		var scope = new Scope(codeBlocker);

		// Act
		scope.Dispose();

		// Assert
		Assert.AreEqual(initialIndent, codeBlocker.Indent);
		var result = codeBlocker.ToString();
		Assert.IsTrue(result.EndsWith("};\r\n", StringComparison.Ordinal));
	}

	[TestMethod]
	public void UsingStatementShouldProperlyOpenAndCloseScope()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act
		using (var scope = new Scope(codeBlocker))
		{
			codeBlocker.WriteLine("content inside scope");
		}

		// Assert
		var result = codeBlocker.ToString();
		var expected = "{\r\n\tcontent inside scope\r\n};\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void NestedScopesShouldMaintainProperIndentation()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act
		using (var scope1 = new Scope(codeBlocker))
		{
			codeBlocker.WriteLine("level 1");
			using (var scope2 = new Scope(codeBlocker))
			{
				codeBlocker.WriteLine("level 2");
			}
			codeBlocker.WriteLine("back to level 1");
		}

		// Assert
		var result = codeBlocker.ToString();
		var expected = "{\r\n\tlevel 1\r\n\t{\r\n\t\tlevel 2\r\n\t};\r\n\tback to level 1\r\n};\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void MultipleDisposeShouldNotThrowException()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();
		var scope = new Scope(codeBlocker);

		// Act & Assert
		scope.Dispose();
		scope.Dispose(); // Second call should not throw
	}

	[TestMethod]
	public void ScopeWithoutContentShouldStillFormatCorrectly()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act
		using (var scope = new Scope(codeBlocker))
		{
			// No content added
		}

		// Assert
		var result = codeBlocker.ToString();
		var expected = "{\r\n};\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void MultipleSequentialScopesShouldFormatCorrectly()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act
		using (var scope1 = new Scope(codeBlocker))
		{
			codeBlocker.WriteLine("scope 1 content");
		}

		using (var scope2 = new Scope(codeBlocker))
		{
			codeBlocker.WriteLine("scope 2 content");
		}

		// Assert
		var result = codeBlocker.ToString();
		var expected = "{\r\n\tscope 1 content\r\n};\r\n{\r\n\tscope 2 content\r\n};\r\n";
		Assert.AreEqual(expected, result);
	}
}

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

	[TestMethod]
	public void ScopeConstructorWithNullCodeBlockerShouldThrowException()
	{
		// Act & Assert - Should throw some exception (likely NullReferenceException)

		Assert.ThrowsExactly<NullReferenceException>(() => new Scope(null!));
	}

	[TestMethod]
	public void ScopeWithDisposedCodeBlockerShouldThrowException()
	{
		// Arrange

		CodeBlocker codeBlocker = CodeBlocker.Create();
		codeBlocker.Dispose();

		// Act & Assert - Should throw when trying to use disposed CodeBlocker

		Assert.ThrowsExactly<ObjectDisposedException>(() => new Scope(codeBlocker));
	}

	[TestMethod]
	public void ScopeWithVeryDeepNestingShouldWork()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();
		const int nestingLevels = 50;
		List<Scope> scopes = [];

		// Act

		for (int i = 0; i < nestingLevels; i++)
		{
			scopes.Add(new Scope(codeBlocker));
			codeBlocker.WriteLine($"level {i}");
		}

		// Dispose all scopes in reverse order

		for (int i = scopes.Count - 1; i >= 0; i--)
		{
			scopes[i].Dispose();
		}

		// Assert

		string result = codeBlocker.ToString();
		Assert.IsTrue(result.Contains("level 0\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains($"level {nestingLevels - 1}\r\n", StringComparison.Ordinal));

		// Count braces to ensure they match

		int openBraces = result.Count(c => c == '{');
		int closeBraces = result.Count(c => c == '}');
		Assert.AreEqual(openBraces, closeBraces);
		Assert.AreEqual(nestingLevels, openBraces);
	}

	[TestMethod]
	public void ScopeWithMixedManualIndentAndScopeIndentShouldWork()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act

		codeBlocker.Indent(); // Manual indent

		using (Scope scope = new(codeBlocker)) // Scope adds another indent

		{
			codeBlocker.WriteLine("double indented");
			codeBlocker.Outdent(); // Manual outdent within scope

			codeBlocker.WriteLine("single indented");
		}
		codeBlocker.WriteLine("back to manual indent");

		// Assert

		string result = codeBlocker.ToString();
		// Note: After manual Outdent within scope, the closing }; will be at the current indent level
		// The scope ends at whatever the current indent is when Dispose() is called

		string expected = "\t{\r\n\t\tdouble indented\r\n\tsingle indented\r\n};\r\nback to manual indent\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ScopeWithCurrentIndentSetterShouldWork()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act

		using (Scope scope = new(codeBlocker))
		{
			codeBlocker.CurrentIndent = 5; // Set to specific level

			codeBlocker.WriteLine("level 5 content");
		}

		// Assert - After scope disposal and manual CurrentIndent change, final level depends on implementation
		// The Scope's EndScope method decreases indent by 1 from whatever the current level is

		Assert.IsGreaterThanOrEqualTo(0, codeBlocker.CurrentIndent); // Should be reasonable value

		string result = codeBlocker.ToString();
		Assert.IsTrue(result.Contains("{\r\n", StringComparison.Ordinal));
		Assert.IsTrue(result.Contains("\t\t\t\t\tlevel 5 content\r\n", StringComparison.Ordinal)); // 5 tabs

		Assert.IsTrue(result.EndsWith("};\r\n", StringComparison.Ordinal));
	}

	[TestMethod]
	public void ScopeDisposalOrderShouldNotMatterForCorrectness()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act

		Scope scope1 = new(codeBlocker);
		Scope scope2 = new(codeBlocker);
		Scope scope3 = new(codeBlocker);

		codeBlocker.WriteLine("innermost");

		// Dispose in different order than creation

		scope2.Dispose();
		scope1.Dispose();
		scope3.Dispose();

		// Assert - Should still generate valid structure even with out-of-order disposal

		string result = codeBlocker.ToString();
		int openBraces = result.Count(c => c == '{');
		int closeBraces = result.Count(c => c == '}');
		Assert.AreEqual(3, openBraces);
		Assert.AreEqual(3, closeBraces);
	}

	[TestMethod]
	public void ScopeWithEmptyCustomIndentStringShouldWork()
	{
		// Arrange

		using CodeBlocker codeBlocker = CodeBlocker.Create(string.Empty);

		// Act

		using (Scope scope = new(codeBlocker))
		{
			codeBlocker.WriteLine("no indent");
		}

		// Assert

		string result = codeBlocker.ToString();
		string expected = "{\r\nno indent\r\n};\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ScopeAfterManualDisposeOfCodeBlockerShouldThrowException()
	{
		// Arrange

		CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act - Dispose the CodeBlocker while scope is still active

		codeBlocker.Dispose();

		// Assert - Creating scope with disposed CodeBlocker should throw

		Assert.ThrowsExactly<ObjectDisposedException>(() => new Scope(codeBlocker));
	}
}

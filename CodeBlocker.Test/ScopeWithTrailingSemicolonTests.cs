// Copyright (c) 2023-2026 ktsu-dev contributors

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

		using CodeBlocker codeBlocker = TestCodeBlocker.CreateCrLf();
		int initialIndent = codeBlocker.CurrentIndent;

		// Act

		using ScopeWithTrailingSemicolon scope = new(codeBlocker);

		// Assert

		Assert.AreEqual(initialIndent + 1, codeBlocker.CurrentIndent);
		string result = codeBlocker.ToString();
		Assert.IsTrue(result.Contains("{" + Environment.NewLine, StringComparison.Ordinal), "Result should contain opening brace with newline");
	}

	[TestMethod]
	public void DisposeShouldCloseBraceWithSemicolonAndDecreaseIndentation()
	{
		// Arrange

		using CodeBlocker codeBlocker = TestCodeBlocker.CreateCrLf();
		int initialIndent = codeBlocker.CurrentIndent;
		ScopeWithTrailingSemicolon scope = new(codeBlocker);

		// Act

		scope.Dispose();

		// Assert

		Assert.AreEqual(initialIndent, codeBlocker.CurrentIndent);
		string result = codeBlocker.ToString();
		Assert.IsTrue(result.EndsWith("};" + Environment.NewLine, StringComparison.Ordinal), "Result should end with closing brace, semicolon, and newline");
	}

	[TestMethod]
	public void UsingStatementShouldProperlyOpenAndCloseScope()
	{
		// Arrange

		using CodeBlocker codeBlocker = TestCodeBlocker.CreateCrLf();

		// Act

		using (ScopeWithTrailingSemicolon scope = new(codeBlocker))
		{
			codeBlocker.WriteLine("content inside scope");
		}

		// Assert

		string result = codeBlocker.ToString();
		string expected = "{" + Environment.NewLine + "\tcontent inside scope" + Environment.NewLine + "};" + Environment.NewLine;
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void NestedScopesShouldMaintainProperIndentation()
	{
		// Arrange

		using CodeBlocker codeBlocker = TestCodeBlocker.CreateCrLf();

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
		string expected = "{" + Environment.NewLine + "\tlevel 1" + Environment.NewLine + "\t{" + Environment.NewLine + "\t\tlevel 2" + Environment.NewLine + "\t};" + Environment.NewLine + "\tback to level 1" + Environment.NewLine + "};" + Environment.NewLine;
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void MultipleDisposeShouldNotThrowException()
	{
		// Arrange

		using CodeBlocker codeBlocker = TestCodeBlocker.CreateCrLf();
		ScopeWithTrailingSemicolon scope = new(codeBlocker);

		// Act & Assert

		scope.Dispose();
		scope.Dispose(); // Second call should not throw
	}

	[TestMethod]
	public void ScopeWithoutContentShouldStillFormatCorrectly()
	{
		// Arrange

		using CodeBlocker codeBlocker = TestCodeBlocker.CreateCrLf();

		// Act

		using (ScopeWithTrailingSemicolon scope = new(codeBlocker))
		{
			// No content added
		}

		// Assert

		string result = codeBlocker.ToString();
		string expected = "{" + Environment.NewLine + "};" + Environment.NewLine;
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ScopeWithCustomIndentStringShouldWork()
	{
		// Arrange

		const string customIndent = "  "; // Two spaces

		using CodeBlocker codeBlocker = TestCodeBlocker.CreateCrLf(customIndent);

		// Act

		using (ScopeWithTrailingSemicolon scope = new(codeBlocker))
		{
			codeBlocker.WriteLine("custom indented content");
		}

		// Assert

		string result = codeBlocker.ToString();
		string expected = "{" + Environment.NewLine + "  custom indented content" + Environment.NewLine + "};" + Environment.NewLine;
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

		CodeBlocker codeBlocker = TestCodeBlocker.CreateCrLf();
		codeBlocker.Dispose();

		// Act & Assert - Should throw when trying to use disposed CodeBlocker

		Assert.ThrowsExactly<ObjectDisposedException>(() => new ScopeWithTrailingSemicolon(codeBlocker));
	}

	[TestMethod]
	public void MixedWithRegularScopeShouldWork()
	{
		// Arrange

		using CodeBlocker codeBlocker = TestCodeBlocker.CreateCrLf();

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
			"namespace Test" + Environment.NewLine +
			"{" + Environment.NewLine +
			"\tpublic class Example" + Environment.NewLine +
			"\t{" + Environment.NewLine +
			"\t\tpublic enum Color" + Environment.NewLine +
			"\t\t{" + Environment.NewLine +
			"\t\t\tRed," + Environment.NewLine +
			"\t\t\tGreen," + Environment.NewLine +
			"\t\t\tBlue" + Environment.NewLine +
			"\t\t};" + Environment.NewLine +
			"\t}" + Environment.NewLine +
			"}" + Environment.NewLine;

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void ScopeWithEmptyCustomIndentStringShouldWork()
	{
		// Arrange

		using CodeBlocker codeBlocker = TestCodeBlocker.CreateCrLf(string.Empty);

		// Act

		using (ScopeWithTrailingSemicolon scope = new(codeBlocker))
		{
			codeBlocker.WriteLine("no indent");
		}

		// Assert

		string result = codeBlocker.ToString();
		string expected = "{" + Environment.NewLine + "no indent" + Environment.NewLine + "};" + Environment.NewLine;
		Assert.AreEqual(expected, result);
	}
}

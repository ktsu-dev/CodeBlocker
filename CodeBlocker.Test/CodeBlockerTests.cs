// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace CodeBlocker.Tests;
using ktsu.CodeBlocker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class CodeBlockerTests
{
	[TestMethod]
	public void CreateShouldReturnValidInstance()
	{
		// Act
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Assert
		Assert.IsNotNull(codeBlocker);
		Assert.IsInstanceOfType<CodeBlocker>(codeBlocker);
	}

	[TestMethod]
	public void ToStringEmptyCodeBlockerShouldReturnEmptyString()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act
		string result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void WriteLineShouldAddLineWithIndentation()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act
		codeBlocker.WriteLine("test line");
		string result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual("test line\r\n", result);
	}

	[TestMethod]
	public void NewLineShouldAddEmptyLine()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act
		codeBlocker.NewLine();
		string result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual("\r\n", result);
	}

	[TestMethod]
	public void WriteLineWithIndentationShouldRespectIndentLevel()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act
		codeBlocker.Indent();
		codeBlocker.WriteLine("indented line");
		string result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual("\tindented line\r\n", result);
	}

	[TestMethod]
	public void MultipleLinesShouldMaintainProperIndentation()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act
		codeBlocker.WriteLine("line 1");
		codeBlocker.Indent();
		codeBlocker.WriteLine("line 2 indented");
		codeBlocker.Outdent();
		codeBlocker.WriteLine("line 3");
		string result = codeBlocker.ToString();

		// Assert
		string expected = "line 1\r\n\tline 2 indented\r\nline 3\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void DisposeShouldNotThrowException()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act & Assert
		codeBlocker.Dispose(); // Should not throw
	}

	[TestMethod]
	public void DisposeMultipleCallsShouldNotThrow()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act & Assert
		codeBlocker.Dispose();
		codeBlocker.Dispose(); // Second call should not throw
	}

	[TestMethod]
	public void CreateWithCustomIndentStringShouldUseSpecifiedIndent()
	{
		// Arrange
		const string customIndent = "  "; // Two spaces

		// Act
		using CodeBlocker codeBlocker = CodeBlocker.Create(customIndent);
		codeBlocker.Indent();
		codeBlocker.WriteLine("test line");
		string result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual("  test line\r\n", result);
		Assert.AreEqual(customIndent, codeBlocker.IndentString);
	}

	[TestMethod]
	public void ConstructorWithCustomIndentStringShouldWork()
	{
		// Arrange
		using StringWriter stringWriter = new();
		const string customIndent = "    "; // Four spaces

		// Act
		using CodeBlocker codeBlocker = new(stringWriter, customIndent);
		codeBlocker.Indent();
		codeBlocker.WriteLine("indented content");
		string result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual("    indented content\r\n", result);
		Assert.AreEqual(customIndent, codeBlocker.IndentString);
	}

	[TestMethod]
	public void DefaultIndentStringShouldBeTab()
	{
		// Act
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Assert
		Assert.AreEqual("\t", codeBlocker.IndentString);
	}

	[TestMethod]
	public void CustomIndentStringWithMultipleIndentLevels()
	{
		// Arrange
		const string customIndent = ">>"; // Custom string
		using CodeBlocker codeBlocker = CodeBlocker.Create(customIndent);

		// Act
		codeBlocker.WriteLine("level 0");
		codeBlocker.Indent();
		codeBlocker.WriteLine("level 1");
		codeBlocker.Indent();
		codeBlocker.WriteLine("level 2");
		string result = codeBlocker.ToString();

		// Assert
		string expected = "level 0\r\n>>level 1\r\n>>>>level 2\r\n";
		Assert.AreEqual(expected, result);
		Assert.AreEqual(customIndent, codeBlocker.IndentString);
	}
}

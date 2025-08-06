// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.CodeBlocker;

namespace CodeBlocker.Tests;

[TestClass]
public sealed class CodeBlockerTests
{
	[TestMethod]
	public void CreateShouldReturnValidInstance()
	{
		// Act
		var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Assert
		Assert.IsNotNull(codeBlocker);
		Assert.IsInstanceOfType<ktsu.CodeBlocker.CodeBlocker>(codeBlocker);
	}

	[TestMethod]
	public void ToStringEmptyCodeBlockerShouldReturnEmptyString()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act
		var result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void WriteLineShouldAddLineWithIndentation()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act
		codeBlocker.WriteLine("test line");
		var result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual("test line\r\n", result);
	}

	[TestMethod]
	public void NewLineShouldAddEmptyLine()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act
		codeBlocker.NewLine();
		var result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual("\r\n", result);
	}

	[TestMethod]
	public void WriteLineWithIndentationShouldRespectIndentLevel()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act
		codeBlocker.Indent++;
		codeBlocker.WriteLine("indented line");
		var result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual("\tindented line\r\n", result);
	}

	[TestMethod]
	public void MultipleLinesShouldMaintainProperIndentation()
	{
		// Arrange
		using var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act
		codeBlocker.WriteLine("line 1");
		codeBlocker.Indent++;
		codeBlocker.WriteLine("line 2 indented");
		codeBlocker.Indent--;
		codeBlocker.WriteLine("line 3");
		var result = codeBlocker.ToString();

		// Assert
		var expected = "line 1\r\n\tline 2 indented\r\nline 3\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void DisposeShouldNotThrowException()
	{
		// Arrange
		var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act & Assert
		codeBlocker.Dispose(); // Should not throw
	}

	[TestMethod]
	public void DisposeMultipleCallsShouldNotThrow()
	{
		// Arrange
		var codeBlocker = ktsu.CodeBlocker.CodeBlocker.Create();

		// Act & Assert
		codeBlocker.Dispose();
		codeBlocker.Dispose(); // Second call should not throw
	}
}

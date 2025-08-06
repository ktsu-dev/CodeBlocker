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

	[TestMethod]
	public void WriteLineWithoutParametersShouldAddEmptyLineWithIndentation()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act
		codeBlocker.Indent();
		codeBlocker.WriteLine(); // Empty WriteLine
		string result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual("\t\r\n", result);
	}

	[TestMethod]
	public void WriteMethodShouldAddTextWithoutNewline()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act
		codeBlocker.Write("part1");
		codeBlocker.Write("part2");
		string result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual("part1part2", result);
	}

	[TestMethod]
	public void WriteMethodWithIndentationShouldRespectIndentLevel()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act
		codeBlocker.Indent();
		codeBlocker.Write("indented text");
		string result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual("\tindented text", result);
	}

	[TestMethod]
	public void CurrentIndentSetterShouldUpdateIndentationLevel()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act
		codeBlocker.CurrentIndent = 3;
		codeBlocker.WriteLine("test line");
		string result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual(3, codeBlocker.CurrentIndent);
		Assert.AreEqual("\t\t\ttest line\r\n", result);
	}

	[TestMethod]
	public void CurrentIndentSetterWithZeroShouldRemoveIndentation()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();
		codeBlocker.Indent();
		codeBlocker.Indent();

		// Act
		codeBlocker.CurrentIndent = 0;
		codeBlocker.WriteLine("no indent");
		string result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual(0, codeBlocker.CurrentIndent);
		Assert.AreEqual("no indent\r\n", result);
	}

	[TestMethod]
	public void ConstructorWithNullStringWriterShouldThrowArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => new CodeBlocker(null!));
	}

	[TestMethod]
	public void ConstructorWithNullStringWriterAndIndentStringShouldThrowArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => new CodeBlocker(null!, "  "));
	}

	[TestMethod]
	public void CreateWithNullIndentStringShouldWork()
	{
		// Arrange & Act
		using CodeBlocker codeBlocker = CodeBlocker.Create(null!);
		codeBlocker.Indent();
		codeBlocker.WriteLine("test");
		string result = codeBlocker.ToString();

		// Assert - Should work with null indent string (treated as default)
		Assert.IsNotNull(result);
		Assert.IsTrue(result.Contains("test\r\n", StringComparison.Ordinal));
	}

	[TestMethod]
	public void WriteLineWithNullParameterShouldWork()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act
		codeBlocker.WriteLine(null!);
		string result = codeBlocker.ToString();

		// Assert - IndentedTextWriter should handle null gracefully
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public void WriteWithNullParameterShouldWork()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act
		codeBlocker.Write(null!);
		string result = codeBlocker.ToString();

		// Assert - IndentedTextWriter should handle null gracefully
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public void OutdentBelowZeroShouldNotThrow()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act & Assert - Should not throw, but may not go below 0
		codeBlocker.Outdent();
		Assert.IsTrue(codeBlocker.CurrentIndent >= 0); // IndentedTextWriter may clamp to 0

		codeBlocker.WriteLine("test");
		string result = codeBlocker.ToString();
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public void DisposeWithStringWriterManagementShouldNotThrowWhenCalledMultipleTimes()
	{
		// Arrange
		CodeBlocker codeBlocker = CodeBlocker.Create(); // This should manage StringWriter disposal

		// Act & Assert - Should not throw
		codeBlocker.Dispose();
		codeBlocker.Dispose();
		codeBlocker.Dispose();
	}

	[TestMethod]
	public void MixedWriteAndWriteLineShouldFormatCorrectly()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		// Act
		codeBlocker.Write("start ");
		codeBlocker.Write("middle ");
		codeBlocker.WriteLine("end");
		codeBlocker.WriteLine("new line");
		string result = codeBlocker.ToString();

		// Assert
		string expected = "start middle end\r\nnew line\r\n";
		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void DeepIndentationStressTest()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();
		const int maxDepth = 100;

		// Act
		for (int i = 0; i < maxDepth; i++)
		{
			codeBlocker.Indent();
		}

		codeBlocker.WriteLine("deeply nested");
		string result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual(maxDepth, codeBlocker.CurrentIndent);
		Assert.IsTrue(result.StartsWith(new string('\t', maxDepth) + "deeply nested\r\n", StringComparison.Ordinal));
	}

	[TestMethod]
	public void LargeStringContentShouldBeHandledCorrectly()
	{
		// Arrange
		using CodeBlocker codeBlocker = CodeBlocker.Create();
		string largeString = new('x', 10000);

		// Act
		codeBlocker.WriteLine(largeString);
		string result = codeBlocker.ToString();

		// Assert
		Assert.IsTrue(result.Contains(largeString, StringComparison.Ordinal));
		Assert.IsTrue(result.EndsWith("\r\n", StringComparison.Ordinal));
	}

	[TestMethod]
	public void EmptyIndentStringShouldWork()
	{
		// Arrange & Act
		using CodeBlocker codeBlocker = CodeBlocker.Create(string.Empty);
		codeBlocker.Indent();
		codeBlocker.WriteLine("test");
		string result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual(string.Empty, codeBlocker.IndentString);
		Assert.AreEqual("test\r\n", result); // No indentation with empty string
	}

	[TestMethod]
	public void VeryLongIndentStringShouldWork()
	{
		// Arrange
		const string longIndent = "====================================";
		using CodeBlocker codeBlocker = CodeBlocker.Create(longIndent);

		// Act
		codeBlocker.Indent();
		codeBlocker.WriteLine("test");
		string result = codeBlocker.ToString();

		// Assert
		Assert.AreEqual(longIndent, codeBlocker.IndentString);
		Assert.AreEqual(longIndent + "test\r\n", result);
	}
}

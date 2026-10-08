// Copyright (c) 2023-2026 ktsu-dev contributors

namespace CodeBlocker.Tests;

using ktsu.CodeBlocker;
using ktsu.CodeBlocker.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Pins where <see cref="TemplateRendering.SplitLines"/> splits a fragment: at every terminator
/// except one inside a verbatim string literal, however that literal is reached.
/// </summary>
[TestClass]
public sealed class SplitLinesTests
{
	private static string Split(string fragment, string newLine = NewLines.Lf)
	{
		using CodeBlocker parent = CodeBlocker.Create(CodeBlocker.DefaultIndentString, newLine);
		return string.Join("|", TemplateRendering.SplitLines(parent, fragment));
	}

	[TestMethod]
	[DataRow("a\nb\n", "a|b", DisplayName = "Plain lines, trailing terminator dropped")]
	[DataRow("a = @\"p\nq\";\nb", "a = @\"p\nq\";|b", DisplayName = "Verbatim")]
	[DataRow("a = @\"p\"\"\nq\";\nb", "a = @\"p\"\"\nq\";|b", DisplayName = "Verbatim with an escaped quote")]
	[DataRow("a = @\"p\nq", "a = @\"p\nq", DisplayName = "Unterminated verbatim")]
	[DataRow("s = $\"{f(new[] { 1 })}\" + @\"p\nq\";\nb", "s = $\"{f(new[] { 1 })}\" + @\"p\nq\";|b", DisplayName = "Nested braces in a hole")]
	[DataRow("s = $\"{{\" + @\"p\nq\";\nb", "s = $\"{{\" + @\"p\nq\";|b", DisplayName = "Escaped brace in an interpolated string")]
	[DataRow("s = \"\\\"\" + @\"p\nq\";\nb", "s = \"\\\"\" + @\"p\nq\";|b", DisplayName = "Escaped quote in a regular string")]
	[DataRow("s = \"\" + @\"p\nq\";\nb", "s = \"\" + @\"p\nq\";|b", DisplayName = "Empty regular string")]
	[DataRow("s = \"\"\"a\"\"b\"\"\" + @\"p\nq\";\nb", "s = \"\"\"a\"\"b\"\"\" + @\"p\nq\";|b", DisplayName = "Raw string with a shorter run of quotes inside")]
	[DataRow("c = '\\'' + @\"p\nq\";\nb", "c = '\\'' + @\"p\nq\";|b", DisplayName = "Escaped quote in a char literal")]
	[DataRow("@class = 1; // @\"\nb", "@class = 1; // @\"|b", DisplayName = "Verbatim identifier, and a quote in a line comment")]
	[DataRow("c = '\nb", "c = '|b", DisplayName = "Unterminated char literal")]
	[DataRow("s = \"p\nb", "s = \"p|b", DisplayName = "Unterminated regular string")]
	[DataRow("/* @\"\nb", "/* @\"|b", DisplayName = "Unterminated block comment")]
	public void SplitsOnlyOutsideVerbatimStrings(string fragment, string expected) =>
		Assert.AreEqual(expected, Split(fragment));

	[TestMethod]
	public void ACrLfTerminatorInsideAVerbatimStringIsNotASplitPoint() =>
		Assert.AreEqual("a = @\"p\r\nq\";|b", Split("a = @\"p\r\nq\";\r\nb\r\n", NewLines.CrLf));

	[TestMethod]
	public void AnEmptyTerminatorLeavesTheFragmentWhole()
	{
		Assert.AreEqual("a\nb", Split("a\nb", string.Empty));
		Assert.AreEqual(string.Empty, Split(string.Empty, string.Empty));
	}
}

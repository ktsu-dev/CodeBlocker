// Copyright (c) 2023-2026 ktsu-dev contributors

namespace CodeBlocker.Tests;

using ktsu.CodeBlocker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Pins <see cref="CodeBlocker.WriteLines"/>: every line of a multi-line fragment lands at the
/// current indent, line endings follow the writer, and a verbatim literal's value is left alone.
/// </summary>
[TestClass]
public sealed class WriteLinesTests
{
	private const string Nl = CodeBlocker.DefaultNewLineString;

	[TestMethod]
	public void IndentsEveryLineOfAMultiLineFragment()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create();
		using (new Scope(codeBlocker))
		{
			codeBlocker.WriteLines("int a = 1;\nint b = 2;\n");
			codeBlocker.WriteLines("x();\n");
			codeBlocker.WriteLine("y();");
		}

		Assert.AreEqual($"{{{Nl}\tint a = 1;{Nl}\tint b = 2;{Nl}\tx();{Nl}\ty();{Nl}}}{Nl}", codeBlocker.ToString());
	}

	[TestMethod]
	public void NormalisesCrLfAndCrToTheWritersTerminator()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.Indent();
		codeBlocker.WriteLines("a();\r\nb();\rc();\r\n");

		Assert.AreEqual("\ta();\n\tb();\n\tc();\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void LeavesAVerbatimLiteralsLinesAsWritten()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create();
		codeBlocker.Indent();
		codeBlocker.WriteLines("string s = @\"first\r\nsecond\";\nf();\n");

		Assert.AreEqual($"\tstring s = @\"first\r\nsecond\";{Nl}\tf();{Nl}", codeBlocker.ToString());
	}

	[TestMethod]
	public void IndentsEveryLineAfterAVerbatimPrefixInADirective()
	{
		// ktsu-dev/CodeBlocker#194: the @" in the #region name opened a verbatim string that never
		// closed, so the three lines after it were written at column 0.
		using CodeBlocker codeBlocker = CodeBlocker.Create();
		codeBlocker.Indent();
		codeBlocker.WriteLines("#region Handles @\" prefixes\nint a;\nint b;\n#endregion\n");

		Assert.AreEqual($"\t#region Handles @\" prefixes{Nl}\tint a;{Nl}\tint b;{Nl}\t#endregion{Nl}", codeBlocker.ToString());
	}

	[TestMethod]
	public void KeepsBlankLinesFreeOfIndentation()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create();
		codeBlocker.Indent();
		codeBlocker.WriteLines("a();\n\nb();\n");

		Assert.AreEqual($"\ta();{Nl}{Nl}\tb();{Nl}", codeBlocker.ToString());
	}

	[TestMethod]
	public void LeavesTheLastLineOpenWhenTheFragmentHasNoTrailingBreak()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create();
		codeBlocker.Indent();
		codeBlocker.WriteLines("a();\nb(");
		codeBlocker.Write("1);");
		codeBlocker.NewLine();
		codeBlocker.WriteLine("c();");

		Assert.AreEqual($"\ta();{Nl}\tb(1);{Nl}\tc();{Nl}", codeBlocker.ToString());
	}

	[TestMethod]
	public void EndsALineAlreadyUnderWayAtTheFirstBreak()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create();
		codeBlocker.Indent();
		codeBlocker.Write("int a = ");
		codeBlocker.WriteLines("1;\nint b = 2;\n");

		Assert.AreEqual($"\tint a = 1;{Nl}\tint b = 2;{Nl}", codeBlocker.ToString());
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	public void WritesNothingForNullOrEmpty(string? fragment)
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create();
		codeBlocker.Indent();
		codeBlocker.WriteLines(fragment!);
		codeBlocker.NewLine();
		codeBlocker.WriteLine("a();");

		Assert.AreEqual($"{Nl}\ta();{Nl}", codeBlocker.ToString());
	}
}

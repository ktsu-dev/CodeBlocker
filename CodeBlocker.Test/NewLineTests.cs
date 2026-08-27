// Copyright (c) 2023-2026 ktsu-dev contributors

namespace CodeBlocker.Tests;

using ktsu.CodeBlocker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the configurable line terminator.
/// </summary>
/// <remarks>
/// <see cref="System.CodeDom.Compiler.IndentedTextWriter"/> terminates lines with
/// <see cref="Environment.NewLine"/>, so before the terminator became configurable the same calls
/// produced CRLF on Windows and LF elsewhere. Any generator whose output is committed to a
/// repository needs the bytes to be identical wherever it ran, so these tests assert exact bytes
/// rather than comparing against <see cref="Environment.NewLine"/>.
/// </remarks>
[TestClass]
public sealed class NewLineTests
{
	[TestMethod]
	public void TheDefaultTerminatorIsLineFeedRatherThanTheHostTerminator()
	{
		// The point of the default: the same calls give the same bytes on every platform. This
		// assertion is only meaningful on a host whose terminator is not LF, so it is written to
		// fail loudly there rather than to pass vacuously everywhere.
		using CodeBlocker codeBlocker = CodeBlocker.Create();

		Assert.AreEqual(NewLines.Lf, codeBlocker.NewLineString);
		Assert.AreEqual(CodeBlocker.DefaultNewLineString, codeBlocker.NewLineString);
	}

	[TestMethod]
	public void TheHostTerminatorIsStillAvailableByAskingForIt()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Host);

		codeBlocker.WriteLine("a");

		Assert.AreEqual($"a{Environment.NewLine}", codeBlocker.ToString());
	}

	[TestMethod]
	public void CreateWithLfEmitsLineFeedsOnly()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		codeBlocker.WriteLine("a");
		codeBlocker.Indent();
		codeBlocker.WriteLine("b");
		codeBlocker.Outdent();

		Assert.AreEqual("a\n\tb\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void CreateWithCrLfEmitsCarriageReturnLineFeeds()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.CrLf);

		codeBlocker.WriteLine("a");
		codeBlocker.Indent();
		codeBlocker.WriteLine("b");
		codeBlocker.Outdent();

		Assert.AreEqual("a\r\n\tb\r\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void NewLineStringIsReportedBackVerbatim()
	{
		using CodeBlocker lf = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		using CodeBlocker crlf = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.CrLf);

		Assert.AreEqual("\n", lf.NewLineString);
		Assert.AreEqual("\r\n", crlf.NewLineString);
	}

	[TestMethod]
	public void NullNewLineStringFallsBackToTheDefault()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, null!);

		Assert.AreEqual(CodeBlocker.DefaultNewLineString, codeBlocker.NewLineString);
	}

	[TestMethod]
	public void ParameterlessWriteLineUsesTheConfiguredTerminator()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		codeBlocker.WriteLine();

		Assert.AreEqual("\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void BlankLineUsesTheConfiguredTerminator()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		codeBlocker.Indent();
		codeBlocker.NewLine();

		// NewLine() writes without tabs, so the configured terminator is the entire output.
		Assert.AreEqual("\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void ScopesUseTheConfiguredTerminator()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		codeBlocker.WriteLine("if (x)");
		using (new Scope(codeBlocker))
		{
			codeBlocker.WriteLine("y();");
		}

		Assert.AreEqual("if (x)\n{\n\ty();\n}\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void TrailingSemicolonScopesUseTheConfiguredTerminator()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		codeBlocker.WriteLine("enum E");
		using (new ScopeWithTrailingSemicolon(codeBlocker))
		{
			codeBlocker.WriteLine("A,");
		}

		Assert.AreEqual("enum E\n{\n\tA,\n};\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void ATerminatorThatIsNeitherLfNorCrLfIsHonoured()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, "<EOL>");

		codeBlocker.WriteLine("a");

		Assert.AreEqual("a<EOL>", codeBlocker.ToString());
	}

	[TestMethod]
	public void SameCallsWithTheSameTerminatorProduceIdenticalBytes()
	{
		// The point of the option: output is a function of the calls and the configuration, never
		// of the machine it ran on.
		static string Render(string newLineString)
		{
			using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, newLineString);
			codeBlocker.WriteLine("class C");
			using (new Scope(codeBlocker))
			{
				codeBlocker.WriteLine("void M()");
				using (new Scope(codeBlocker))
				{
					codeBlocker.WriteLine("return;");
				}
			}

			return codeBlocker.ToString();
		}

		Assert.AreEqual("class C\n{\n\tvoid M()\n\t{\n\t\treturn;\n\t}\n}\n", Render(NewLines.Lf));
		Assert.AreEqual("class C\r\n{\r\n\tvoid M()\r\n\t{\r\n\t\treturn;\r\n\t}\r\n}\r\n", Render(NewLines.CrLf));
	}

	[TestMethod]
	public void ConstructorOverloadHonoursTheTerminator()
	{
		using StringWriter stringWriter = new();
		using CodeBlocker codeBlocker = new(stringWriter, CodeBlocker.DefaultIndentString, NewLines.Lf);

		codeBlocker.WriteLine("a");

		Assert.AreEqual("a\n", stringWriter.ToString());
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace CodeBlocker.Tests;

using ktsu.CodeBlocker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the scopes added alongside <see cref="Scope"/>: delimiters, bare indentation, and the
/// preprocessor directive pairs.
/// </summary>
/// <remarks>
/// Every fixture pins the line terminator so the expectations are exact bytes rather than
/// host-dependent ones.
/// </remarks>
[TestClass]
public sealed class ScopesTests
{
	private static CodeBlocker Create() => CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

	[TestMethod]
	public void ParenScopeWritesParenthesesAndIndentsTheBody()
	{
		using CodeBlocker codeBlocker = Create();

		codeBlocker.WriteLine("Method");
		using (new ParenScope(codeBlocker))
		{
			codeBlocker.WriteLine("first,");
			codeBlocker.WriteLine("second");
		}

		Assert.AreEqual("Method\n(\n\tfirst,\n\tsecond\n)\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void BracketScopeWritesBracketsAndIndentsTheBody()
	{
		using CodeBlocker codeBlocker = Create();

		codeBlocker.WriteLine("int[] values =");
		using (new BracketScope(codeBlocker))
		{
			codeBlocker.WriteLine("1,");
			codeBlocker.WriteLine("2,");
		}

		Assert.AreEqual("int[] values =\n[\n\t1,\n\t2,\n]\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void IndentScopeIndentsWithoutWritingDelimiters()
	{
		using CodeBlocker codeBlocker = Create();

		codeBlocker.WriteLine("public class Repository<T>");
		using (new IndentScope(codeBlocker))
		{
			codeBlocker.WriteLine("where T : class");
		}

		codeBlocker.WriteLine("{ }");

		Assert.AreEqual("public class Repository<T>\n\twhere T : class\n{ }\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void RegionScopeWrapsTheBodyWithoutIndentingIt()
	{
		using CodeBlocker codeBlocker = Create();

		using (new RegionScope(codeBlocker, "Generated members"))
		{
			codeBlocker.WriteLine("int x;");
		}

		Assert.AreEqual("#region Generated members\nint x;\n#endregion\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void RegionScopeWithoutANameOmitsTheTrailingSpace()
	{
		using CodeBlocker codeBlocker = Create();

		using (new RegionScope(codeBlocker, string.Empty))
		{
			codeBlocker.WriteLine("int x;");
		}

		Assert.AreEqual("#region\nint x;\n#endregion\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void DirectiveScopeWrapsTheBodyInAConditional()
	{
		using CodeBlocker codeBlocker = Create();

		using (new DirectiveScope(codeBlocker, "NET8_0_OR_GREATER"))
		{
			codeBlocker.WriteLine("Span<char> buffer = stackalloc char[16];");
		}

		Assert.AreEqual(
			"#if NET8_0_OR_GREATER\nSpan<char> buffer = stackalloc char[16];\n#endif\n",
			codeBlocker.ToString());
	}

	[TestMethod]
	public void PragmaScopeDisablesAndRestoresTheSameWarnings()
	{
		using CodeBlocker codeBlocker = Create();

		using (new PragmaScope(codeBlocker, "CS1591"))
		{
			codeBlocker.WriteLine("public int Undocumented;");
		}

		Assert.AreEqual(
			"#pragma warning disable CS1591\npublic int Undocumented;\n#pragma warning restore CS1591\n",
			codeBlocker.ToString());
	}

	[TestMethod]
	public void PragmaScopeJoinsSeveralWarnings()
	{
		using CodeBlocker codeBlocker = Create();

		using (new PragmaScope(codeBlocker, ["CS1591", "CA1707"]))
		{
			codeBlocker.WriteLine("public int Undocumented_Name;");
		}

		Assert.AreEqual(
			"#pragma warning disable CS1591, CA1707\npublic int Undocumented_Name;\n#pragma warning restore CS1591, CA1707\n",
			codeBlocker.ToString());
	}

	[TestMethod]
	public void ScopesOfMixedKindsNestCorrectly()
	{
		using CodeBlocker codeBlocker = Create();

		codeBlocker.WriteLine("class C");
		using (new Scope(codeBlocker))
		{
			using (new RegionScope(codeBlocker, "Ctors"))
			{
				codeBlocker.WriteLine("public C");
				using (new ParenScope(codeBlocker))
				{
					codeBlocker.WriteLine("int a,");
					codeBlocker.WriteLine("int b");
				}

				codeBlocker.WriteLine("{ }");
			}
		}

		Assert.AreEqual(
			"""
			class C
			{
				#region Ctors
				public C
				(
					int a,
					int b
				)
				{ }
				#endregion
			}

			""".ReplaceLineEndings("\n"),
			codeBlocker.ToString());
	}

	[TestMethod]
	public void TheClosingDelimiterIsWrittenEvenWhenTheBodyThrows()
	{
		using CodeBlocker codeBlocker = Create();

		try
		{
			using (new ParenScope(codeBlocker))
			{
				codeBlocker.WriteLine("arg");
				throw new InvalidOperationException("boom");
			}
		}
		catch (InvalidOperationException)
		{
			// Expected: the scope's disposal still has to close the delimiter and restore the indent.
		}

		Assert.AreEqual("(\n\targ\n)\n", codeBlocker.ToString());
		Assert.AreEqual(0, codeBlocker.CurrentIndent);
	}

	[TestMethod]
	public void ScopesRestoreTheIndentLevelTheyFound()
	{
		using CodeBlocker codeBlocker = Create();

		codeBlocker.Indent();
		codeBlocker.Indent();
		int before = codeBlocker.CurrentIndent;

		using (new BracketScope(codeBlocker))
		{
			Assert.AreEqual(before + 1, codeBlocker.CurrentIndent);
		}

		Assert.AreEqual(before, codeBlocker.CurrentIndent);
	}

	[TestMethod]
	public void NullCodeBlockerThrowsArgumentNullException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => new ParenScope(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => new BracketScope(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => new IndentScope(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => new RegionScope(null!, "r"));
		Assert.ThrowsExactly<ArgumentNullException>(() => new DirectiveScope(null!, "C"));
		Assert.ThrowsExactly<ArgumentNullException>(() => new PragmaScope(null!, "CS1591"));
	}
}

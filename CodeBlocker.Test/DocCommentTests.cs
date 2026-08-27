// Copyright (c) 2023-2026 ktsu-dev contributors

namespace CodeBlocker.Tests;

using ktsu.CodeBlocker;
using ktsu.CodeBlocker.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers XML documentation modelled as data: escaping, tag order, multi-line layout, and
/// validation against the documented member.
/// </summary>
[TestClass]
public sealed class DocCommentTests
{
	private static string Render(Action<CodeBlocker> write)
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		write(codeBlocker);
		return codeBlocker.ToString();
	}

	private static string Render(DocComment documentation) => Render(documentation.WriteTo);

	[TestMethod]
	public void AnEmptyCommentWritesNothing()
	{
		DocComment documentation = new();

		Assert.IsTrue(documentation.IsEmpty);
		Assert.AreEqual(string.Empty, Render(documentation));
	}

	[TestMethod]
	public void ASingleLineSummaryStaysOnOneLine() =>
		Assert.AreEqual(
			"/// <summary>How many widgets.</summary>\n",
			Render(new DocComment { Summary = "How many widgets." }));

	[TestMethod]
	public void AMultiLineSummaryPrefixesEveryLine()
	{
		DocComment documentation = new()
		{
			Summary = "How many widgets.\nCounted lazily.",
		};

		Assert.AreEqual(
			"""
			/// <summary>
			/// How many widgets.
			/// Counted lazily.
			/// </summary>

			""".ReplaceLineEndings("\n"),
			Render(documentation));
	}

	[TestMethod]
	public void ABlankLineInsideATagIsWrittenWithoutTrailingWhitespace()
	{
		DocComment documentation = new() { Remarks = "First.\n\nSecond." };

		Assert.AreEqual(
			"/// <remarks>\n/// First.\n///\n/// Second.\n/// </remarks>\n",
			Render(documentation));
	}

	[TestMethod]
	public void EitherLineTerminatorSplitsTheText()
	{
		Assert.AreEqual(
			Render(new DocComment { Summary = "a\nb" }),
			Render(new DocComment { Summary = "a\r\nb" }));
	}

	[TestMethod]
	public void TextContentIsEscapedByDefault()
	{
		// "values in the range <0, 1>" is an entirely ordinary thing for metadata to say, and it
		// used to emit malformed XML.
		DocComment documentation = new() { Summary = "Values in the range <0, 1> & beyond." };

		Assert.AreEqual(
			"/// <summary>Values in the range &lt;0, 1&gt; &amp; beyond.</summary>\n",
			Render(documentation));
	}

	[TestMethod]
	public void EscapingCanBeTurnedOffForTextThatEmbedsMarkup()
	{
		DocComment documentation = new()
		{
			EscapeText = false,
			Summary = "Wraps <see cref=\"System.Int32\"/>.",
		};

		Assert.AreEqual(
			"/// <summary>Wraps <see cref=\"System.Int32\"/>.</summary>\n",
			Render(documentation));
	}

	[TestMethod]
	public void AttributeValuesAreAlwaysEscaped()
	{
		DocComment documentation = new() { EscapeText = false };
		documentation.Params.Add(new DocTag { Name = "a<b", Text = "x" });

		Assert.AreEqual("/// <param name=\"a&lt;b\">x</param>\n", Render(documentation));
	}

	[TestMethod]
	public void TagsAreWrittenInCanonicalOrder()
	{
		DocComment documentation = new()
		{
			Remarks = "Remarks.",
			Returns = "The sum.",
			Value = "The value.",
			Summary = "Adds.",
		};
		documentation.SeeAlso.Add("System.Math");
		documentation.Exceptions.Add(new DocTag { Name = "System.OverflowException", Text = "It overflowed." });
		documentation.Params.Add(new DocTag { Name = "a", Text = "First." });
		documentation.TypeParams.Add(new DocTag { Name = "T", Text = "The type." });

		Assert.AreEqual(
			"""
			/// <summary>Adds.</summary>
			/// <typeparam name="T">The type.</typeparam>
			/// <param name="a">First.</param>
			/// <returns>The sum.</returns>
			/// <value>The value.</value>
			/// <exception cref="System.OverflowException">It overflowed.</exception>
			/// <remarks>Remarks.</remarks>
			/// <seealso cref="System.Math"/>

			""".ReplaceLineEndings("\n"),
			Render(documentation));
	}

	[TestMethod]
	public void InheritDocIsWrittenFirst()
	{
		DocComment bare = new() { InheritDoc = true };
		DocComment withCref = new() { InheritDoc = true, InheritDocCref = "IWidget.Count", Summary = "Count." };

		Assert.AreEqual("/// <inheritdoc/>\n", Render(bare));
		Assert.AreEqual(
			"/// <inheritdoc cref=\"IWidget.Count\"/>\n/// <summary>Count.</summary>\n",
			Render(withCref));
	}

	[TestMethod]
	public void ValidationAcceptsAMatchingComment()
	{
		DocComment documentation = new();
		documentation.Params.Add(new DocTag { Name = "a", Text = "First." });
		documentation.TypeParams.Add(new DocTag { Name = "T", Text = "The type." });

		Assert.IsEmpty(documentation.Validate(["a"], ["T"]));
	}

	[TestMethod]
	public void ValidationReportsATagThatNamesNothing()
	{
		DocComment documentation = new();
		documentation.Params.Add(new DocTag { Name = "typo", Text = "First." });

		IReadOnlyList<string> issues = documentation.Validate(["a"], []);

		Assert.HasCount(2, issues);
		Assert.Contains("typo", issues[0]);
		Assert.Contains("'a' has no <param> entry", issues[1]);
	}

	[TestMethod]
	public void ValidationReportsADuplicateTag()
	{
		DocComment documentation = new();
		documentation.Params.Add(new DocTag { Name = "a", Text = "First." });
		documentation.Params.Add(new DocTag { Name = "a", Text = "Again." });

		IReadOnlyList<string> issues = documentation.Validate(["a"], []);

		Assert.HasCount(1, issues);
		Assert.Contains("more than once", issues[0]);
	}

	[TestMethod]
	public void ValidationRejectsNullArguments()
	{
		DocComment documentation = new();

		Assert.ThrowsExactly<ArgumentNullException>(() => documentation.Validate(null!, []));
		Assert.ThrowsExactly<ArgumentNullException>(() => documentation.Validate([], null!));
	}

	[TestMethod]
	public void ATemplateWritesItsDocumentationAboveItsAttributes()
	{
		MethodTemplate method = new()
		{
			Type = "int",
			Name = "Add",
			Keywords = { "public" },
			Attributes = { "Pure" },
			Comments = { "// Runs in constant time." },
			Parameters =
			{
				new ParameterTemplate { Type = "int", Name = "a" },
				new ParameterTemplate { Type = "int", Name = "b" },
			},
			Documentation = new DocComment
			{
				Summary = "Adds two numbers.",
				Returns = "Their sum.",
				Params =
				{
					new DocTag { Name = "a", Text = "The first." },
					new DocTag { Name = "b", Text = "The second." },
				},
			},
			BodyFactory = codeBlocker => codeBlocker.Write("=> a + b;"),
		};

		Assert.AreEqual(
			"""
			/// <summary>Adds two numbers.</summary>
			/// <param name="a">The first.</param>
			/// <param name="b">The second.</param>
			/// <returns>Their sum.</returns>
			// Runs in constant time.
			[Pure]
			public int Add(int a, int b) => a + b;

			""".ReplaceLineEndings("\n"),
			Render(method.WriteTo));
	}

	[TestMethod]
	public void NullCodeBlockerIsRejected() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => new DocComment().WriteTo(null!));
}

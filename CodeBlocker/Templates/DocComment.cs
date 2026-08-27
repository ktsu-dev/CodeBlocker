// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

using Polyfills;
using System.Collections.ObjectModel;
using System.Text;

/// <summary>
/// One named XML documentation tag: a <c>&lt;param&gt;</c>, <c>&lt;typeparam&gt;</c> or
/// <c>&lt;exception&gt;</c> entry.
/// </summary>
public class DocTag
{
	/// <summary>
	/// Gets or sets the tag's identifying attribute: the parameter or type parameter name, or for an
	/// exception the <c>cref</c>.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>Gets or sets the tag's description.</summary>
	public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Describes a member's XML documentation as data rather than as pre-formatted comment lines.
/// </summary>
/// <remarks>
/// Assembling doc comments by hand means every generator re-derives which tags go in which order,
/// how a long description wraps, and how a multi-line <c>&lt;remarks&gt;</c> is prefixed — and
/// nothing escapes the content, so a description that happens to contain <c>&lt;</c> or <c>&amp;</c>
/// produces malformed XML and trips the compiler's doc-comment warnings. This owns all of that.
/// </remarks>
public class DocComment
{
	/// <summary>
	/// Gets or sets a value indicating whether text content is XML-escaped when written. Defaults to
	/// <see langword="true"/>; clear it when the text deliberately embeds markup such as
	/// <c>&lt;c&gt;</c> or <c>&lt;see cref="…"/&gt;</c>, in which case escaping is the caller's job.
	/// </summary>
	public bool EscapeText { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether the documentation is inherited, written as
	/// <c>&lt;inheritdoc/&gt;</c> ahead of anything else.
	/// </summary>
	public bool InheritDoc { get; set; }

	/// <summary>
	/// Gets or sets the <c>cref</c> of the member to inherit documentation from. Ignored unless
	/// <see cref="InheritDoc"/> is set; empty writes a bare <c>&lt;inheritdoc/&gt;</c>.
	/// </summary>
	public string InheritDocCref { get; set; } = string.Empty;

	/// <summary>Gets or sets the <c>&lt;summary&gt;</c> text.</summary>
	public string? Summary { get; set; }

	/// <summary>Gets or sets the <c>&lt;remarks&gt;</c> text.</summary>
	public string? Remarks { get; set; }

	/// <summary>Gets or sets the <c>&lt;returns&gt;</c> text.</summary>
	public string? Returns { get; set; }

	/// <summary>Gets or sets the <c>&lt;value&gt;</c> text, describing what a property holds.</summary>
	public string? Value { get; set; }

	/// <summary>Gets the <c>&lt;typeparam&gt;</c> entries, keyed by type parameter name.</summary>
	public Collection<DocTag> TypeParams { get; } = [];

	/// <summary>Gets the <c>&lt;param&gt;</c> entries, keyed by parameter name.</summary>
	public Collection<DocTag> Params { get; } = [];

	/// <summary>Gets the <c>&lt;exception&gt;</c> entries, keyed by exception type <c>cref</c>.</summary>
	public Collection<DocTag> Exceptions { get; } = [];

	/// <summary>Gets the <c>&lt;seealso&gt;</c> <c>cref</c> values.</summary>
	public Collection<string> SeeAlso { get; } = [];

	/// <summary>
	/// Gets a value indicating whether this comment would write anything at all.
	/// </summary>
	public bool IsEmpty =>
		!InheritDoc
		&& string.IsNullOrEmpty(Summary)
		&& string.IsNullOrEmpty(Remarks)
		&& string.IsNullOrEmpty(Returns)
		&& string.IsNullOrEmpty(Value)
		&& TypeParams.Count == 0
		&& Params.Count == 0
		&& Exceptions.Count == 0
		&& SeeAlso.Count == 0;

	/// <summary>
	/// Writes the documentation in the canonical tag order: <c>inheritdoc</c>, <c>summary</c>,
	/// <c>typeparam</c>, <c>param</c>, <c>returns</c>, <c>value</c>, <c>exception</c>,
	/// <c>remarks</c>, <c>seealso</c>.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	public void WriteTo(CodeBlocker codeBlocker)
	{
		Ensure.NotNull(codeBlocker);

		if (InheritDoc)
		{
			codeBlocker.WriteLine(string.IsNullOrEmpty(InheritDocCref)
				? "/// <inheritdoc/>"
				: $"/// <inheritdoc cref=\"{EscapeAttribute(InheritDocCref)}\"/>");
		}

		WriteElement(codeBlocker, "summary", null, Summary);

		foreach (DocTag typeParam in TypeParams)
		{
			WriteElement(codeBlocker, "typeparam", $" name=\"{EscapeAttribute(typeParam.Name)}\"", typeParam.Text);
		}

		foreach (DocTag param in Params)
		{
			WriteElement(codeBlocker, "param", $" name=\"{EscapeAttribute(param.Name)}\"", param.Text);
		}

		WriteElement(codeBlocker, "returns", null, Returns);
		WriteElement(codeBlocker, "value", null, Value);

		foreach (DocTag exception in Exceptions)
		{
			WriteElement(codeBlocker, "exception", $" cref=\"{EscapeAttribute(exception.Name)}\"", exception.Text);
		}

		WriteElement(codeBlocker, "remarks", null, Remarks);

		foreach (string cref in SeeAlso)
		{
			codeBlocker.WriteLine($"/// <seealso cref=\"{EscapeAttribute(cref)}\"/>");
		}
	}

	/// <summary>
	/// Checks that every <c>&lt;param&gt;</c> and <c>&lt;typeparam&gt;</c> entry names something the
	/// documented member actually declares, and that nothing is documented twice.
	/// </summary>
	/// <param name="parameterNames">The member's parameter names.</param>
	/// <param name="typeParameterNames">The member's type parameter names.</param>
	/// <returns>
	/// One message per problem found, empty when there are none. Nothing is thrown: a generator can
	/// report these as build diagnostics, which is far easier to act on than the CS1572 and CS1573
	/// warnings the mismatch would otherwise raise inside generated source.
	/// </returns>
	/// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
	public IReadOnlyList<string> Validate(IEnumerable<string> parameterNames, IEnumerable<string> typeParameterNames)
	{
		Ensure.NotNull(parameterNames);
		Ensure.NotNull(typeParameterNames);

		List<string> issues = [];
		Check(Params, [.. parameterNames], "param", "parameter");
		Check(TypeParams, [.. typeParameterNames], "typeparam", "type parameter");
		return issues;

		void Check(Collection<DocTag> tags, HashSet<string> declared, string tagName, string what)
		{
			HashSet<string> documented = [];
			foreach (string name in tags.Select(tag => tag.Name))
			{
				if (!documented.Add(name))
				{
					issues.Add($"<{tagName} name=\"{name}\"> is documented more than once.");
				}

				if (!declared.Contains(name))
				{
					issues.Add($"<{tagName} name=\"{name}\"> does not match any declared {what}.");
				}
			}

			foreach (string name in declared.Where(name => !documented.Contains(name)))
			{
				issues.Add($"The {what} '{name}' has no <{tagName}> entry.");
			}
		}
	}

	private void WriteElement(CodeBlocker codeBlocker, string tagName, string? attributes, string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		string[] lines = SplitLines(text!);
		if (lines.Length == 1)
		{
			codeBlocker.WriteLine($"/// <{tagName}{attributes}>{Escape(lines[0])}</{tagName}>");
			return;
		}

		codeBlocker.WriteLine($"/// <{tagName}{attributes}>");
		foreach (string line in lines)
		{
			codeBlocker.WriteLine(line.Length == 0 ? "///" : $"/// {Escape(line)}");
		}

		codeBlocker.WriteLine($"/// </{tagName}>");
	}

	private string Escape(string text) => EscapeText ? EscapeContent(text) : text;

	/// <summary>
	/// Splits text into lines, accepting either line terminator so that a caller's verbatim or raw
	/// string literal lays out the same way whichever platform the source file was written on.
	/// </summary>
	/// <param name="text">The text to split.</param>
	/// <returns>The text's lines.</returns>
	private static string[] SplitLines(string text) =>
		text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

	/// <summary>
	/// Escapes the characters that would otherwise be read as markup in element content.
	/// </summary>
	/// <param name="text">The text to escape.</param>
	/// <returns>The escaped text.</returns>
	private static string EscapeContent(string text)
	{
		StringBuilder builder = new(text.Length);
		foreach (char character in text)
		{
			switch (character)
			{
				case '&':
					builder.Append("&amp;");
					break;

				case '<':
					builder.Append("&lt;");
					break;

				case '>':
					builder.Append("&gt;");
					break;

				default:
					builder.Append(character);
					break;
			}
		}

		return builder.ToString();
	}

	/// <summary>
	/// Escapes an attribute value. Always escaped, regardless of <see cref="EscapeText"/>: an
	/// attribute value is never a place to embed markup, so there is nothing to opt out of.
	/// </summary>
	/// <param name="value">The value to escape.</param>
	/// <returns>The escaped value.</returns>
	private static string EscapeAttribute(string value) =>
		EscapeContent(value).Replace("\"", "&quot;");
}

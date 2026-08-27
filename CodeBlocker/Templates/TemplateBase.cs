// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

using System.Collections.ObjectModel;

/// <summary>
/// Base class for every template: the parts that a declaration of any kind can carry.
/// </summary>
/// <remarks>
/// A template describes <em>what</em> to emit and leaves the punctuation, spacing and ordering to
/// the model. Build a tree of templates, then render it with
/// <see cref="SourceFileTemplateExtensions.AddSourceFile"/> or by calling <see cref="WriteTo"/>.
/// </remarks>
public abstract class TemplateBase
{
	/// <summary>Gets or sets the declared name.</summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>Gets or sets the declared type, written before <see cref="Name"/>.</summary>
	public string Type { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the initialiser or default value. Empty means no initialiser is written.
	/// </summary>
	public string DefaultValue { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets a value indicating whether <see cref="DefaultValue"/> is written inside double
	/// quotes. Set this for string literals; leave it clear for anything already written as an
	/// expression.
	/// </summary>
	public bool DefaultValueIsQuoted { get; set; }

	/// <summary>
	/// Gets the attributes to write before the declaration, each without its square brackets.
	/// </summary>
	public Collection<string> Attributes { get; } = [];

	/// <summary>
	/// Gets the modifiers to write before the declaration, in the order they should appear —
	/// for example <c>public</c>, <c>static</c>, <c>partial</c>. The declaration keyword is not a
	/// modifier: <see cref="ClassTemplate.Kind"/> supplies it.
	/// </summary>
	public Collection<string> Keywords { get; } = [];

	/// <summary>
	/// Gets the comment lines to write above the declaration, each written verbatim and so
	/// including its own <c>//</c> or <c>///</c> prefix.
	/// </summary>
	/// <remarks>
	/// This is the escape hatch, for a plain comment or for anything
	/// <see cref="Documentation"/> does not model. Prefer <see cref="Documentation"/> for XML
	/// documentation: it escapes the content and orders the tags.
	/// </remarks>
	public Collection<string> Comments { get; } = [];

	/// <summary>
	/// Gets or sets the XML documentation, written above <see cref="Comments"/>.
	/// <see langword="null"/> writes none.
	/// </summary>
	public DocComment? Documentation { get; set; }

	/// <summary>
	/// Writes the comments, attributes and modifiers this template carries, leaving the declaration
	/// line open for the derived template to continue.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	public virtual void WriteTo(CodeBlocker codeBlocker)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);

		codeBlocker.AddDocumentation(Documentation);
		codeBlocker.AddComments(Comments);
		codeBlocker.AddAttributes(Attributes);
		codeBlocker.AddKeywords(Keywords);
	}

	/// <summary>
	/// Writes <c> = value</c> when <see cref="DefaultValue"/> is set, quoting it when
	/// <see cref="DefaultValueIsQuoted"/> is set.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	protected void WriteDefaultValueTo(CodeBlocker codeBlocker)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);

		if (string.IsNullOrEmpty(DefaultValue))
		{
			return;
		}

		codeBlocker.Write(" = ");
		codeBlocker.Write(DefaultValueIsQuoted ? $"\"{DefaultValue}\"" : DefaultValue);
	}
}

/// <summary>
/// Extension methods for writing the parts shared by every template.
/// </summary>
public static class TemplateBaseExtensions
{
	/// <summary>
	/// Writes the comments, attributes and modifiers a template carries.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="template">The template whose shared parts to write.</param>
	/// <returns>The same <see cref="CodeBlocker"/>, for chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="codeBlocker"/> or <paramref name="template"/> is <see langword="null"/>.
	/// </exception>
	public static CodeBlocker AddTemplate(this CodeBlocker codeBlocker, TemplateBase template)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);
		ArgumentNullException.ThrowIfNull(template);

		codeBlocker.AddDocumentation(template.Documentation);
		codeBlocker.AddComments(template.Comments);
		codeBlocker.AddAttributes(template.Attributes);
		codeBlocker.AddKeywords(template.Keywords);
		return codeBlocker;
	}

	/// <summary>
	/// Writes XML documentation, or nothing when there is none to write.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="documentation">The documentation, or <see langword="null"/>.</param>
	/// <returns>The same <see cref="CodeBlocker"/>, for chaining.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	public static CodeBlocker AddDocumentation(this CodeBlocker codeBlocker, DocComment? documentation)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);

		documentation?.WriteTo(codeBlocker);
		return codeBlocker;
	}

	/// <summary>
	/// Writes each comment on its own line, verbatim.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="comments">The comment lines, each including its own prefix.</param>
	/// <returns>The same <see cref="CodeBlocker"/>, for chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="codeBlocker"/> or <paramref name="comments"/> is <see langword="null"/>.
	/// </exception>
	public static CodeBlocker AddComments(this CodeBlocker codeBlocker, IEnumerable<string> comments)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);
		ArgumentNullException.ThrowIfNull(comments);

		foreach (string comment in comments)
		{
			codeBlocker.WriteLine(comment);
		}

		return codeBlocker;
	}

	/// <summary>
	/// Writes each attribute on its own line, bracketed.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="attributes">The attributes, each without its square brackets.</param>
	/// <returns>The same <see cref="CodeBlocker"/>, for chaining.</returns>
	/// <remarks>
	/// One per line rather than inline: a member carrying several attributes — and generated members
	/// often carry a suppression apiece — otherwise produces a line long enough to hide the
	/// declaration at the end of it.
	/// </remarks>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="codeBlocker"/> or <paramref name="attributes"/> is <see langword="null"/>.
	/// </exception>
	public static CodeBlocker AddAttributes(this CodeBlocker codeBlocker, IEnumerable<string> attributes)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);
		ArgumentNullException.ThrowIfNull(attributes);

		foreach (string attribute in attributes)
		{
			codeBlocker.WriteLine($"[{attribute}]");
		}

		return codeBlocker;
	}

	/// <summary>
	/// Writes each attribute bracketed and inline, followed by a space.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="attributes">The attributes, each without its square brackets.</param>
	/// <returns>The same <see cref="CodeBlocker"/>, for chaining.</returns>
	/// <remarks>
	/// Used for parameters, where an attribute has to stay on the declaration line.
	/// </remarks>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="codeBlocker"/> or <paramref name="attributes"/> is <see langword="null"/>.
	/// </exception>
	public static CodeBlocker AddInlineAttributes(this CodeBlocker codeBlocker, IEnumerable<string> attributes)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);
		ArgumentNullException.ThrowIfNull(attributes);

		foreach (string attribute in attributes)
		{
			codeBlocker.Write($"[{attribute}] ");
		}

		return codeBlocker;
	}

	/// <summary>
	/// Writes the modifiers space-separated, followed by a trailing space when there is at least one.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="keywords">The modifiers, in the order they should appear.</param>
	/// <returns>The same <see cref="CodeBlocker"/>, for chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="codeBlocker"/> or <paramref name="keywords"/> is <see langword="null"/>.
	/// </exception>
	public static CodeBlocker AddKeywords(this CodeBlocker codeBlocker, IEnumerable<string> keywords)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);
		ArgumentNullException.ThrowIfNull(keywords);

		if (keywords.Any())
		{
			codeBlocker.Write(string.Join(" ", keywords) + " ");
		}

		return codeBlocker;
	}
}

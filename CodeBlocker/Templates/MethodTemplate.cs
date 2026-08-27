// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

using System.Collections.ObjectModel;

/// <summary>
/// Describes a method declaration.
/// </summary>
public class MethodTemplate : MemberTemplate
{
	/// <summary>Gets the method's type parameters, written as <c>&lt;T, TResult&gt;</c>.</summary>
	public Collection<string> TypeParameters { get; } = [];

	/// <summary>Gets the method's parameters, in declaration order.</summary>
	public Collection<ParameterTemplate> Parameters { get; } = [];

	/// <summary>
	/// Gets the generic constraint clauses, each written verbatim on its own indented line — for
	/// example <c>where T : struct</c>.
	/// </summary>
	public Collection<string> Constraints { get; } = [];

	/// <summary>
	/// Gets or sets the callback that writes the method body. <see langword="null"/> declares the
	/// method without a body — an abstract, partial or interface declaration — and terminates it
	/// with a semicolon.
	/// </summary>
	/// <remarks>
	/// The callback supplies its own braces, or writes an expression body such as
	/// <c>=&gt; value;</c>. It renders into a nested <see cref="CodeBlocker"/> configured like this
	/// one, and every line of the result is re-indented to the position it is spliced into.
	/// </remarks>
	public Action<CodeBlocker>? BodyFactory { get; set; }

	/// <summary>
	/// Writes the declaration, its type parameters, its parameter list, its constraints, and its body.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	public override void WriteTo(CodeBlocker codeBlocker)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);

		base.WriteTo(codeBlocker);
		TemplateRendering.WriteTypeParameterList(codeBlocker, TypeParameters);
		TemplateRendering.WriteParameterList(codeBlocker, Parameters);
		TemplateRendering.WriteConstraints(codeBlocker, Constraints);
		TemplateRendering.WriteBody(codeBlocker, BodyFactory);
	}
}

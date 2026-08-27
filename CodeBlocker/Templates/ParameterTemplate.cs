// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

using Polyfills;

/// <summary>
/// Describes one parameter of a method, constructor, operator, or positional record.
/// </summary>
public class ParameterTemplate : TemplateBase
{
	/// <summary>
	/// Writes the parameter as <c>Type name</c>, followed by <c> = default</c> when
	/// <see cref="TemplateBase.DefaultValue"/> is set.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <remarks>
	/// A parameter's attributes and modifiers stay on the declaration line — <c>[In] ref int x</c> —
	/// so this does not go through the base implementation, which puts attributes on their own line.
	/// </remarks>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	public override void WriteTo(CodeBlocker codeBlocker)
	{
		Ensure.NotNull(codeBlocker);

		codeBlocker.AddInlineAttributes(Attributes);
		codeBlocker.AddKeywords(Keywords);
		codeBlocker.Write($"{Type} {Name}");
		WriteDefaultValueTo(codeBlocker);
	}
}

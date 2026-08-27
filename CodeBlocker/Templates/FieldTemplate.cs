// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

/// <summary>
/// Describes a field declaration.
/// </summary>
public class FieldTemplate : MemberTemplate
{
	/// <summary>
	/// Writes the field, its initialiser when <see cref="TemplateBase.DefaultValue"/> is set, and
	/// the terminating semicolon.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	public override void WriteTo(CodeBlocker codeBlocker)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);

		base.WriteTo(codeBlocker);
		WriteDefaultValueTo(codeBlocker);
		codeBlocker.WriteLine(";");
	}
}

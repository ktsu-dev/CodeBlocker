// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

using Polyfills;

/// <summary>
/// Describes one member of an enum.
/// </summary>
/// <remarks>
/// Only <see cref="TemplateBase.Name"/> and, optionally,
/// <see cref="TemplateBase.DefaultValue"/> apply: an enum member has no type of its own.
/// </remarks>
public class EnumMemberTemplate : MemberTemplate
{
	/// <summary>
	/// Writes the member as <c>Name</c> or <c>Name = value</c>, followed by a comma.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <remarks>
	/// Every member gets a trailing comma, the last one included: it is legal C#, and it keeps the
	/// diff to one line when a member is appended.
	/// </remarks>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	public override void WriteTo(CodeBlocker codeBlocker)
	{
		Ensure.NotNull(codeBlocker);

		base.WriteTo(codeBlocker);
		WriteDefaultValueTo(codeBlocker);
		codeBlocker.WriteLine(",");
	}
}

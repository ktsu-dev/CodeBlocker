// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

using Polyfills;

/// <summary>
/// Base class for anything declared inside a type.
/// </summary>
public abstract class MemberTemplate : TemplateBase
{
	/// <summary>
	/// Writes the shared parts, then the member's type and name.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	public override void WriteTo(CodeBlocker codeBlocker)
	{
		Ensure.NotNull(codeBlocker);

		base.WriteTo(codeBlocker);

		// Trimmed because a constructor has no type, which would otherwise leave a leading space.
		codeBlocker.Write($"{Type} {Name}".Trim());
	}

	/// <summary>
	/// The order members are emitted in within a type: enum members and fields first, then
	/// constructors, properties, methods and operators.
	/// </summary>
	/// <param name="memberTemplate">The member to rank.</param>
	/// <returns>The member's sort key.</returns>
	/// <remarks>
	/// The sort is stable, so members of the same kind keep the order they were added in. Set
	/// <see cref="ClassTemplate.SortMembers"/> to <see langword="false"/> to keep declaration order
	/// across kinds too.
	/// </remarks>
	public static int MemberSortOrder(MemberTemplate memberTemplate) =>
		memberTemplate switch
		{
			EnumMemberTemplate => 0,
			FieldTemplate => 1,
			ConstructorTemplate => 2,
			PropertyTemplate => 3,
			MethodTemplate => 4,
			OperatorTemplate => 5,
			_ => 6
		};
}

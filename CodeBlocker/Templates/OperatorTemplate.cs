// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

using System.Collections.ObjectModel;

/// <summary>
/// How an operator is declared.
/// </summary>
public enum OperatorKind
{
	/// <summary>
	/// An ordinary operator: <c>public static Result operator *(Left left, Right right)</c>.
	/// </summary>
	Normal,

	/// <summary>
	/// An implicit conversion: <c>public static implicit operator Result(Source value)</c>.
	/// </summary>
	Implicit,

	/// <summary>
	/// An explicit conversion: <c>public static explicit operator Result(Source value)</c>.
	/// </summary>
	Explicit,
}

/// <summary>
/// Describes an operator or conversion declaration.
/// </summary>
/// <remarks>
/// <see cref="TemplateBase.Type"/> is the result type, and <see cref="TemplateBase.Name"/> is
/// unused — an operator is named by its symbol. Remember that operators are always
/// <c>public static</c>, so both belong in <see cref="TemplateBase.Keywords"/>.
/// </remarks>
public class OperatorTemplate : MemberTemplate
{
	/// <summary>Gets or sets how the operator is declared.</summary>
	public OperatorKind Kind { get; set; } = OperatorKind.Normal;

	/// <summary>
	/// Gets or sets the operator symbol, for example <c>*</c>, <c>==</c> or <c>true</c>. Ignored for
	/// a conversion, which is named by its result type instead.
	/// </summary>
	public string Symbol { get; set; } = string.Empty;

	/// <summary>Gets the operator's parameters, in declaration order.</summary>
	public Collection<ParameterTemplate> Parameters { get; } = [];

	/// <summary>
	/// Gets or sets the callback that writes the operator body. <see langword="null"/> declares it
	/// without one, which is only valid in a partial declaration.
	/// </summary>
	public Action<CodeBlocker>? BodyFactory { get; set; }

	/// <summary>
	/// Writes the declaration, its parameter list, and its body.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	public override void WriteTo(CodeBlocker codeBlocker)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);

		// Not through the MemberTemplate implementation: an operator's signature is its symbol and
		// result type, not a type followed by a name.
		codeBlocker.AddTemplate(this);

		switch (Kind)
		{
			case OperatorKind.Normal:
				codeBlocker.Write($"{Type} operator {Symbol}");
				break;

			case OperatorKind.Implicit:
				codeBlocker.Write($"implicit operator {Type}");
				break;

			case OperatorKind.Explicit:
				codeBlocker.Write($"explicit operator {Type}");
				break;

			default:
				throw new InvalidOperationException($"Unknown operator kind '{Kind}'.");
		}

		TemplateRendering.WriteParameterList(codeBlocker, Parameters);
		TemplateRendering.WriteBody(codeBlocker, BodyFactory);
	}
}

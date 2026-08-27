// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

using Polyfills;

/// <summary>
/// How a property accessor is written.
/// </summary>
public enum AccessorKind
{
	/// <summary>An automatic accessor: <c>get;</c>.</summary>
	Auto,

	/// <summary>An expression-bodied accessor: <c>get =&gt; expression;</c>.</summary>
	Expression,

	/// <summary>A block-bodied accessor: <c>get</c> followed by a braced body.</summary>
	Block,
}

/// <summary>
/// Describes one accessor of a property.
/// </summary>
/// <remarks>
/// The accessor's shape is data — <see cref="Kind"/> — rather than something the model infers by
/// comparing callback instances, so a caller-supplied body is never mistaken for an automatic
/// accessor and an accessor can carry its own accessibility modifier.
/// </remarks>
public class AccessorTemplate
{
	/// <summary>Gets or sets how the accessor is written. Defaults to <see cref="AccessorKind.Auto"/>.</summary>
	public AccessorKind Kind { get; set; } = AccessorKind.Auto;

	/// <summary>
	/// Gets or sets the accessor's own accessibility modifier, for example <c>private</c>. Empty
	/// writes none, which is the usual case.
	/// </summary>
	public string Modifier { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the callback that writes the accessor body.
	/// </summary>
	/// <remarks>
	/// For <see cref="AccessorKind.Expression"/> the callback writes only the expression: the model
	/// supplies the <c>=&gt;</c> and the terminating semicolon. For
	/// <see cref="AccessorKind.Block"/> it writes the statements only: the model supplies the
	/// braces. It is ignored for <see cref="AccessorKind.Auto"/>.
	/// </remarks>
	public Action<CodeBlocker>? BodyFactory { get; set; }

	/// <summary>
	/// Creates an automatic accessor.
	/// </summary>
	/// <returns>A new <see cref="AccessorTemplate"/>.</returns>
	public static AccessorTemplate Auto() => new() { Kind = AccessorKind.Auto };

	/// <summary>
	/// Creates an expression-bodied accessor.
	/// </summary>
	/// <param name="expression">Writes the expression, without <c>=&gt;</c> or a semicolon.</param>
	/// <returns>A new <see cref="AccessorTemplate"/>.</returns>
	public static AccessorTemplate Expression(Action<CodeBlocker> expression) =>
		new() { Kind = AccessorKind.Expression, BodyFactory = expression };

	/// <summary>
	/// Creates a block-bodied accessor.
	/// </summary>
	/// <param name="body">Writes the statements, without the enclosing braces.</param>
	/// <returns>A new <see cref="AccessorTemplate"/>.</returns>
	public static AccessorTemplate Block(Action<CodeBlocker> body) =>
		new() { Kind = AccessorKind.Block, BodyFactory = body };

	/// <summary>
	/// Writes the accessor.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="keyword">The accessor keyword: <c>get</c>, <c>set</c> or <c>init</c>.</param>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	public void WriteTo(CodeBlocker codeBlocker, string keyword)
	{
		Ensure.NotNull(codeBlocker);

		string prefix = string.IsNullOrEmpty(Modifier) ? keyword : $"{Modifier} {keyword}";

		switch (Kind)
		{
			case AccessorKind.Auto:
				codeBlocker.WriteLine($"{prefix};");
				break;

			case AccessorKind.Expression:
				codeBlocker.Write(prefix);
				codeBlocker.Write(" => ");
				codeBlocker.Write(TemplateRendering.RenderFragment(codeBlocker, BodyFactory));
				codeBlocker.WriteLine(";");
				break;

			case AccessorKind.Block:
				codeBlocker.WriteLine(prefix);
				using (new Scope(codeBlocker))
				{
					TemplateRendering.SpliceFragment(codeBlocker, TemplateRendering.RenderFragment(codeBlocker, BodyFactory));
				}

				break;

			default:
				throw new InvalidOperationException($"Unknown accessor kind '{Kind}'.");
		}
	}

	/// <summary>
	/// Gets a value indicating whether this accessor can appear in the one-line
	/// <c>{ get; set; }</c> shorthand.
	/// </summary>
	internal bool IsShorthandEligible => Kind == AccessorKind.Auto && string.IsNullOrEmpty(Modifier);
}

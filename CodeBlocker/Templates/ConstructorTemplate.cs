// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

using Polyfills;
using System.Collections.ObjectModel;

/// <summary>
/// Describes a constructor declaration.
/// </summary>
public class ConstructorTemplate : MemberTemplate
{
	/// <summary>Gets the constructor's parameters, in declaration order.</summary>
	public Collection<ParameterTemplate> Parameters { get; } = [];

	/// <summary>
	/// Gets the arguments passed to the base constructor, written verbatim. Empty omits the
	/// <c>: base(...)</c> clause entirely.
	/// </summary>
	public Collection<string> BaseParameters { get; } = [];

	/// <summary>
	/// Gets or sets a value indicating whether the initialiser chains to <c>this</c> rather than
	/// <c>base</c>.
	/// </summary>
	public bool ChainsToThis { get; set; }

	/// <summary>
	/// Gets or sets the callback that writes the constructor body. Defaults to a callback that
	/// writes nothing, which renders as <c>{ }</c>; <see langword="null"/> terminates the
	/// declaration with a semicolon instead.
	/// </summary>
	public Action<CodeBlocker>? BodyFactory { get; set; } = _ => { };

	/// <summary>
	/// Writes the declaration, its parameter list, its constructor initialiser, and its body.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	public override void WriteTo(CodeBlocker codeBlocker)
	{
		Ensure.NotNull(codeBlocker);

		base.WriteTo(codeBlocker);
		TemplateRendering.WriteParameterList(codeBlocker, Parameters);
		WriteInitialiserTo(codeBlocker);
		TemplateRendering.WriteBody(codeBlocker, BodyFactory);
	}

	private void WriteInitialiserTo(CodeBlocker codeBlocker)
	{
		if (BaseParameters.Count == 0)
		{
			return;
		}

		// The initialiser goes on its own line, indented below the declaration it belongs to.
		codeBlocker.WriteLine();
		using IndentScope indent = new(codeBlocker);
		codeBlocker.Write($": {(ChainsToThis ? "this" : "base")}({string.Join(", ", BaseParameters)})");
	}
}

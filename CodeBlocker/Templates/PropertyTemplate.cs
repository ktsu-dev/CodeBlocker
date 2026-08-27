// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

using Polyfills;

/// <summary>
/// Describes a property declaration.
/// </summary>
public class PropertyTemplate : MemberTemplate
{
	/// <summary>Gets or sets the getter. <see langword="null"/> declares no getter.</summary>
	public AccessorTemplate? Getter { get; set; }

	/// <summary>Gets or sets the setter. <see langword="null"/> declares no setter.</summary>
	public AccessorTemplate? Setter { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the setter is written as <c>init</c> rather than
	/// <c>set</c>.
	/// </summary>
	public bool SetterIsInitOnly { get; set; }

	/// <summary>
	/// Gets or sets the callback that writes an expression body, producing
	/// <c>Type Name =&gt; expression;</c>. It writes only the expression: the model supplies the
	/// <c>=&gt;</c> and the semicolon.
	/// </summary>
	/// <remarks>
	/// An expression body replaces the accessor list, so <see cref="Getter"/> and
	/// <see cref="Setter"/> are ignored when this is set.
	/// </remarks>
	public Action<CodeBlocker>? ExpressionBodyFactory { get; set; }

	/// <summary>
	/// Writes the property.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <remarks>
	/// An expression body is written on the declaration line. Otherwise, accessors that are all
	/// automatic and unqualified collapse to <c>{ get; set; }</c> on one line, with any
	/// <see cref="TemplateBase.DefaultValue"/> as its initialiser; anything else gets a braced
	/// accessor list. A property with no accessors at all is declared and terminated with a
	/// semicolon, which is what an abstract or interface property looks like.
	/// </remarks>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	public override void WriteTo(CodeBlocker codeBlocker)
	{
		Ensure.NotNull(codeBlocker);

		base.WriteTo(codeBlocker);

		if (ExpressionBodyFactory is not null)
		{
			codeBlocker.Write(" => ");
			codeBlocker.Write(TemplateRendering.RenderFragment(codeBlocker, ExpressionBodyFactory));
			codeBlocker.WriteLine(";");
			return;
		}

		if (Getter is null && Setter is null)
		{
			// Writing "Type Name;" here would emit a field, not a property — which is what an
			// accessorless property used to render as. An abstract or interface property is
			// spelled with accessors that have no body: Getter = AccessorTemplate.Auto().
			throw new InvalidOperationException(
				$"Property '{Name}' declares no accessors and no expression body. Set Getter, " +
				"Setter, or ExpressionBodyFactory.");
		}

		if (CanUseShorthand)
		{
			WriteShorthand(codeBlocker);
			return;
		}

		WriteAccessorList(codeBlocker);
	}

	/// <summary>The keyword the setter is written with.</summary>
	private string SetterKeyword => SetterIsInitOnly ? "init" : "set";

	/// <summary>
	/// Gets a value indicating whether every declared accessor is automatic and unqualified, in
	/// which case the whole property fits on one line.
	/// </summary>
	private bool CanUseShorthand =>
		(Getter is null || Getter.IsShorthandEligible)
		&& (Setter is null || Setter.IsShorthandEligible);

	private void WriteShorthand(CodeBlocker codeBlocker)
	{
		codeBlocker.Write(" { ");
		if (Getter is not null)
		{
			codeBlocker.Write("get;");
		}

		if (Getter is not null && Setter is not null)
		{
			codeBlocker.Write(" ");
		}

		if (Setter is not null)
		{
			codeBlocker.Write($"{SetterKeyword};");
		}

		codeBlocker.Write(" }");
		WriteDefaultValueTo(codeBlocker);
		codeBlocker.WriteLine(DefaultValueIsSet ? ";" : string.Empty);
	}

	private bool DefaultValueIsSet => !string.IsNullOrEmpty(DefaultValue);

	private void WriteAccessorList(CodeBlocker codeBlocker)
	{
		codeBlocker.WriteLine();
		codeBlocker.WriteLine("{");
		codeBlocker.Indent();

		Getter?.WriteTo(codeBlocker, "get");
		Setter?.WriteTo(codeBlocker, SetterKeyword);

		codeBlocker.Outdent();

		// Written rather than WriteLine'd so an initialiser can follow the closing brace, which a
		// Scope would not allow.
		codeBlocker.Write("}");
		WriteDefaultValueTo(codeBlocker);
		codeBlocker.WriteLine(DefaultValueIsSet ? ";" : string.Empty);
	}
}

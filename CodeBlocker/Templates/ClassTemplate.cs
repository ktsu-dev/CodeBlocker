// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

using System.Collections.ObjectModel;

/// <summary>
/// Describes a type declaration and everything inside it.
/// </summary>
/// <remarks>
/// <see cref="Kind"/> supplies the declaration keyword, so
/// <see cref="TemplateBase.Keywords"/> carries only the modifiers, and
/// <see cref="TypeParameters"/> carries the generic parameters, so
/// <see cref="TemplateBase.Name"/> is the bare name.
/// </remarks>
public class ClassTemplate : TemplateBase
{
	/// <summary>Gets or sets the kind of type declared. Defaults to <see cref="TypeKind.Class"/>.</summary>
	public TypeKind Kind { get; set; } = TypeKind.Class;

	/// <summary>Gets the type's generic parameters, written as <c>&lt;T, TResult&gt;</c>.</summary>
	public Collection<string> TypeParameters { get; } = [];

	/// <summary>
	/// Gets the positional parameters of a record declaration, written as a parameter list
	/// immediately after the name. Empty declares no positional parameters.
	/// </summary>
	public Collection<ParameterTemplate> PositionalParameters { get; } = [];

	/// <summary>
	/// Gets or sets the base type, or for an enum its underlying type. Empty declares none.
	/// </summary>
	public string BaseClass { get; set; } = string.Empty;

	/// <summary>Gets the interfaces the type implements.</summary>
	public Collection<string> Interfaces { get; } = [];

	/// <summary>
	/// Gets the generic constraint clauses, each written verbatim on its own indented line — for
	/// example <c>where T : struct</c>.
	/// </summary>
	public Collection<string> Constraints { get; } = [];

	/// <summary>Gets the members declared in the type.</summary>
	public Collection<MemberTemplate> Members { get; } = [];

	/// <summary>Gets the types nested inside this one.</summary>
	public Collection<ClassTemplate> NestedClasses { get; } = [];

	/// <summary>
	/// Gets or sets a value indicating whether members are grouped by kind — see
	/// <see cref="MemberTemplate.MemberSortOrder"/> — before being written. Defaults to
	/// <see langword="true"/>; clear it to write them in the order they were added.
	/// </summary>
	public bool SortMembers { get; set; } = true;

	/// <summary>
	/// Writes the declaration, its base type and interfaces, its constraints, and its body.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	public override void WriteTo(CodeBlocker codeBlocker)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);

		base.WriteTo(codeBlocker);

		codeBlocker.Write($"{TypeKindKeywords.For(Kind)} {Name}");
		TemplateRendering.WriteTypeParameterList(codeBlocker, TypeParameters);

		if (PositionalParameters.Count > 0)
		{
			TemplateRendering.WriteParameterList(codeBlocker, PositionalParameters);
		}

		WriteBaseClassAndInterfacesTo(codeBlocker);
		TemplateRendering.WriteConstraints(codeBlocker, Constraints);

		// A positional record with nothing in it needs no body at all.
		if (IsBodylessPositionalRecord)
		{
			codeBlocker.WriteLine(";");
			return;
		}

		// Terminate the declaration line, whether it ended with the name, the base list, or the
		// last constraint clause.
		codeBlocker.WriteLine();
		WriteBodyTo(codeBlocker);
	}

	/// <summary>
	/// Gets a value indicating whether this is a record whose positional parameters are its whole
	/// definition, which is declared with a semicolon instead of an empty body.
	/// </summary>
	private bool IsBodylessPositionalRecord =>
		PositionalParameters.Count > 0
		&& Members.Count == 0
		&& NestedClasses.Count == 0
		&& Kind is TypeKind.Record or TypeKind.RecordStruct;

	private void WriteBaseClassAndInterfacesTo(CodeBlocker codeBlocker)
	{
		List<string> baseAndInterfaces = [];
		if (!string.IsNullOrEmpty(BaseClass))
		{
			baseAndInterfaces.Add(BaseClass);
		}

		baseAndInterfaces.AddRange(Interfaces);

		if (baseAndInterfaces.Count == 0)
		{
			return;
		}

		codeBlocker.Write($" : {string.Join(", ", baseAndInterfaces)}");
	}

	private void WriteBodyTo(CodeBlocker codeBlocker)
	{
		using Scope scope = new(codeBlocker);

		IEnumerable<MemberTemplate> members = SortMembers
			? Members.OrderBy(MemberTemplate.MemberSortOrder)
			: Members;

		// Enum members read as a list, so they are not spaced apart the way declarations are.
		bool separateMembers = Kind != TypeKind.Enum;

		bool first = true;
		foreach (MemberTemplate member in members)
		{
			if (!first && separateMembers)
			{
				codeBlocker.NewLine();
			}

			member.WriteTo(codeBlocker);
			first = false;
		}

		foreach (ClassTemplate nestedClass in NestedClasses)
		{
			if (!first)
			{
				codeBlocker.NewLine();
			}

			nestedClass.WriteTo(codeBlocker);
			first = false;
		}
	}
}

/// <summary>
/// Extension methods for writing type declarations.
/// </summary>
public static class ClassTemplateExtensions
{
	/// <summary>
	/// Writes a type declaration.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="classTemplate">The type to write.</param>
	/// <returns>The same <see cref="CodeBlocker"/>, for chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="codeBlocker"/> or <paramref name="classTemplate"/> is <see langword="null"/>.
	/// </exception>
	public static CodeBlocker AddClass(this CodeBlocker codeBlocker, ClassTemplate classTemplate)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);
		ArgumentNullException.ThrowIfNull(classTemplate);

		classTemplate.WriteTo(codeBlocker);
		return codeBlocker;
	}
}

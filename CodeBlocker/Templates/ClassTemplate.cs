// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

using Polyfills;
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
	/// <exception cref="ArgumentOutOfRangeException">
	/// <see cref="Kind"/>, or the kind of a nested type, is not a known kind. Nothing is written.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// A member, or a member of a nested type, would be refused when written, or
	/// <see cref="Members"/>, <see cref="NestedClasses"/> or <see cref="PositionalParameters"/> holds a
	/// <see langword="null"/> entry. Nothing is written.
	/// </exception>
	public override void WriteTo(CodeBlocker codeBlocker)
	{
		Ensure.NotNull(codeBlocker);

		using CodeBlocker.TemplateWriteScope scope = codeBlocker.BeginTemplateWrite();

		// Everything is checked before the doc comment, attributes and modifiers are written, so a
		// failing check cannot leave an unfinished declaration behind, nor a closed body that is
		// silently missing the failing member and everything after it. A type already checked by an
		// enclosing type or by AddClasses/AddSourceFile is not checked again, so an expression body's
		// callback runs once to check it and once to write it, however deeply its type is nested.
		if (!codeBlocker.IsChecked(this))
		{
			Validate(codeBlocker);
		}

		string keyword = TypeKindKeywords.For(Kind);
		base.WriteTo(codeBlocker);

		codeBlocker.Write($"{keyword} {Name}");
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

	/// <inheritdoc/>
	internal override void Validate(CodeBlocker codeBlocker)
	{
		base.Validate(codeBlocker);
		_ = TypeKindKeywords.For(Kind);

		// Null entries are refused here rather than found while writing: a null member or nested
		// type would throw a bare NullReferenceException, and a null positional parameter would
		// throw after "record R" is already written.
		string owner = $"Type '{Name}'";
		TemplateRendering.EnsureNoNullEntries(Members, nameof(Members), owner);
		TemplateRendering.EnsureNoNullEntries(NestedClasses, nameof(NestedClasses), owner);
		TemplateRendering.EnsureNoNullEntries(PositionalParameters, nameof(PositionalParameters), owner);

		foreach (MemberTemplate member in Members)
		{
			member.Validate(codeBlocker);
		}

		foreach (ClassTemplate nestedClass in NestedClasses.Where(nested => !codeBlocker.IsChecked(nested)))
		{
			nestedClass.Validate(codeBlocker);
		}

		codeBlocker.MarkChecked(this);
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
		// Blank entries are skipped, so an all-blank list writes no " : " at all.
		List<string> baseAndInterfaces = TemplateRendering.NonBlank([BaseClass, .. Interfaces]);

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
		Ensure.NotNull(codeBlocker);
		Ensure.NotNull(classTemplate);

		classTemplate.WriteTo(codeBlocker);
		return codeBlocker;
	}
}

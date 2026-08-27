// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

/// <summary>
/// The kind of type a <see cref="ClassTemplate"/> declares.
/// </summary>
/// <remarks>
/// The kind supplies the declaration keyword, so it does not also belong in
/// <see cref="TemplateBase.Keywords"/> — those carry only the modifiers.
/// </remarks>
public enum TypeKind
{
	/// <summary>A <c>class</c> declaration.</summary>
	Class,

	/// <summary>A <c>struct</c> declaration.</summary>
	Struct,

	/// <summary>An <c>interface</c> declaration.</summary>
	Interface,

	/// <summary>A <c>record</c> declaration.</summary>
	Record,

	/// <summary>A <c>record struct</c> declaration.</summary>
	RecordStruct,

	/// <summary>An <c>enum</c> declaration.</summary>
	Enum,
}

/// <summary>
/// The declaration keywords for each <see cref="TypeKind"/>.
/// </summary>
internal static class TypeKindKeywords
{
	/// <summary>
	/// Gets the declaration keyword for a type kind.
	/// </summary>
	/// <param name="kind">The kind to translate.</param>
	/// <returns>The C# keyword or keyword pair that declares that kind.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a known kind.</exception>
	internal static string For(TypeKind kind) => kind switch
	{
		TypeKind.Class => "class",
		TypeKind.Struct => "struct",
		TypeKind.Interface => "interface",
		TypeKind.Record => "record",
		TypeKind.RecordStruct => "record struct",
		TypeKind.Enum => "enum",
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown type kind."),
	};
}

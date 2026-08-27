// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

using System.Collections.ObjectModel;

/// <summary>
/// Describes a whole source file: its namespace, its using directives, and the types it declares.
/// </summary>
public class SourceFileTemplate : TemplateBase
{
	/// <summary>
	/// Gets or sets the file name. The model does not write this anywhere; it is carried so a
	/// generator can hand it to whatever writes the file out.
	/// </summary>
	public string FileName { get; set; } = string.Empty;

	/// <summary>Gets or sets the namespace. Empty writes no namespace declaration.</summary>
	public string Namespace { get; set; } = string.Empty;

	/// <summary>
	/// Gets the namespaces to import, without the <c>using</c> keyword or trailing semicolon.
	/// </summary>
	public Collection<string> Usings { get; } = [];

	/// <summary>Gets the types declared in the file.</summary>
	public Collection<ClassTemplate> Classes { get; } = [];
}

/// <summary>
/// Extension methods for writing whole source files.
/// </summary>
public static class SourceFileTemplateExtensions
{
	/// <summary>
	/// Writes a source file: its shared parts, its namespace, its using directives, and its types.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="template">The file to write.</param>
	/// <returns>The same <see cref="CodeBlocker"/>, for chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="codeBlocker"/> or <paramref name="template"/> is <see langword="null"/>.
	/// </exception>
	public static CodeBlocker AddSourceFile(this CodeBlocker codeBlocker, SourceFileTemplate template)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);
		ArgumentNullException.ThrowIfNull(template);

		codeBlocker.AddTemplate(template);
		codeBlocker.WriteFileScopedNamespace(template.Namespace);
		codeBlocker.WriteUsings(template.Usings);
		codeBlocker.AddClasses(template.Classes);
		return codeBlocker;
	}

	/// <summary>
	/// Writes each type followed by a blank line.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="classes">The types to write.</param>
	/// <returns>The same <see cref="CodeBlocker"/>, for chaining.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="codeBlocker"/> or <paramref name="classes"/> is <see langword="null"/>.
	/// </exception>
	public static CodeBlocker AddClasses(this CodeBlocker codeBlocker, IEnumerable<ClassTemplate> classes)
	{
		ArgumentNullException.ThrowIfNull(codeBlocker);
		ArgumentNullException.ThrowIfNull(classes);

		foreach (ClassTemplate classTemplate in classes)
		{
			codeBlocker.AddClass(classTemplate);
			codeBlocker.NewLine();
		}

		return codeBlocker;
	}
}

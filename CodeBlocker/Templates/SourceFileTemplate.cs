// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

using Polyfills;
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
	/// <remarks>
	/// The only attributes a file can carry are global ones (<c>assembly:</c>, <c>module:</c>), and
	/// C# only accepts them after every using directive and before the namespace. So when the file
	/// has attributes, its usings come first, then the attributes, then the namespace; that is the one
	/// order in which an attribute can be named through the file's own usings. A file with no
	/// attributes keeps the usings inside its file-scoped namespace.
	/// </remarks>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="codeBlocker"/> or <paramref name="template"/> is <see langword="null"/>.
	/// </exception>
	public static CodeBlocker AddSourceFile(this CodeBlocker codeBlocker, SourceFileTemplate template)
	{
		Ensure.NotNull(codeBlocker);
		Ensure.NotNull(template);

		codeBlocker.AddDocumentation(template.Documentation);
		codeBlocker.AddComments(template.Comments);
		if (template.Attributes.Count > 0)
		{
			codeBlocker.WriteUsings(template.Usings);
			codeBlocker.AddAttributes(template.Attributes);
			codeBlocker.NewLine();
			codeBlocker.AddKeywords(template.Keywords);
			codeBlocker.WriteFileScopedNamespace(template.Namespace);
		}
		else
		{
			codeBlocker.AddKeywords(template.Keywords);
			codeBlocker.WriteFileScopedNamespace(template.Namespace);
			codeBlocker.WriteUsings(template.Usings);
		}

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
		Ensure.NotNull(codeBlocker);
		Ensure.NotNull(classes);

		foreach (ClassTemplate classTemplate in classes)
		{
			codeBlocker.AddClass(classTemplate);
			codeBlocker.NewLine();
		}

		return codeBlocker;
	}
}

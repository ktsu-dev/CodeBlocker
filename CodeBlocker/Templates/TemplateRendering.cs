// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

/// <summary>
/// The rendering steps shared by more than one template.
/// </summary>
/// <remarks>
/// Methods, constructors and operators differ only in their signature, so their parameter list and
/// body are written from here rather than implemented once per member kind.
/// </remarks>
internal static class TemplateRendering
{
	/// <summary>
	/// Renders a fragment — a body, an expression, a parameter — into its own buffer.
	/// </summary>
	/// <param name="parent">The <see cref="CodeBlocker"/> the fragment will be spliced into.</param>
	/// <param name="factory">The callback that writes the fragment, or <see langword="null"/>.</param>
	/// <returns>The rendered fragment, empty when <paramref name="factory"/> is <see langword="null"/>.</returns>
	/// <remarks>
	/// The buffer is configured exactly like <paramref name="parent"/>, so the fragment's own
	/// indentation and line terminators match the document it is going into. In particular the
	/// terminator is the configured one and never the host's, which is what lets a body assembled on
	/// one platform lay out identically on another.
	/// </remarks>
	internal static string RenderFragment(CodeBlocker parent, Action<CodeBlocker>? factory)
	{
		if (factory is null)
		{
			return string.Empty;
		}

		using CodeBlocker fragmentWriter = CodeBlocker.Create(parent.IndentString, parent.NewLineString);
		factory(fragmentWriter);
		return fragmentWriter.ToString();
	}

	/// <summary>
	/// Splits a rendered fragment into its lines, discarding the empty tail a trailing terminator
	/// leaves behind.
	/// </summary>
	/// <param name="parent">The <see cref="CodeBlocker"/> the fragment was rendered for.</param>
	/// <param name="fragment">The rendered fragment.</param>
	/// <returns>The fragment's lines.</returns>
	internal static string[] SplitLines(CodeBlocker parent, string fragment)
	{
		string[] lines = fragment.Split([parent.NewLineString], StringSplitOptions.None);
		if (lines.Length == 0 || lines[^1].Length != 0)
		{
			return lines;
		}

		// Array range indexing would need RuntimeHelpers.GetSubArray, which netstandard2.0 lacks.
		string[] trimmed = new string[lines.Length - 1];
		Array.Copy(lines, trimmed, trimmed.Length);
		return trimmed;
	}

	/// <summary>
	/// Writes a rendered fragment through <paramref name="parent"/> one line at a time, so every
	/// line picks up the indentation of the position it is being spliced into.
	/// </summary>
	/// <param name="parent">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="fragment">The rendered fragment.</param>
	/// <remarks>
	/// Writing the fragment as one string instead would splice it verbatim: only its first line
	/// would land at the current indent and every following line would keep the indentation it had
	/// in its own buffer, which is why a nested body used to come out flush against the left margin.
	/// </remarks>
	internal static void SpliceFragment(CodeBlocker parent, string fragment)
	{
		foreach (string line in SplitLines(parent, fragment))
		{
			if (line.Length == 0)
			{
				parent.NewLine();
			}
			else
			{
				parent.WriteLine(line);
			}
		}
	}

	/// <summary>
	/// Writes a parenthesised, comma-separated parameter list.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="parameters">The parameters, in declaration order.</param>
	internal static void WriteParameterList(CodeBlocker codeBlocker, IEnumerable<ParameterTemplate> parameters)
	{
		List<string> parameterStrings = [];
		foreach (ParameterTemplate parameterTemplate in parameters)
		{
			parameterStrings.Add(RenderFragment(codeBlocker, parameterTemplate.WriteTo));
		}

		codeBlocker.Write("(");
		codeBlocker.Write(string.Join(", ", parameterStrings));
		codeBlocker.Write(")");
	}

	/// <summary>
	/// Writes the angle-bracketed type parameter list, or nothing when there are none.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="typeParameters">The type parameter names.</param>
	internal static void WriteTypeParameterList(CodeBlocker codeBlocker, IReadOnlyCollection<string> typeParameters)
	{
		if (typeParameters.Count == 0)
		{
			return;
		}

		codeBlocker.Write($"<{string.Join(", ", typeParameters)}>");
	}

	/// <summary>
	/// Writes each generic constraint clause on its own line, indented one level below the
	/// declaration it constrains.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="constraints">The constraint clauses, each written verbatim.</param>
	/// <remarks>
	/// The last clause is left unterminated so that whatever follows still sees an open declaration
	/// line: a member with no body attaches its semicolon to the clause
	/// (<c>where T : struct;</c>), and an expression body attaches its arrow.
	/// </remarks>
	internal static void WriteConstraints(CodeBlocker codeBlocker, IReadOnlyCollection<string> constraints)
	{
		if (constraints.Count == 0)
		{
			return;
		}

		// Terminate the declaration line the constraints hang off. WriteLine() rather than
		// NewLine(): NewLine() writes through IndentedTextWriter.WriteLineNoTabs, which does not
		// re-arm the writer's pending-tab flag, so whatever came next would land at column zero.
		codeBlocker.WriteLine();

		using IndentScope indent = new(codeBlocker);
		int remaining = constraints.Count;
		foreach (string constraint in constraints)
		{
			remaining--;
			if (remaining == 0)
			{
				codeBlocker.Write(constraint);
			}
			else
			{
				codeBlocker.WriteLine(constraint);
			}
		}
	}

	/// <summary>
	/// Writes a member body, terminating the declaration line.
	/// </summary>
	/// <param name="codeBlocker">The <see cref="CodeBlocker"/> to write to.</param>
	/// <param name="bodyFactory">
	/// The callback that writes the body, supplying its own braces or expression-body arrow.
	/// <see langword="null"/> writes a terminating semicolon instead, for a declaration with no body
	/// — an abstract, partial or interface member.
	/// </param>
	internal static void WriteBody(CodeBlocker codeBlocker, Action<CodeBlocker>? bodyFactory)
	{
		if (bodyFactory is null)
		{
			codeBlocker.WriteLine(";");
			return;
		}

		string body = RenderFragment(codeBlocker, bodyFactory);
		string[] lines = SplitLines(codeBlocker, body);

		// A factory that wrote nothing means "declared, but empty" — a virtual base method, or a
		// constructor that only forwards to its base.
		if (lines.Length == 0)
		{
			codeBlocker.WriteLine(" { }");
			return;
		}

		if (lines.Length == 1)
		{
			// An expression body stays on the declaration line.
			codeBlocker.Write(" ");
			codeBlocker.WriteLine(lines[0]);
			return;
		}

		// A braced body starts on the line after the declaration.
		codeBlocker.WriteLine();
		SpliceFragment(codeBlocker, body);
	}
}

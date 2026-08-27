// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker;

using Polyfills;
using ktsu.ScopedAction;

/// <summary>
/// Base class for a scope that writes an opening delimiter, indents the body, and writes a closing
/// delimiter when disposed.
/// </summary>
/// <remarks>
/// Both delimiters are written on their own line, so a scope always reads as a block:
/// <code>
/// (
///     arg
/// )
/// </code>
/// </remarks>
/// <param name="codeBlocker">The parent <see cref="CodeBlocker"/>.</param>
/// <param name="open">The delimiter written before the body.</param>
/// <param name="close">The delimiter written after the body.</param>
public class DelimiterScope(CodeBlocker codeBlocker, string open, string close)
	: ScopedAction(onOpen: () => Begin(codeBlocker, open), onClose: () => End(codeBlocker, close))
{
	/// <summary>
	/// Writes the opening delimiter and increases the indent level.
	/// </summary>
	/// <param name="codeBlocker">The parent <see cref="CodeBlocker"/>.</param>
	/// <param name="open">The delimiter to write.</param>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	protected static void Begin(CodeBlocker codeBlocker, string open)
	{
		Ensure.NotNull(codeBlocker);

		codeBlocker.WriteLine(open);
		codeBlocker.Indent();
	}

	/// <summary>
	/// Decreases the indent level and writes the closing delimiter.
	/// </summary>
	/// <param name="codeBlocker">The parent <see cref="CodeBlocker"/>.</param>
	/// <param name="close">The delimiter to write.</param>
	/// <exception cref="ArgumentNullException"><paramref name="codeBlocker"/> is <see langword="null"/>.</exception>
	protected static void End(CodeBlocker codeBlocker, string close)
	{
		Ensure.NotNull(codeBlocker);

		codeBlocker.Outdent();
		codeBlocker.WriteLine(close);
	}
}

/// <summary>
/// Class to create a parenthesised scope in a code block, for argument lists long enough to break
/// across lines.
/// </summary>
/// <remarks>
/// Create a new instance of <see cref="ParenScope"/>.
/// </remarks>
/// <param name="codeBlocker">The parent <see cref="CodeBlocker"/>.</param>
public class ParenScope(CodeBlocker codeBlocker) : DelimiterScope(codeBlocker, "(", ")");

/// <summary>
/// Class to create a bracketed scope in a code block, for collection expressions and array
/// initialisers long enough to break across lines.
/// </summary>
/// <remarks>
/// Create a new instance of <see cref="BracketScope"/>.
/// </remarks>
/// <param name="codeBlocker">The parent <see cref="CodeBlocker"/>.</param>
public class BracketScope(CodeBlocker codeBlocker) : DelimiterScope(codeBlocker, "[", "]");

/// <summary>
/// Class to indent a run of lines without writing any delimiters, for continuation lines such as
/// generic constraints or a chained call.
/// </summary>
/// <remarks>
/// Create a new instance of <see cref="IndentScope"/>.
/// </remarks>
/// <param name="codeBlocker">The parent <see cref="CodeBlocker"/>.</param>
public class IndentScope(CodeBlocker codeBlocker)
	: ScopedAction(onOpen: () => Begin(codeBlocker), onClose: () => End(codeBlocker))
{
	private static void Begin(CodeBlocker codeBlocker)
	{
		Ensure.NotNull(codeBlocker);
		codeBlocker.Indent();
	}

	private static void End(CodeBlocker codeBlocker)
	{
		Ensure.NotNull(codeBlocker);
		codeBlocker.Outdent();
	}
}

/// <summary>
/// Class to wrap a run of lines in <c>#region</c> and <c>#endregion</c>.
/// </summary>
/// <remarks>
/// Create a new instance of <see cref="RegionScope"/>. The directives are written at the current
/// indent level, and the body is not indented further — a region does not nest code.
/// </remarks>
/// <param name="codeBlocker">The parent <see cref="CodeBlocker"/>.</param>
/// <param name="name">The region name.</param>
public class RegionScope(CodeBlocker codeBlocker, string name)
	: ScopedAction(onOpen: () => Begin(codeBlocker, name), onClose: () => End(codeBlocker))
{
	private static void Begin(CodeBlocker codeBlocker, string name)
	{
		Ensure.NotNull(codeBlocker);
		codeBlocker.WriteLine(string.IsNullOrEmpty(name) ? "#region" : $"#region {name}");
	}

	private static void End(CodeBlocker codeBlocker)
	{
		Ensure.NotNull(codeBlocker);
		codeBlocker.WriteLine("#endregion");
	}
}

/// <summary>
/// Class to wrap a run of lines in <c>#if</c> and <c>#endif</c>.
/// </summary>
/// <remarks>
/// Create a new instance of <see cref="DirectiveScope"/>. The directives are written at the current
/// indent level, and the body is not indented further — a conditional compilation directive does not
/// nest code.
/// </remarks>
/// <param name="codeBlocker">The parent <see cref="CodeBlocker"/>.</param>
/// <param name="condition">The condition expression, for example <c>NET8_0_OR_GREATER</c>.</param>
public class DirectiveScope(CodeBlocker codeBlocker, string condition)
	: ScopedAction(onOpen: () => Begin(codeBlocker, condition), onClose: () => End(codeBlocker))
{
	private static void Begin(CodeBlocker codeBlocker, string condition)
	{
		Ensure.NotNull(codeBlocker);
		codeBlocker.WriteLine($"#if {condition}");
	}

	private static void End(CodeBlocker codeBlocker)
	{
		Ensure.NotNull(codeBlocker);
		codeBlocker.WriteLine("#endif");
	}
}

/// <summary>
/// Class to wrap a run of lines in <c>#pragma warning disable</c> and
/// <c>#pragma warning restore</c> for the same warnings.
/// </summary>
/// <remarks>
/// An unbalanced suppression leaks into the rest of the file and is tedious to trace back, so
/// pairing the two directives in a scope is worth more here than the line saving. The directives are
/// written at the current indent level, and the body is not indented further.
/// </remarks>
/// <param name="codeBlocker">The parent <see cref="CodeBlocker"/>.</param>
/// <param name="warnings">
/// The warning identifiers to suppress, written verbatim after the directive — either a single
/// identifier such as <c>CS1591</c> or a comma-separated list.
/// </param>
public class PragmaScope(CodeBlocker codeBlocker, string warnings)
	: ScopedAction(onOpen: () => Begin(codeBlocker, warnings), onClose: () => End(codeBlocker, warnings))
{
	/// <summary>
	/// Create a new instance of <see cref="PragmaScope"/> suppressing several warnings.
	/// </summary>
	/// <param name="codeBlocker">The parent <see cref="CodeBlocker"/>.</param>
	/// <param name="warnings">The warning identifiers to suppress.</param>
	public PragmaScope(CodeBlocker codeBlocker, IEnumerable<string> warnings)
		: this(codeBlocker, string.Join(", ", warnings ?? []))
	{
	}

	private static void Begin(CodeBlocker codeBlocker, string warnings)
	{
		Ensure.NotNull(codeBlocker);
		codeBlocker.WriteLine($"#pragma warning disable {warnings}");
	}

	private static void End(CodeBlocker codeBlocker, string warnings)
	{
		Ensure.NotNull(codeBlocker);
		codeBlocker.WriteLine($"#pragma warning restore {warnings}");
	}
}

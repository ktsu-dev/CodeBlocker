// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker;

/// <summary>
/// The characters the C# compiler ends a line at, for text that has to stay inside a single-line
/// comment or directive.
/// </summary>
internal static class LineTerminators
{
	/// <summary>
	/// Every line terminator the C# compiler recognises: LF, CR, NEL (U+0085), LINE SEPARATOR
	/// (U+2028) and PARAGRAPH SEPARATOR (U+2029). CRLF is a pair of these.
	/// </summary>
	private static readonly char[] Characters = ['\n', '\r', '\u0085', '\u2028', '\u2029'];

	/// <summary>
	/// Splits text into lines at every terminator the C# compiler recognises: either platform's
	/// line ending, so that a caller's verbatim or raw string literal lays out the same way
	/// whichever platform the source file was written on, and also NEL (U+0085), LINE SEPARATOR
	/// (U+2028) and PARAGRAPH SEPARATOR (U+2029). The compiler ends a <c>//</c> or <c>///</c>
	/// comment at any of those, so left inside a line they would push the rest of the text out of
	/// the comment and into the code.
	/// </summary>
	/// <param name="text">The text to split.</param>
	/// <returns>The lines, without their terminators. Text with no terminator is a single line.</returns>
	internal static string[] Split(string text) =>
		text.Replace("\r\n", "\n").Split(Characters);

	/// <summary>
	/// Reports whether text contains any line terminator the C# compiler recognises.
	/// </summary>
	/// <param name="text">The text to check.</param>
	/// <returns><see langword="true"/> if <paramref name="text"/> would span more than one line.</returns>
	internal static bool Contains(string text) => text.IndexOfAny(Characters) >= 0;
}

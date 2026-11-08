// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker.Templates;

/// <summary>
/// Finds the text of verbatim string literals in a fragment of C#, so that a line break inside one
/// is not mistaken for a line break in the code around it.
/// </summary>
/// <remarks>
/// This is a lexer only as far as it needs to be to find where literals start and end: comments,
/// character literals, regular and raw strings, and the interpolation holes of interpolated strings
/// are walked so that a quote inside any of them is not read as the start of a verbatim string.
/// Text it cannot make sense of is treated as ordinary code, so a malformed fragment is split as it
/// always was.
/// </remarks>
internal static class VerbatimStringScanner
{
	/// <summary>
	/// Marks each character of <paramref name="text"/> that lies inside the body of a verbatim
	/// string literal.
	/// </summary>
	/// <param name="text">The C# fragment to scan.</param>
	/// <returns>
	/// One flag per character of <paramref name="text"/>, true where that character is part of a
	/// verbatim literal's value. Interpolation holes inside the literal are code, not value, and are
	/// not marked.
	/// </returns>
	internal static bool[] FindVerbatimText(string text)
	{
		bool[] inVerbatimText = new bool[text.Length];
		int position = 0;
		ScanCode(text, ref position, inVerbatimText, lineCommentStarts: null);
		return inVerbatimText;
	}

	/// <summary>
	/// Finds the line comments in <paramref name="text"/> that belong to the code itself.
	/// </summary>
	/// <param name="text">The C# fragment to scan.</param>
	/// <returns>
	/// The position of the first <c>/</c> of each such <c>//</c> comment, in order. A <c>//</c>
	/// inside a string or character literal, a block comment or an interpolation hole is not one.
	/// </returns>
	internal static List<int> FindLineComments(string text)
	{
		List<int> lineCommentStarts = [];
		int position = 0;
		ScanCode(text, ref position, new bool[text.Length], lineCommentStarts);
		return lineCommentStarts;
	}

	/// <summary>
	/// Walks code up to the end of <paramref name="text"/> or, inside an interpolation hole, up to
	/// the brace that closes the hole, which is left for the caller to consume.
	/// </summary>
	/// <param name="text">The C# fragment to scan.</param>
	/// <param name="position">Where to start, and on return where the walk stopped.</param>
	/// <param name="inVerbatimText">Receives the verbatim literal flags.</param>
	/// <param name="lineCommentStarts">
	/// Receives the start of each line comment, at the top level only; <see langword="null"/> when
	/// they are not wanted, and for an interpolation hole.
	/// </param>
	/// <param name="isInterpolationHole">Whether the walk is inside an interpolation hole.</param>
	private static void ScanCode(string text, ref int position, bool[] inVerbatimText, List<int>? lineCommentStarts, bool isInterpolationHole = false)
	{
		int braceDepth = 0;
		int bracketDepth = 0;
		bool atLineStart = !isInterpolationHole;
		while (position < text.Length)
		{
			if (TrySkipNonCode(text, ref position, inVerbatimText, lineCommentStarts, ref atLineStart, isInterpolationHole))
			{
				continue;
			}

			if (isInterpolationHole)
			{
				char c = text[position];
				if (IsClosingBrace(c, ref braceDepth))
				{
					return;
				}

				if (braceDepth == 0 && IsFormatSpecifierColon(text, position, ref bracketDepth))
				{
					SkipFormatSpecifier(text, ref position);
					return;
				}
			}

			position++;
		}
	}

	/// <summary>
	/// Moves past the preprocessor line, comment or literal that starts at
	/// <paramref name="position"/>, if one does, recording a line comment's start on the way.
	/// </summary>
	/// <returns>True when something was skipped.</returns>
	private static bool TrySkipNonCode(string text, ref int position, bool[] inVerbatimText, List<int>? lineCommentStarts, ref bool atLineStart, bool isInterpolationHole)
	{
		if (TrySkipPreprocessorLine(text, ref position, ref atLineStart, isInterpolationHole))
		{
			return true;
		}

		if (lineCommentStarts is not null && text[position] == '/' && At(text, position + 1) == '/')
		{
			lineCommentStarts.Add(position);
		}

		if (!TrySkipCommentOrLiteral(text, ref position, inVerbatimText))
		{
			return false;
		}

		// A literal can span lines, but the code after it does not start a line.
		atLineStart = false;
		return true;
	}

	/// <summary>
	/// Moves past the rest of a preprocessor line when <paramref name="position"/> is at its
	/// <c>#</c>, and otherwise tracks whether the walk is still at the start of a line.
	/// </summary>
	/// <remarks>
	/// The compiler does not tokenize the rest of a preprocessor line: the text after
	/// <c>#region</c>, <c>#error</c> or <c>#warning</c> is free text, so an <c>@"</c> in it opens
	/// nothing (ktsu-dev/CodeBlocker#194). The line break is left to the code around it. Only a
	/// <c>#</c> preceded on its line by nothing but whitespace starts a directive, and never inside
	/// an interpolation hole.
	/// </remarks>
	/// <param name="text">The C# fragment to scan.</param>
	/// <param name="position">The current position, moved to the line break when a directive is skipped.</param>
	/// <param name="atLineStart">Whether only whitespace has been seen since the last line break.</param>
	/// <param name="isInterpolationHole">Whether the walk is inside an interpolation hole.</param>
	/// <returns>True when a directive line was skipped.</returns>
	private static bool TrySkipPreprocessorLine(string text, ref int position, ref bool atLineStart, bool isInterpolationHole)
	{
		char c = text[position];
		if (IsLineBreak(c))
		{
			atLineStart = !isInterpolationHole;
			return false;
		}

		if (!atLineStart)
		{
			return false;
		}

		if (c == '#')
		{
			SkipLineComment(text, ref position);
			return true;
		}

		atLineStart = char.IsWhiteSpace(c);
		return false;
	}

	/// <summary>
	/// Tracks parenthesis and bracket nesting inside an interpolation hole, and reports whether the
	/// character at <paramref name="position"/> is the colon that starts the hole's format specifier.
	/// </summary>
	/// <remarks>
	/// Only a colon outside every nested parenthesis, bracket and brace starts one: a conditional
	/// expression or a named argument is always nested. The <c>::</c> of an alias qualifier is not
	/// one either.
	/// </remarks>
	private static bool IsFormatSpecifierColon(string text, int position, ref int bracketDepth)
	{
		char c = text[position];
		if (c is '(' or '[')
		{
			bracketDepth++;
		}
		else if (c is ')' or ']')
		{
			bracketDepth = Math.Max(bracketDepth - 1, 0);
		}

		return c == ':'
			&& bracketDepth == 0
			&& At(text, position + 1) != ':'
			&& (position == 0 || text[position - 1] != ':');
	}

	/// <summary>
	/// Moves from a format specifier's colon up to the brace that closes the interpolation hole,
	/// which is left for the caller to consume. A format specifier is literal text: a <c>//</c>,
	/// quote or apostrophe in it starts nothing.
	/// </summary>
	private static void SkipFormatSpecifier(string text, ref int position)
	{
		while (position < text.Length && text[position] != '}' && !IsLineBreak(text[position]))
		{
			position++;
		}
	}

	/// <summary>
	/// Moves past the comment or literal that starts at <paramref name="position"/>, if one does.
	/// </summary>
	private static bool TrySkipCommentOrLiteral(string text, ref int position, bool[] inVerbatimText)
	{
		char c = text[position];
		if (c == '/' && At(text, position + 1) == '/')
		{
			SkipLineComment(text, ref position);
			return true;
		}

		if (c == '/' && At(text, position + 1) == '*')
		{
			int end = text.IndexOf("*/", position + 2, StringComparison.Ordinal);
			position = end < 0 ? text.Length : end + 2;
			return true;
		}

		if (c == '\'')
		{
			SkipCharLiteral(text, ref position);
			return true;
		}

		return c is '"' or '$' or '@' && TryScanString(text, ref position, inVerbatimText);
	}

	/// <summary>
	/// Tracks brace nesting inside an interpolation hole.
	/// </summary>
	/// <returns>True when <paramref name="c"/> is the brace that closes the hole.</returns>
	private static bool IsClosingBrace(char c, ref int braceDepth)
	{
		if (c == '{')
		{
			braceDepth++;
		}
		else if (c == '}')
		{
			if (braceDepth == 0)
			{
				return true;
			}

			braceDepth--;
		}

		return false;
	}

	private static char At(string text, int position) => position < text.Length ? text[position] : '\0';

	private static bool IsLineBreak(char c) => c is '\n' or '\r' or '\u0085' or '\u2028' or '\u2029';

	private static void SkipLineComment(string text, ref int position)
	{
		while (position < text.Length && !IsLineBreak(text[position]))
		{
			position++;
		}
	}

	private static void SkipCharLiteral(string text, ref int position)
	{
		position++;
		while (position < text.Length)
		{
			char c = text[position];
			if (c == '\\')
			{
				position += 2;
			}
			else if (c == '\'')
			{
				position++;
				return;
			}
			else if (IsLineBreak(c))
			{
				// Unterminated: leave the line break to the code around it.
				return;
			}
			else
			{
				position++;
			}
		}
	}

	/// <summary>
	/// Moves past the string literal that starts at <paramref name="position"/>, if one does.
	/// </summary>
	/// <returns>
	/// False, without moving, when the <c>$</c>, <c>@</c> or quote there does not start a string
	/// literal — an <c>@</c> before an identifier, for one.
	/// </returns>
	private static bool TryScanString(string text, ref int position, bool[] inVerbatimText)
	{
		int cursor = position;
		int dollars = 0;
		bool isVerbatim = false;
		while (At(text, cursor) == '$')
		{
			dollars++;
			cursor++;
		}

		if (At(text, cursor) == '@')
		{
			isVerbatim = true;
			cursor++;
			while (At(text, cursor) == '$')
			{
				dollars++;
				cursor++;
			}
		}

		if (At(text, cursor) != '"')
		{
			return false;
		}

		bool isInterpolated = dollars > 0;
		if (isVerbatim)
		{
			position = cursor + 1;
			ScanVerbatimBody(text, ref position, inVerbatimText, isInterpolated);
			return true;
		}

		int quotes = 0;
		while (At(text, cursor + quotes) == '"')
		{
			quotes++;
		}

		if (quotes >= 3)
		{
			position = cursor + quotes;
			SkipRawBody(text, ref position, quotes);
		}
		else if (quotes == 2)
		{
			// An empty regular string.
			position = cursor + 2;
		}
		else
		{
			position = cursor + 1;
			SkipRegularBody(text, ref position, inVerbatimText, isInterpolated);
		}

		return true;
	}

	private static void ScanVerbatimBody(string text, ref int position, bool[] inVerbatimText, bool isInterpolated)
	{
		while (position < text.Length)
		{
			char c = text[position];
			if (c == '"')
			{
				if (At(text, position + 1) != '"')
				{
					position++;
					return;
				}

				// A doubled quote is an escaped quote.
				inVerbatimText[position] = true;
				inVerbatimText[position + 1] = true;
				position += 2;
			}
			else if (isInterpolated && c == '{' && At(text, position + 1) != '{')
			{
				SkipInterpolationHole(text, ref position, inVerbatimText);
			}
			else if (isInterpolated && c == '{')
			{
				// A doubled brace is an escaped brace.
				inVerbatimText[position] = true;
				inVerbatimText[position + 1] = true;
				position += 2;
			}
			else
			{
				inVerbatimText[position] = true;
				position++;
			}
		}
	}

	private static void SkipRegularBody(string text, ref int position, bool[] inVerbatimText, bool isInterpolated)
	{
		while (position < text.Length)
		{
			char c = text[position];
			if (c == '\\')
			{
				position += 2;
			}
			else if (c == '"')
			{
				position++;
				return;
			}
			else if (isInterpolated && c == '{' && At(text, position + 1) == '{')
			{
				position += 2;
			}
			else if (isInterpolated && c == '{')
			{
				SkipInterpolationHole(text, ref position, inVerbatimText);
			}
			else if (IsLineBreak(c))
			{
				// Unterminated: leave the line break to the code around it.
				return;
			}
			else
			{
				position++;
			}
		}
	}

	/// <summary>
	/// Moves past a raw string's body and its closing delimiter. A raw string is re-indented like
	/// any other code: the compiler strips the closing delimiter's indentation from every line.
	/// </summary>
	private static void SkipRawBody(string text, ref int position, int delimiterLength)
	{
		while (position < text.Length)
		{
			int run = 0;
			while (At(text, position + run) == '"')
			{
				run++;
			}

			if (run >= delimiterLength)
			{
				position += run;
				return;
			}

			position += Math.Max(run, 1);
		}
	}

	/// <summary>
	/// Moves past an interpolation hole, from its opening brace to just after its closing one.
	/// </summary>
	private static void SkipInterpolationHole(string text, ref int position, bool[] inVerbatimText)
	{
		position++;
		ScanCode(text, ref position, inVerbatimText, lineCommentStarts: null, isInterpolationHole: true);
		if (At(text, position) == '}')
		{
			position++;
		}
	}
}

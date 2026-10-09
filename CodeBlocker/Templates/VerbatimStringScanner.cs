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
		ScanCode(text, ref position, inVerbatimText, isInterpolationHole: false);
		return inVerbatimText;
	}

	/// <summary>
	/// Walks code up to the end of <paramref name="text"/> or, inside an interpolation hole, up to
	/// the brace that closes the hole, which is left for the caller to consume.
	/// </summary>
	private static void ScanCode(string text, ref int position, bool[] inVerbatimText, bool isInterpolationHole)
	{
		int braceDepth = 0;
		while (position < text.Length)
		{
			if (TrySkipCommentOrLiteral(text, ref position, inVerbatimText))
			{
				continue;
			}

			if (isInterpolationHole && IsClosingBrace(text[position], ref braceDepth))
			{
				return;
			}

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
		ScanCode(text, ref position, inVerbatimText, isInterpolationHole: true);
		if (position < text.Length)
		{
			position++;
		}
	}
}

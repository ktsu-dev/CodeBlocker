// Copyright (c) 2023-2026 ktsu-dev contributors

namespace CodeBlocker.Tests;

using ktsu.CodeBlocker;

/// <summary>
/// Factory used by the test suite in place of <see cref="CodeBlocker.Create()"/>.
/// </summary>
/// <remarks>
/// The assertions throughout these tests spell their expected output with CRLF line endings, which
/// only matched the writer's behaviour while it inherited <see cref="Environment.NewLine"/> from the
/// host — so the suite passed on Windows and failed on every other platform. Pinning the terminator
/// here makes those expectations true everywhere, and keeps the CRLF spelling in the expectations
/// (which is far more readable for the multi-line fixtures) rather than splicing
/// <see cref="Environment.NewLine"/> into every literal.
/// <para>
/// The default terminator is covered separately by <c>NewLineTests</c>, which is the only place that
/// should call <see cref="CodeBlocker.Create()"/> directly.
/// </para>
/// </remarks>
internal static class TestCodeBlocker
{
	/// <summary>Creates a CRLF-terminated <see cref="CodeBlocker"/> with the default indent string.</summary>
	/// <returns>A new <see cref="CodeBlocker"/>.</returns>
	internal static CodeBlocker CreateCrLf() =>
		CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.CrLf);

	/// <summary>Creates a CRLF-terminated <see cref="CodeBlocker"/> with a custom indent string.</summary>
	/// <param name="indentString">The string to use for indentation.</param>
	/// <returns>A new <see cref="CodeBlocker"/>.</returns>
	internal static CodeBlocker CreateCrLf(string indentString) =>
		CodeBlocker.Create(indentString, NewLines.CrLf);
}

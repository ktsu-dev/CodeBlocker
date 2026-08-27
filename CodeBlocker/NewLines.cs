// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker;

/// <summary>
/// The line terminators <see cref="CodeBlocker"/> understands, named so that call sites read as
/// intent rather than as escape sequences.
/// </summary>
/// <remarks>
/// Generated code that is committed to a repository has to be byte-identical no matter which
/// operating system produced it, so a generator should pick one of these explicitly rather than
/// inheriting the host's <see cref="System.Environment.NewLine"/>.
/// </remarks>
public static class NewLines
{
	/// <summary>A single line feed, <c>"\n"</c>. The conventional choice for reproducible output.</summary>
	public const string Lf = "\n";

	/// <summary>A carriage return followed by a line feed, <c>"\r\n"</c>.</summary>
	public const string CrLf = "\r\n";

	/// <summary>
	/// The host operating system's line terminator. This is what a <see cref="System.IO.TextWriter"/>
	/// uses by default; <see cref="CodeBlocker"/> deliberately does not, because it makes output
	/// depend on where it was produced. Pass it explicitly when that is what you want.
	/// </summary>
	public static string Host => System.Environment.NewLine;
}

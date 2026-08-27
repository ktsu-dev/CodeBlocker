// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker;

using System.CodeDom.Compiler;

/// <summary>
/// Class to create indented code blocks wrapped in braces.
/// </summary>
public class CodeBlocker : IDisposable
{
	/// <summary>The indent string used when none is specified: a single tab.</summary>
	public const string DefaultIndentString = "\t";

	private readonly StringWriter stringWriter;
	private bool disposedValue;
	private bool shouldDisposeStringWriter;

	private IndentedTextWriter IndentedTextWriter { get; }

	/// <summary>
	/// Get the current indent string being used.
	/// </summary>
	public string IndentString { get; }

	/// <summary>
	/// Get the line terminator written at the end of every line.
	/// </summary>
	/// <remarks>
	/// Defaults to <see cref="NewLines.Host"/>, which makes output depend on the operating system it
	/// was produced on. Generators whose output is committed to a repository should pass an explicit
	/// terminator — <see cref="NewLines.Lf"/> or <see cref="NewLines.CrLf"/> — so the same input
	/// always produces the same bytes.
	/// </remarks>
	public string NewLineString { get; }

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/>.
	/// </summary>
	/// <param name="stringWriter">The <see cref="StringWriter"/> to write to.</param>
	public CodeBlocker(StringWriter stringWriter)
		: this(stringWriter, DefaultIndentString, NewLines.Host)
	{
	}

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/> with a custom indent string.
	/// </summary>
	/// <param name="stringWriter">The <see cref="StringWriter"/> to write to.</param>
	/// <param name="indentString">The string to use for indentation.</param>
	public CodeBlocker(StringWriter stringWriter, string indentString)
		: this(stringWriter, indentString, NewLines.Host)
	{
	}

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/> with a custom indent string and line terminator.
	/// </summary>
	/// <param name="stringWriter">The <see cref="StringWriter"/> to write to.</param>
	/// <param name="indentString">The string to use for indentation.</param>
	/// <param name="newLineString">
	/// The line terminator to write at the end of every line. <see langword="null"/> selects
	/// <see cref="NewLines.Host"/>.
	/// </param>
	/// <exception cref="ArgumentNullException"><paramref name="stringWriter"/> is <see langword="null"/>.</exception>
	public CodeBlocker(StringWriter stringWriter, string indentString, string newLineString)
	{
		ArgumentNullException.ThrowIfNull(stringWriter);

		// indentString is deliberately not null-checked: a null indent has always meant "no
		// indentation" here, and CreateWithNullIndentStringShouldWork pins that behaviour.
		newLineString ??= NewLines.Host;

		this.stringWriter = stringWriter;
		IndentString = indentString;
		NewLineString = newLineString;

		// The terminator is set on both writers rather than on IndentedTextWriter alone: its NewLine
		// property forwards to the inner writer on modern targets, but CodeBlocker also ships for
		// netstandard2.0, where the running framework supplies IndentedTextWriter and that forwarding
		// is not guaranteed. Setting both is cheap and makes the behaviour identical everywhere.
		stringWriter.NewLine = newLineString;
		IndentedTextWriter = new IndentedTextWriter(stringWriter, indentString)
		{
			NewLine = newLineString
		};
	}

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/>.
	/// </summary>
	/// <returns>A new instance of <see cref="CodeBlocker"/>.</returns>
	public static CodeBlocker Create() => Create(DefaultIndentString, NewLines.Host);

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/> with a custom indent string.
	/// </summary>
	/// <param name="indentString">The string to use for indentation.</param>
	/// <returns>A new instance of <see cref="CodeBlocker"/>.</returns>
	public static CodeBlocker Create(string indentString) => Create(indentString, NewLines.Host);

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/> with a custom indent string and line terminator.
	/// </summary>
	/// <param name="indentString">The string to use for indentation.</param>
	/// <param name="newLineString">The line terminator to write at the end of every line.</param>
	/// <returns>A new instance of <see cref="CodeBlocker"/>.</returns>
	public static CodeBlocker Create(string indentString, string newLineString)
	{
#pragma warning disable CA2000 // Dispose objects before losing scope - StringWriter will be disposed by CodeBlocker when shouldDisposeStringWriter is true
		return new(new(), indentString, newLineString)
		{
			shouldDisposeStringWriter = true
		};
#pragma warning restore CA2000 // Dispose objects before losing scope
	}

	/// <summary>
	/// Get the string representation of the code.
	/// </summary>
	/// <returns>The string representation of the code.</returns>
	public override string ToString() => stringWriter.ToString();

	/// <summary>
	/// Write a line of code without indentation.
	/// </summary>
	public void NewLine() => IndentedTextWriter.WriteLineNoTabs(string.Empty);

	/// <summary>
	/// Write a line of code with indentation.
	/// </summary>
	/// <param name="line">The line of code to write.</param>
	public void WriteLine(string line) => IndentedTextWriter.WriteLine(line);

	/// <summary>
	/// Write a line of code with indentation.
	/// </summary>
	public void WriteLine() => IndentedTextWriter.WriteLine();

	/// <summary>
	/// Write a line of code with indentation.
	/// </summary>
	/// <param name="text">The text to write.</param>
	public void Write(string text) => IndentedTextWriter.Write(text);

	/// <summary>
	/// Increase the indentation level.
	/// </summary>
	public void Indent() => IndentedTextWriter.Indent++;

	/// <summary>
	/// Decrease the indentation level.
	/// </summary>
	public void Outdent() => IndentedTextWriter.Indent--;

	/// <summary>
	/// Get/set the current indentation level.
	/// </summary>
	public int CurrentIndent
	{
		get => IndentedTextWriter.Indent;
		set => IndentedTextWriter.Indent = value;
	}

	/// <summary>
	/// Dispose of the <see cref="CodeBlocker"/>.
	/// </summary>
	/// <param name="disposing">True if disposing from Dispose() method, false if from finalizer.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				if (shouldDisposeStringWriter)
				{
					stringWriter.Dispose();
					shouldDisposeStringWriter = false;
				}

				IndentedTextWriter.Dispose();
			}

			disposedValue = true;
		}
	}

	/// <summary>
	/// Dispose of the <see cref="CodeBlocker"/>.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}

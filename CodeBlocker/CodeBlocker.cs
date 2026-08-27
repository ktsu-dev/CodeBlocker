// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.CodeBlocker;

using Polyfills;
using System.CodeDom.Compiler;

/// <summary>
/// Class to create indented code blocks wrapped in braces.
/// </summary>
public class CodeBlocker : IDisposable
{
	/// <summary>The indent string used when none is specified: a single tab.</summary>
	public const string DefaultIndentString = "\t";

	/// <summary>
	/// The line terminator used when none is specified: a line feed.
	/// </summary>
	/// <remarks>
	/// LF rather than the host's terminator, so the same calls produce the same bytes wherever they
	/// run. That is what generated code almost always needs: it gets written to a file, committed,
	/// diffed, or compared against a golden file, and every one of those wants reproducibility more
	/// than it wants the local convention. Pass <see cref="NewLines.Host"/> explicitly for the old
	/// behaviour.
	/// </remarks>
	public const string DefaultNewLineString = NewLines.Lf;

	private readonly TextWriter writer;

	private bool disposedValue;
	private bool shouldDisposeWriter;

	private IndentedTextWriter IndentedTextWriter { get; }

	/// <summary>
	/// Get the current indent string being used.
	/// </summary>
	public string IndentString { get; }

	/// <summary>
	/// Get the line terminator written at the end of every line.
	/// </summary>
	/// <remarks>
	/// Defaults to <see cref="DefaultNewLineString"/>. Pass <see cref="NewLines.Host"/> to follow the
	/// operating system's convention instead, at the cost of output that differs by platform.
	/// </remarks>
	public string NewLineString { get; }

	/// <summary>
	/// Gets a value indicating whether <see cref="ToString"/> can return the generated code.
	/// </summary>
	/// <remarks>
	/// True when this instance writes to a <see cref="StringWriter"/> — which is always the case for
	/// instances from <see cref="Create()"/>. A <see cref="CodeBlocker"/> built over some other
	/// <see cref="TextWriter"/> streams straight through to it and keeps no copy, so the generated
	/// code has to be read from that writer instead.
	/// </remarks>
	public bool IsBuffered => writer is StringWriter;

	/// <summary>
	/// The writer as a <see cref="StringWriter"/> when it is one, otherwise <see langword="null"/>.
	/// Only a <see cref="StringWriter"/> buffers what was written, so this is what lets
	/// <see cref="ToString"/> hand the generated code back. Kept as a property rather than a field
	/// so there is exactly one writer reference to own and dispose.
	/// </summary>
	private StringWriter? BufferedWriter => writer as StringWriter;

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/>.
	/// </summary>
	/// <param name="stringWriter">The <see cref="StringWriter"/> to write to.</param>
	public CodeBlocker(StringWriter stringWriter)
		: this((TextWriter)stringWriter, DefaultIndentString, DefaultNewLineString)
	{
	}

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/> with a custom indent string.
	/// </summary>
	/// <param name="stringWriter">The <see cref="StringWriter"/> to write to.</param>
	/// <param name="indentString">The string to use for indentation.</param>
	public CodeBlocker(StringWriter stringWriter, string indentString)
		: this((TextWriter)stringWriter, indentString, DefaultNewLineString)
	{
	}

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/> with a custom indent string and line terminator.
	/// </summary>
	/// <param name="stringWriter">The <see cref="StringWriter"/> to write to.</param>
	/// <param name="indentString">The string to use for indentation.</param>
	/// <param name="newLineString">
	/// The line terminator to write at the end of every line. <see langword="null"/> selects
	/// <see cref="DefaultNewLineString"/>.
	/// </param>
	public CodeBlocker(StringWriter stringWriter, string indentString, string newLineString)
		: this((TextWriter)stringWriter, indentString, newLineString)
	{
	}

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/> over any <see cref="TextWriter"/>.
	/// </summary>
	/// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
	/// <remarks>
	/// The writer is not disposed by <see cref="Dispose()"/> — whoever created it owns it. Only the
	/// <see cref="StringWriter"/> that <see cref="Create()"/> makes for itself is disposed here.
	/// </remarks>
	public CodeBlocker(TextWriter writer)
		: this(writer, DefaultIndentString, DefaultNewLineString)
	{
	}

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/> over any <see cref="TextWriter"/> with a
	/// custom indent string.
	/// </summary>
	/// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
	/// <param name="indentString">The string to use for indentation.</param>
	public CodeBlocker(TextWriter writer, string indentString)
		: this(writer, indentString, DefaultNewLineString)
	{
	}

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/> over any <see cref="TextWriter"/> with a
	/// custom indent string and line terminator.
	/// </summary>
	/// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
	/// <param name="indentString">The string to use for indentation.</param>
	/// <param name="newLineString">
	/// The line terminator to write at the end of every line. <see langword="null"/> selects
	/// <see cref="DefaultNewLineString"/>.
	/// </param>
	/// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
	public CodeBlocker(TextWriter writer, string indentString, string newLineString)
	{
		Ensure.NotNull(writer);

		// indentString is deliberately not null-checked: a null indent has always meant "no
		// indentation" here, and CreateWithNullIndentStringShouldWork pins that behaviour.
		newLineString ??= DefaultNewLineString;

		this.writer = writer;
		IndentString = indentString;
		NewLineString = newLineString;

		// The terminator is set on both writers rather than on IndentedTextWriter alone: its NewLine
		// property forwards to the inner writer on modern targets, but CodeBlocker also ships for
		// netstandard2.0, where the running framework supplies IndentedTextWriter and that forwarding
		// is not guaranteed. Setting both is cheap and makes the behaviour identical everywhere.
		writer.NewLine = newLineString;
		IndentedTextWriter = new IndentedTextWriter(writer, indentString)
		{
			NewLine = newLineString
		};
	}

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/>.
	/// </summary>
	/// <returns>A new instance of <see cref="CodeBlocker"/>.</returns>
	public static CodeBlocker Create() => Create(DefaultIndentString, DefaultNewLineString);

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/> with a custom indent string.
	/// </summary>
	/// <param name="indentString">The string to use for indentation.</param>
	/// <returns>A new instance of <see cref="CodeBlocker"/>.</returns>
	public static CodeBlocker Create(string indentString) => Create(indentString, DefaultNewLineString);

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/> with a custom indent string and line terminator.
	/// </summary>
	/// <param name="indentString">The string to use for indentation.</param>
	/// <param name="newLineString">The line terminator to write at the end of every line.</param>
	/// <returns>A new instance of <see cref="CodeBlocker"/>.</returns>
	public static CodeBlocker Create(string indentString, string newLineString)
	{
#pragma warning disable CA2000 // Dispose objects before losing scope - the StringWriter is disposed by CodeBlocker because shouldDisposeWriter is true
		return new(new StringWriter(), indentString, newLineString)
		{
			shouldDisposeWriter = true
		};
#pragma warning restore CA2000 // Dispose objects before losing scope
	}

	/// <summary>
	/// Get the string representation of the code.
	/// </summary>
	/// <returns>
	/// The code written so far when this instance is buffered — see <see cref="IsBuffered"/> — and
	/// otherwise the type name, as <see cref="object.ToString"/> would give.
	/// </returns>
	/// <remarks>
	/// A <see cref="CodeBlocker"/> over a non-buffering <see cref="TextWriter"/> (a file, a network
	/// stream) keeps no copy of what it wrote, so there is nothing to hand back; read the generated
	/// code from that writer instead. This returns the type name rather than throwing so that
	/// diagnostics and debuggers, which call <see cref="ToString"/> freely, stay safe.
	/// </remarks>
	public override string ToString() => BufferedWriter?.ToString() ?? base.ToString() ?? nameof(CodeBlocker);

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
				if (shouldDisposeWriter)
				{
					writer.Dispose();
					shouldDisposeWriter = false;
				}

				// Disposing the IndentedTextWriter does not dispose the writer it wraps, so a
				// caller-supplied writer survives this and stays theirs to dispose.
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

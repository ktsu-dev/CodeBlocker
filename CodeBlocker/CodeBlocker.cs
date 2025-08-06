// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.CodeBlocker;
using System.CodeDom.Compiler;

/// <summary>
/// Class to create indented code blocks wrapped in braces.
/// </summary>
/// <remarks>
/// Create a new instance of <see cref="CodeBlocker"/>.
/// </remarks>
/// <param name="stringWriter">The <see cref="StringWriter"/> to write to.</param>
public class CodeBlocker(StringWriter stringWriter) : IDisposable
{
	private bool disposedValue;
	private bool shouldDisposeStringWriter;

	private IndentedTextWriter IndentedTextWriter { get; set; } = new(stringWriter, "\t");

	/// <summary>
	/// Get the current indent string being used.
	/// </summary>
	public string IndentString { get; private set; } = "\t";

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/> with a custom indent string.
	/// </summary>
	/// <param name="stringWriter">The <see cref="StringWriter"/> to write to.</param>
	/// <param name="indentString">The string to use for indentation.</param>
	public CodeBlocker(StringWriter stringWriter, string indentString) : this(stringWriter)
	{
		IndentString = indentString;
		IndentedTextWriter.Dispose(); // Dispose the default one
		IndentedTextWriter = new IndentedTextWriter(stringWriter, indentString);
	}

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/>.
	/// </summary>
	/// <returns>A new instance of <see cref="CodeBlocker"/>.</returns>
	public static CodeBlocker Create()
	{
#pragma warning disable CA2000 // Dispose objects before losing scope - StringWriter will be disposed by CodeBlocker when shouldDisposeStringWriter is true
		return new(new())
		{
			shouldDisposeStringWriter = true
		};
#pragma warning restore CA2000 // Dispose objects before losing scope
	}

	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/> with a custom indent string.
	/// </summary>
	/// <param name="indentString">The string to use for indentation.</param>
	/// <returns>A new instance of <see cref="CodeBlocker"/>.</returns>
	public static CodeBlocker Create(string indentString)
	{
#pragma warning disable CA2000 // Dispose objects before losing scope - StringWriter will be disposed by CodeBlocker when shouldDisposeStringWriter is true
		return new(new(), indentString)
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

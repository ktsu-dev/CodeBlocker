using System.CodeDom.Compiler;

namespace ktsu.io.CodeBlocker;

/// <summary>
/// Class to create indented code blocks wrapped in braces.
/// </summary>
public class CodeBlocker : IndentedTextWriter
{
	/// <summary>
	/// Create a new instance of <see cref="CodeBlocker"/>.
	/// </summary>
	/// <returns>A new instance of <see cref="CodeBlocker"/>.</returns>
	public static CodeBlocker Create() => new(new());

	private StringWriter StringWriter { get; set; }
	internal CodeBlocker(StringWriter stringWriter) : base(stringWriter, "\t") => StringWriter = stringWriter;

	/// <summary>
	/// Get the string representation of the code.
	/// </summary>
	/// <returns>The string representation of the code.</returns>
	public override string ToString() => InnerWriter.ToString() ?? string.Empty;

	/// <summary>
	/// Write a line of code without indentation.
	/// </summary>
	public new void NewLine() => WriteLineNoTabs(string.Empty);

	/// <summary>
	/// Dispose of the <see cref="CodeBlocker"/>.
	/// </summary>
	/// <param name="disposing"></param>
	protected override void Dispose(bool disposing)
	{
		if (disposing && StringWriter != null)
		{
			StringWriter.Dispose();
		}

		base.Dispose(disposing);
	}
}

/// <summary>
/// Class to create a scope in a code block.
/// </summary>
public class Scope : IDisposable
{
	private bool disposedValue;

	private CodeBlocker? CodeBlocker { get; set; }

	/// <summary>
	/// Create a new instance of <see cref="Scope"/>.
	/// </summary>
	/// <param name="codeBlocker">The parent <see cref="CodeBlocker"/>.</param>
	public Scope(CodeBlocker codeBlocker)
	{
		CodeBlocker = codeBlocker;
		CodeBlocker.WriteLine("{");
		CodeBlocker.Indent++;
	}

	/// <summary>
	/// Dispose of the <see cref="Scope"/>.
	/// </summary>
	/// <param name="disposing"></param>
	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing && CodeBlocker != null)
			{
				CodeBlocker.Indent--;
				CodeBlocker.WriteLine("};");
			}

			CodeBlocker = null;
			disposedValue = true;
		}
	}

	/// <summary>
	/// Dispose of the <see cref="Scope"/>.
	/// </summary>
	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}

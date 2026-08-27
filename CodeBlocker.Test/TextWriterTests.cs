// Copyright (c) 2023-2026 ktsu-dev contributors

namespace CodeBlocker.Tests;

using System.Text;
using ktsu.CodeBlocker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers building a <see cref="CodeBlocker"/> over an arbitrary <see cref="TextWriter"/> rather
/// than only over a <see cref="StringWriter"/>.
/// </summary>
[TestClass]
public sealed class TextWriterTests
{
	/// <summary>
	/// A <see cref="TextWriter"/> that is not a <see cref="StringWriter"/>, so it exercises the
	/// unbuffered path, and that records whether it was disposed.
	/// </summary>
	private sealed class RecordingWriter : TextWriter
	{
		private readonly StringBuilder builder = new();

		public bool Disposed { get; private set; }

		public override Encoding Encoding => Encoding.UTF8;

		public override void Write(char value) => builder.Append(value);

		public override string ToString() => builder.ToString();

		protected override void Dispose(bool disposing)
		{
			Disposed = true;
			base.Dispose(disposing);
		}
	}

	[TestMethod]
	public void WritesIndentedOutputToAnArbitraryTextWriter()
	{
		using RecordingWriter target = new();

		using (CodeBlocker codeBlocker = new(target, CodeBlocker.DefaultIndentString, NewLines.Lf))
		{
			codeBlocker.WriteLine("class C");
			using Scope scope = new(codeBlocker);
			codeBlocker.WriteLine("int x;");
		}

		Assert.AreEqual("class C\n{\n\tint x;\n}\n", target.ToString());
	}

	[TestMethod]
	public void ACallerSuppliedWriterIsNotDisposed()
	{
		using RecordingWriter target = new();

		using (CodeBlocker codeBlocker = new(target))
		{
			codeBlocker.WriteLine("a");
		}

		Assert.IsFalse(target.Disposed, "CodeBlocker must not dispose a writer it did not create.");
	}

	[TestMethod]
	public void ACallerSuppliedStringWriterIsNotDisposed()
	{
		StringWriter target = new();

		using (CodeBlocker codeBlocker = new(target))
		{
			codeBlocker.WriteLine("a");
		}

		// A disposed StringWriter throws from Write; reaching this line means it is still usable.
		target.Write("still open");
		Assert.IsTrue(target.ToString().EndsWith("still open", StringComparison.Ordinal));
		target.Dispose();
	}

	[TestMethod]
	public void AWriterCreateOwnsIsDisposed()
	{
		CodeBlocker codeBlocker = CodeBlocker.Create();
		codeBlocker.WriteLine("a");
		codeBlocker.Dispose();

		// Writing after disposal would throw if the StringWriter were still open; instead the
		// already-buffered text is all that remains readable.
		Assert.AreEqual($"a{CodeBlocker.DefaultNewLineString}", codeBlocker.ToString());
	}

	[TestMethod]
	public void IsBufferedIsTrueForStringWriterBackedInstances()
	{
		using CodeBlocker created = CodeBlocker.Create();
		using StringWriter stringWriter = new();
		using CodeBlocker overStringWriter = new(stringWriter);

		Assert.IsTrue(created.IsBuffered);
		Assert.IsTrue(overStringWriter.IsBuffered);
	}

	[TestMethod]
	public void IsBufferedIsFalseForOtherWriters()
	{
		using RecordingWriter target = new();
		using CodeBlocker codeBlocker = new(target);

		Assert.IsFalse(codeBlocker.IsBuffered);
	}

	[TestMethod]
	public void ToStringReturnsTheTypeNameWhenThereIsNothingBuffered()
	{
		using RecordingWriter target = new();
		using CodeBlocker codeBlocker = new(target);

		codeBlocker.WriteLine("a");

		// Documented behaviour: no copy is kept, and ToString does not throw, so debuggers and
		// diagnostics stay safe. The generated code is read from the writer instead.
		Assert.AreEqual(typeof(CodeBlocker).ToString(), codeBlocker.ToString());
		Assert.AreEqual("a" + CodeBlocker.DefaultNewLineString, target.ToString());
	}

	[TestMethod]
	public void NullTextWriterThrows() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => new CodeBlocker((TextWriter)null!));

	[TestMethod]
	public void IndentAndTerminatorApplyToAnArbitraryWriter()
	{
		using RecordingWriter target = new();
		using CodeBlocker codeBlocker = new(target, "  ", NewLines.CrLf);

		codeBlocker.Indent();
		codeBlocker.WriteLine("x");

		Assert.AreEqual("  x\r\n", target.ToString());
		Assert.AreEqual("  ", codeBlocker.IndentString);
		Assert.AreEqual(NewLines.CrLf, codeBlocker.NewLineString);
	}
}

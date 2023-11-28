using System.CodeDom.Compiler;

namespace ktsu.io.CodeBlocker
{
	public class CodeBlocker : IndentedTextWriter
	{
		public static CodeBlocker Create() => new(new());

		private StringWriter StringWriter { get; set; }
		internal CodeBlocker(StringWriter stringWriter) : base(stringWriter, "\t")
		{
			StringWriter = stringWriter;
		}

		public override string ToString() => InnerWriter.ToString() ?? string.Empty;

		public new void NewLine() => WriteLineNoTabs(string.Empty);

		protected override void Dispose(bool disposing)
		{
			if (disposing && StringWriter != null)
			{
				StringWriter.Dispose();
			}

			base.Dispose(disposing);
		}
	}

	public class Scope : IDisposable
	{
		private bool disposedValue;

		private CodeBlocker? CodeBlocker { get; set; }

		public Scope(CodeBlocker codeBlocker)
		{
			CodeBlocker = codeBlocker;
			CodeBlocker.WriteLine("{");
			CodeBlocker.Indent++;
		}

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

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
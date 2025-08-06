// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.CodeBlocker;

using ktsu.ScopedAction;

/// <summary>
/// Class to create a scope in a code block.
/// </summary>
/// <remarks>
/// Create a new instance of <see cref="Scope"/>.
/// </remarks>
/// <param name="codeBlocker">The parent <see cref="CodeBlocker"/>.</param>
public class Scope(CodeBlocker codeBlocker)
	: ScopedAction(onOpen: () => BeginScope(codeBlocker), onClose: () => EndScope(codeBlocker))
{
	private static void BeginScope(CodeBlocker codeBlocker)
	{
		codeBlocker.WriteLine("{");
		codeBlocker.Indent();
	}

	private static void EndScope(CodeBlocker codeBlocker)
	{
		codeBlocker.Outdent();
		codeBlocker.WriteLine("};");
	}
}

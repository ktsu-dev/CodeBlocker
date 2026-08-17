// Copyright (c) 2023-2026 ktsu-dev contributors

using System.Diagnostics.CodeAnalysis;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]
[assembly: SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Test classes need to be public for MSTest discovery", Scope = "namespaceanddescendants", Target = "~N:CodeBlocker.Tests")]

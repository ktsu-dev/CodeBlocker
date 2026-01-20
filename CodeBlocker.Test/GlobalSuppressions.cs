// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]
[assembly: SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Test classes need to be public for MSTest discovery", Scope = "namespaceanddescendants", Target = "~N:CodeBlocker.Tests")]

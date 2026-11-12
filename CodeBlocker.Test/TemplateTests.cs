// Copyright (c) 2023-2026 ktsu-dev contributors

namespace CodeBlocker.Tests;

using System.Text;
using ktsu.CodeBlocker;
using ktsu.CodeBlocker.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Per-template render tests. Each one pins the exact text a template produces.
/// </summary>
[TestClass]
public sealed class TemplateTests
{
	private static string Render(Action<CodeBlocker> write)
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		write(codeBlocker);
		return codeBlocker.ToString();
	}

	private static string Render(TemplateBase template) => Render(template.WriteTo);

	[TestMethod]
	public void RegionNameWithAVerbatimPrefixKeepsTheMemberIndented()
	{
		// ktsu-dev/CodeBlocker#194.
		ClassTemplate template = new()
		{
			Name = "C",
			Members =
			{
				new MethodTemplate
				{
					Type = "void",
					Name = "M",
					BodyFactory = b =>
					{
						b.WriteLine("{");
						b.Indent();
						using (new RegionScope(b, "Paths like @\"C:\\"))
						{
							b.WriteLine("int a = 1;");
							b.WriteLine("int b = 2;");
						}

						b.Outdent();
						b.WriteLine("}");
					},
				},
			},
		};

		Assert.AreEqual(
			"class C\n{\n\tvoid M()\n\t{\n\t\t#region Paths like @\"C:\\\n\t\tint a = 1;\n\t\tint b = 2;\n\t\t#endregion\n\t}\n}\n",
			Render(template));
	}

	#region Blank entries

	[TestMethod]
	public void BlankInterfacesWriteNoBaseList() =>
		Assert.AreEqual(
			"class C\n{\n}\n",
			Render(new ClassTemplate { Name = "C", Interfaces = { "", " " } }));

	[TestMethod]
	public void BlankInterfacesAreSkippedBetweenRealOnes() =>
		Assert.AreEqual(
			"class D : B, I\n{\n}\n",
			Render(new ClassTemplate { Name = "D", BaseClass = "B", Interfaces = { "", "I", "  " } }));

	[TestMethod]
	public void BlankTypeParametersWriteNoAngleBrackets()
	{
		Assert.AreEqual("class E\n{\n}\n", Render(new ClassTemplate { Name = "E", TypeParameters = { "" } }));
		Assert.AreEqual(
			"void M<T>() { }\n",
			Render(new MethodTemplate { Name = "M", Type = "void", TypeParameters = { "T", "" }, BodyFactory = _ => { } }));
	}

	[TestMethod]
	public void BlankAttributesWriteNoEmptyBrackets()
	{
		Assert.AreEqual(
			"[Obsolete]\nvoid M() { }\n",
			Render(new MethodTemplate { Name = "M", Type = "void", Attributes = { "", "Obsolete", " " }, BodyFactory = _ => { } }));
		Assert.AreEqual(
			"[NotNull] int x",
			Render(new ParameterTemplate { Type = "int", Name = "x", Attributes = { "", "NotNull" } }));
	}

	[TestMethod]
	public void BlankKeywordsWriteNoExtraSpace() =>
		Assert.AreEqual(
			"public static void M() { }\n",
			Render(new MethodTemplate { Name = "M", Type = "void", Keywords = { "public", "", "static" }, BodyFactory = _ => { } }));

	[TestMethod]
	public void BlankConstraintsWriteNoClauseLine()
	{
		Assert.AreEqual(
			"void M<T>() { }\n",
			Render(new MethodTemplate { Name = "M", Type = "void", TypeParameters = { "T" }, Constraints = { "" }, BodyFactory = _ => { } }));
		Assert.AreEqual(
			"void M<T>()\n\twhere T : struct { }\n",
			Render(new MethodTemplate { Name = "M", Type = "void", TypeParameters = { "T" }, Constraints = { "where T : struct", " " }, BodyFactory = _ => { } }));
	}

	[TestMethod]
	public void BlankBaseParametersAreSkipped()
	{
		Assert.AreEqual(
			"F()\n\t: base(a) { }\n",
			Render(new ConstructorTemplate { Name = "F", BaseParameters = { "a", "" } }));
		Assert.AreEqual("F() { }\n", Render(new ConstructorTemplate { Name = "F", BaseParameters = { "" } }));
		Assert.AreEqual(
			"F()\n\t: this() { }\n",
			Render(new ConstructorTemplate { Name = "F", ChainsToThis = true, BaseParameters = { " " } }));
	}

	[TestMethod]
	public void OnlyBlankFileAttributesKeepTheUsingsInsideTheNamespace()
	{
		SourceFileTemplate file = new() { Namespace = "N", Usings = { "System" }, Attributes = { "" } };
		SourceFileTemplate plain = new() { Namespace = "N", Usings = { "System" } };

		Assert.AreEqual(Render(cb => cb.AddSourceFile(plain)), Render(cb => cb.AddSourceFile(file)));
	}

	#endregion

	#region ParameterTemplate

	[TestMethod]
	public void ParameterIsTypeThenName() =>
		Assert.AreEqual("int value", Render(new ParameterTemplate { Type = "int", Name = "value" }));

	[TestMethod]
	public void ParameterWritesItsDefaultValue() =>
		Assert.AreEqual("int value = 1", Render(new ParameterTemplate { Type = "int", Name = "value", DefaultValue = "1" }));

	[TestMethod]
	public void ParameterQuotesItsDefaultValueWhenAsked() =>
		Assert.AreEqual(
			"string name = \"none\"",
			Render(new ParameterTemplate { Type = "string", Name = "name", DefaultValue = "none", DefaultValueIsQuoted = true }));

	[TestMethod]
	[DataRow(@"C:\temp", @"""C:\\temp""", DisplayName = "Backslash")]
	[DataRow("say \"hi\"", @"""say \""hi\""""", DisplayName = "Quote")]
	[DataRow("one\ntwo\r\n", @"""one\ntwo\r\n""", DisplayName = "Newline")]
	[DataRow("tab\there\0", @"""tab\there\0""", DisplayName = "Tab and null")]
	[DataRow("bell\u0007 sep\u2028", @"""bell\a sep\u2028""", DisplayName = "Other control and separator")]
	public void ParameterEscapesAQuotedDefaultValue(string value, string expectedLiteral) =>
		Assert.AreEqual(
			$"string name = {expectedLiteral}",
			Render(new ParameterTemplate { Type = "string", Name = "name", DefaultValue = value, DefaultValueIsQuoted = true }));

	[TestMethod]
	public void ParameterEscapesALoneSurrogateInAQuotedDefaultValue()
	{
		// These are built in code rather than passed through DataRow: an attribute argument is
		// stored as UTF-8, which cannot carry a lone surrogate either.
		(string Value, string ExpectedLiteral)[] cases =
		[
			("a\uD800b", @"""a\ud800b"""),
			("a\uDC00b", @"""a\udc00b"""),
			("end\uD83D", @"""end\ud83d"""),
			("\uDE00start", @"""\ude00start"""),
			("\uDE00\uD83D", @"""\ude00\ud83d"""),
			("pair \uD83D\uDE00", "\"pair \uD83D\uDE00\""),
		];

		UTF8Encoding strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
		foreach ((string value, string expectedLiteral) in cases)
		{
			string rendered = Render(new ParameterTemplate { Type = "string", Name = "name", DefaultValue = value, DefaultValueIsQuoted = true });

			Assert.AreEqual($"string name = {expectedLiteral}", rendered);
			_ = strictUtf8.GetBytes(rendered);
		}
	}

	[TestMethod]
	public void ParameterWritesAQuotedEmptyDefaultValue() =>
		Assert.AreEqual(
			"string separator = \"\"",
			Render(new ParameterTemplate { Type = "string", Name = "separator", DefaultValue = "", DefaultValueIsQuoted = true }));

	[TestMethod]
	public void ParameterWritesAQuotedNullDefaultValueAsEmpty() =>
		// ktsu-dev/CodeBlocker#209: a null quoted default threw NullReferenceException mid-declaration.
		Assert.AreEqual(
			"string separator = \"\"",
			Render(new ParameterTemplate { Type = "string", Name = "separator", DefaultValue = null!, DefaultValueIsQuoted = true }));

	[TestMethod]
	public void AQuotedNullDefaultKeepsAMethodWhole()
	{
		ClassTemplate type = new()
		{
			Name = "C",
			Members =
			{
				new MethodTemplate
				{
					Type = "void",
					Name = "M",
					BodyFactory = _ => { },
					Parameters = { new ParameterTemplate { Type = "string", Name = "s", DefaultValue = null!, DefaultValueIsQuoted = true } },
				},
			},
		};

		StringAssert.Contains(Render(type), "void M(string s = \"\") { }");
	}

	[TestMethod]
	public void ParameterWithAnEmptyUnquotedDefaultValueHasNone() =>
		Assert.AreEqual("string separator", Render(new ParameterTemplate { Type = "string", Name = "separator", DefaultValue = "" }));

	[TestMethod]
	public void AQuotedEmptyDefaultKeepsAParameterOptional()
	{
		MethodTemplate method = new()
		{
			Type = "void",
			Name = "M",
			Keywords = { "public" },
			BodyFactory = _ => { },
			Parameters =
			{
				new ParameterTemplate { Type = "int", Name = "count", DefaultValue = "0" },
				new ParameterTemplate { Type = "string", Name = "separator", DefaultValue = "", DefaultValueIsQuoted = true },
			},
		};

		Assert.AreEqual("public void M(int count = 0, string separator = \"\") { }\n", Render(method));
	}

	[TestMethod]
	public void ParameterKeepsItsAttributesOnTheDeclarationLine()
	{
		ParameterTemplate parameter = new() { Type = "int", Name = "value", Attributes = { "In" }, Keywords = { "ref" } };

		Assert.AreEqual("[In] ref int value", Render(parameter));
	}

	#endregion

	#region FieldTemplate

	[TestMethod]
	public void FieldIsTerminatedWithASemicolon() =>
		Assert.AreEqual("private int count;\n", Render(new FieldTemplate { Type = "int", Name = "count", Keywords = { "private" } }));

	[TestMethod]
	public void FieldWritesItsInitialiser() =>
		Assert.AreEqual(
			"private const int Max = 10;\n",
			Render(new FieldTemplate { Type = "int", Name = "Max", Keywords = { "private", "const" }, DefaultValue = "10" }));

	[TestMethod]
	public void FieldEscapesAQuotedInitialiser() =>
		Assert.AreEqual(
			"private string p = \"C:\\\\temp\\\\\\\"x\\\"\";\n",
			Render(new FieldTemplate { Type = "string", Name = "p", Keywords = { "private" }, DefaultValue = "C:\\temp\\\"x\"", DefaultValueIsQuoted = true }));

	[TestMethod]
	public void FieldWritesAQuotedEmptyInitialiser() =>
		Assert.AreEqual(
			"public const string Empty = \"\";\n",
			Render(new FieldTemplate { Type = "string", Name = "Empty", Keywords = { "public", "const" }, DefaultValue = "", DefaultValueIsQuoted = true }));

	[TestMethod]
	public void FieldWritesAQuotedNullInitialiserAsEmpty() =>
		// ktsu-dev/CodeBlocker#209.
		Assert.AreEqual(
			"private string label = \"\";\n",
			Render(new FieldTemplate { Type = "string", Name = "label", Keywords = { "private" }, DefaultValue = null!, DefaultValueIsQuoted = true }));

	[TestMethod]
	public void FieldCommentsAndAttributesGoOnTheirOwnLines()
	{
		FieldTemplate field = new()
		{
			Type = "int",
			Name = "count",
			Keywords = { "private" },
			Comments = { "/// <summary>How many.</summary>" },
			Attributes = { "Obsolete", "NonSerialized" },
		};

		Assert.AreEqual(
			"""
			/// <summary>How many.</summary>
			[Obsolete]
			[NonSerialized]
			private int count;

			""".ReplaceLineEndings("\n"),
			Render(field));
	}

	#endregion

	#region PropertyTemplate

	[TestMethod]
	public void AutomaticAccessorsCollapseToOneLine()
	{
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "Count",
			Keywords = { "public" },
			Getter = AccessorTemplate.Auto(),
			Setter = AccessorTemplate.Auto(),
		};

		Assert.AreEqual("public int Count { get; set; }\n", Render(property));
	}

	[TestMethod]
	public void AGetOnlyAutomaticPropertyIsWhatAnAbstractPropertyLooksLike()
	{
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "Count",
			Keywords = { "public", "abstract" },
			Getter = AccessorTemplate.Auto(),
		};

		Assert.AreEqual("public abstract int Count { get; }\n", Render(property));
	}

	[TestMethod]
	public void AnInitOnlySetterUsesTheInitKeyword()
	{
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "Count",
			Keywords = { "public" },
			Getter = AccessorTemplate.Auto(),
			Setter = AccessorTemplate.Auto(),
			SetterIsInitOnly = true,
		};

		Assert.AreEqual("public int Count { get; init; }\n", Render(property));
	}

	[TestMethod]
	public void AShorthandPropertyKeepsItsInitialiser()
	{
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "Count",
			Keywords = { "public" },
			Getter = AccessorTemplate.Auto(),
			Setter = AccessorTemplate.Auto(),
			DefaultValue = "7",
		};

		Assert.AreEqual("public int Count { get; set; } = 7;\n", Render(property));
	}

	[TestMethod]
	public void AShorthandPropertyKeepsAQuotedEmptyInitialiser()
	{
		PropertyTemplate property = new()
		{
			Type = "string",
			Name = "Prefix",
			Keywords = { "public" },
			Getter = AccessorTemplate.Auto(),
			Setter = AccessorTemplate.Auto(),
			DefaultValue = "",
			DefaultValueIsQuoted = true,
		};

		Assert.AreEqual("public string Prefix { get; set; } = \"\";\n", Render(property));
	}

	[TestMethod]
	public void APropertyWritesAQuotedNullInitialiserAsEmpty()
	{
		// ktsu-dev/CodeBlocker#209.
		PropertyTemplate property = new()
		{
			Type = "string",
			Name = "Prefix",
			Keywords = { "public" },
			Getter = AccessorTemplate.Auto(),
			Setter = AccessorTemplate.Auto(),
			DefaultValue = null!,
			DefaultValueIsQuoted = true,
		};

		Assert.AreEqual("public string Prefix { get; set; } = \"\";\n", Render(property));
	}

	[TestMethod]
	public void ABracedPropertyKeepsAQuotedEmptyInitialiser()
	{
		PropertyTemplate property = new()
		{
			Type = "string",
			Name = "Prefix",
			Keywords = { "public" },
			Getter = AccessorTemplate.Auto(),
			Setter = new AccessorTemplate { Modifier = "private" },
			DefaultValue = "",
			DefaultValueIsQuoted = true,
		};

		Assert.AreEqual(
			"""
			public string Prefix
			{
				get;
				private set;
			} = "";

			""".ReplaceLineEndings("\n"),
			Render(property));
	}

	[TestMethod]
	public void AnAccessorModifierForcesTheBracedForm()
	{
		// The whole point of modelling accessors as data: a modifier is expressible, and it is what
		// decides the shape rather than the identity of a callback.
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "Count",
			Keywords = { "public" },
			Getter = AccessorTemplate.Auto(),
			Setter = new AccessorTemplate { Modifier = "private" },
		};

		Assert.AreEqual(
			"""
			public int Count
			{
				get;
				private set;
			}

			""".ReplaceLineEndings("\n"),
			Render(property));
	}

	[TestMethod]
	public void AnExpressionBodiedPropertyStaysOnOneLine()
	{
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "Doubled",
			Keywords = { "public" },
			ExpressionBodyFactory = codeBlocker => codeBlocker.Write("count * 2"),
		};

		Assert.AreEqual("public int Doubled => count * 2;\n", Render(property));
	}

	[TestMethod]
	public void AnExpressionBodiedAccessorIsWrittenInFull()
	{
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "Count",
			Keywords = { "public" },
			Getter = AccessorTemplate.Expression(codeBlocker => codeBlocker.Write("count")),
			Setter = AccessorTemplate.Expression(codeBlocker => codeBlocker.Write("count = value")),
		};

		Assert.AreEqual(
			"""
			public int Count
			{
				get => count;
				set => count = value;
			}

			""".ReplaceLineEndings("\n"),
			Render(property));
	}

	[TestMethod]
	public void AMultiLineExpressionBodiedPropertyKeepsTheEnclosingIndent()
	{
		ClassTemplate type = new()
		{
			Name = "C",
			Keywords = { "public" },
			Members =
			{
				new PropertyTemplate
				{
					Type = "int",
					Name = "X",
					Keywords = { "public" },
					ExpressionBodyFactory = codeBlocker =>
					{
						codeBlocker.WriteLine("k switch");
						codeBlocker.WriteLine("{");
						codeBlocker.Indent();
						codeBlocker.WriteLine("_ => 0,");
						codeBlocker.Outdent();
						codeBlocker.Write("}");
					},
				},
			},
		};

		Assert.AreEqual(
			"""
			public class C
			{
				public int X => k switch
				{
					_ => 0,
				};
			}

			""".ReplaceLineEndings("\n"),
			Render(type));
	}

	[TestMethod]
	public void AMultiLineExpressionBodiedAccessorKeepsTheEnclosingIndent()
	{
		ClassTemplate type = new()
		{
			Name = "C",
			Keywords = { "public" },
			Members =
			{
				new PropertyTemplate
				{
					Type = "int[]",
					Name = "Items",
					Keywords = { "public" },
					Getter = AccessorTemplate.Expression(codeBlocker =>
					{
						codeBlocker.WriteLine("[");
						codeBlocker.Indent();
						codeBlocker.WriteLine("1,");
						codeBlocker.WriteLine("2,");
						codeBlocker.Outdent();
						codeBlocker.WriteLine("]");
					}),
				},
			},
		};

		Assert.AreEqual(
			"""
			public class C
			{
				public int[] Items
				{
					get => [
						1,
						2,
					];
				}
			}

			""".ReplaceLineEndings("\n"),
			Render(type));
	}

	[TestMethod]
	public void AnExpressionBodyEndingInALineTerminatorKeepsItsSemicolonOnTheLastLine()
	{
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "Doubled",
			Keywords = { "public" },
			ExpressionBodyFactory = codeBlocker => codeBlocker.WriteLine("count * 2"),
		};

		Assert.AreEqual("public int Doubled => count * 2;\n", Render(property));
	}

	[TestMethod]
	public void AMultiLineExpressionBodyKeepsABlankLineWithoutIndentingIt()
	{
		ClassTemplate type = new()
		{
			Name = "C",
			Keywords = { "public" },
			Members =
			{
				new PropertyTemplate
				{
					Type = "int",
					Name = "X",
					Keywords = { "public" },
					ExpressionBodyFactory = codeBlocker =>
					{
						codeBlocker.WriteLine("a");
						codeBlocker.NewLine();
						codeBlocker.Write("+ b");
					},
				},
			},
		};

		Assert.AreEqual(
			"public class C\n{\n\tpublic int X => a\n\n\t+ b;\n}\n",
			Render(type));
	}

	[TestMethod]
	public void AnExpressionBodyStartingWithABlankLineKeepsItsFirstLineOnTheDeclaration()
	{
		ClassTemplate type = new()
		{
			Name = "C",
			Keywords = { "public" },
			Members =
			{
				new PropertyTemplate
				{
					Type = "int",
					Name = "X",
					Keywords = { "public" },
					ExpressionBodyFactory = codeBlocker =>
					{
						codeBlocker.NewLine();
						codeBlocker.WriteLine("1 +");
						codeBlocker.Write("2");
					},
				},
			},
		};

		Assert.AreEqual(
			"public class C\n{\n\tpublic int X => 1 +\n\t2;\n}\n",
			Render(type));
	}

	[TestMethod]
	public void AnEmptyExpressionBodyIsRejectedBeforeAnythingIsWritten()
	{
		// It used to render "public int X => ;", which does not compile, and raise nothing.
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "X",
			Keywords = { "public" },
			Documentation = new DocComment { Summary = "s" },
			ExpressionBodyFactory = _ => { },
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int a;");

		Assert.ThrowsExactly<InvalidOperationException>(() => property.WriteTo(codeBlocker));
		Assert.AreEqual("int a;\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void AnExpressionBodyOfOnlyBlankLinesIsRejectedBeforeAnythingIsWritten()
	{
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "X",
			ExpressionBodyFactory = codeBlocker =>
			{
				codeBlocker.NewLine();
				codeBlocker.WriteLine("  ");
			},
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int a;");

		Assert.ThrowsExactly<InvalidOperationException>(() => property.WriteTo(codeBlocker));
		Assert.AreEqual("int a;\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void AnExpressionBodyWithADefaultValueIsRejectedBeforeAnythingIsWritten()
	{
		// It used to render "int P => x;" and drop the "= 5" without a word.
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "P",
			DefaultValue = "5",
			ExpressionBodyFactory = codeBlocker => codeBlocker.Write("x"),
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int a;");

		InvalidOperationException exception =
			Assert.ThrowsExactly<InvalidOperationException>(() => property.WriteTo(codeBlocker));
		Assert.Contains("'P'", exception.Message);
		Assert.AreEqual("int a;\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void AClassWithAnExpressionBodiedPropertyThatHasADefaultValueWritesNothing()
	{
		ClassTemplate type = new() { Name = "C" };
		type.Members.Add(new FieldTemplate { Name = "a", Type = "int" });
		type.Members.Add(new PropertyTemplate
		{
			Type = "int",
			Name = "P",
			DefaultValue = "5",
			ExpressionBodyFactory = codeBlocker => codeBlocker.Write("x"),
		});

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		InvalidOperationException exception =
			Assert.ThrowsExactly<InvalidOperationException>(() => codeBlocker.AddClass(type));
		Assert.Contains("'P'", exception.Message);
		Assert.AreEqual(string.Empty, codeBlocker.ToString());
	}

	private static string RenderExpressionBody(Action<CodeBlocker> expressionFactory) =>
		Render(new PropertyTemplate { Type = "int", Name = "X", Keywords = { "public" }, ExpressionBodyFactory = expressionFactory });

	[TestMethod]
	public void AnExpressionBodyEndingInALineCommentGetsItsSemicolonBeforeTheComment() =>
		Assert.AreEqual(
			"public int X => x; // c\n",
			RenderExpressionBody(codeBlocker => codeBlocker.Write("x // c")));

	[TestMethod]
	public void ASwitchExpressionEndingInALineCommentGetsItsSemicolonAfterTheBrace() =>
		Assert.AreEqual(
			"public int X => v switch\n{\n\t_ => 1,\n}; // c\n",
			RenderExpressionBody(codeBlocker =>
			{
				codeBlocker.WriteLine("v switch");
				codeBlocker.WriteLine("{");
				codeBlocker.WriteLine("\t_ => 1,");
				codeBlocker.Write("} // c");
			}));

	[TestMethod]
	public void AnExpressionBodyEndingInACommentOnlyLineKeepsItsSemicolonOnTheLastLineOfCode() =>
		Assert.AreEqual(
			"public int X => x;\n// why\n",
			RenderExpressionBody(codeBlocker =>
			{
				codeBlocker.WriteLine("x");
				codeBlocker.Write("// why");
			}));

	[TestMethod]
	public void ASlashPairInsideAStringLiteralIsNotTakenForAComment() =>
		Assert.AreEqual(
			"public int X => \"http://x\";\n",
			RenderExpressionBody(codeBlocker => codeBlocker.Write("\"http://x\"")));

	[TestMethod]
	public void ASlashPairInAnInterpolationFormatSpecifierIsNotTakenForAComment() =>
		Assert.AreEqual(
			"public int X => $\"{d:yyyy//MM}\"; // c\n",
			RenderExpressionBody(codeBlocker => codeBlocker.Write("$\"{d:yyyy//MM}\" // c")));

	[TestMethod]
	public void ASlashPairInAFormatSpecifierAfterAnAliasQualifierIsNotTakenForAComment() =>
		Assert.AreEqual(
			"public int X => $\"{global::System.DateTime.Now:yyyy//MM}\"; // c\n",
			RenderExpressionBody(codeBlocker => codeBlocker.Write("$\"{global::System.DateTime.Now:yyyy//MM}\" // c")));

	[TestMethod]
	public void ASlashPairInsideAVerbatimStringOpenedOnAnEarlierLineIsNotTakenForAComment() =>
		Assert.AreEqual(
			"public int X => @\"a\n// b\";\n",
			RenderExpressionBody(codeBlocker =>
			{
				codeBlocker.WriteLine("@\"a");
				codeBlocker.Write("// b\"");
			}));

	[TestMethod]
	public void AnExpressionBodiedAccessorEndingInALineCommentGetsItsSemicolonBeforeTheComment()
	{
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "X",
			Keywords = { "public" },
			Getter = AccessorTemplate.Expression(codeBlocker => codeBlocker.Write("x // c")),
		};

		Assert.AreEqual("public int X\n{\n\tget => x; // c\n}\n", Render(property));
	}

	private static void WriteDebugConditional(CodeBlocker codeBlocker)
	{
		codeBlocker.WriteLine("#if DEBUG");
		codeBlocker.WriteLine("1");
		codeBlocker.WriteLine("#else");
		codeBlocker.WriteLine("2");
		codeBlocker.WriteLine("#endif");
	}

	[TestMethod]
	public void AnExpressionBodyOpeningWithADirectiveStartsBelowTheArrowAndEndsAfterTheLastDirective() =>
		// Used to render "int P => #if DEBUG" (CS1040) and "#endif;" (CS1025).
		Assert.AreEqual(
			"int P =>\n#if DEBUG\n1\n#else\n2\n#endif\n;\n",
			Render(new PropertyTemplate { Type = "int", Name = "P", ExpressionBodyFactory = WriteDebugConditional }));

	[TestMethod]
	public void AnExpressionBodiedAccessorWithADirectiveIsTerminatedAfterIt() =>
		Assert.AreEqual(
			"int P\n{\n\tget =>\n\t#if DEBUG\n\t1\n\t#else\n\t2\n\t#endif\n\t;\n}\n",
			Render(new PropertyTemplate { Type = "int", Name = "P", Getter = AccessorTemplate.Expression(WriteDebugConditional) }));

	[TestMethod]
	public void AnExpressionBodyEndingInADirectiveAfterCodeOnTheArrowLineIsTerminatedAfterIt() =>
		Assert.AreEqual(
			"int P => 1\n#if DEBUG\n+ 2\n#endif\n;\n",
			Render(new PropertyTemplate
			{
				Type = "int",
				Name = "P",
				ExpressionBodyFactory = codeBlocker =>
				{
					codeBlocker.WriteLine("1");
					codeBlocker.WriteLine("#if DEBUG");
					codeBlocker.WriteLine("+ 2");
					codeBlocker.WriteLine("#endif");
				},
			}));

	[TestMethod]
	public void AnExpressionBodyWithCodeAfterItsLastDirectiveTerminatesThatCode() =>
		Assert.AreEqual(
			"int P => 1\n#if DEBUG\n+ 2\n#endif\n+ 3;\n",
			Render(new PropertyTemplate
			{
				Type = "int",
				Name = "P",
				ExpressionBodyFactory = codeBlocker =>
				{
					codeBlocker.WriteLine("1");
					codeBlocker.WriteLine("#if DEBUG");
					codeBlocker.WriteLine("+ 2");
					codeBlocker.WriteLine("#endif");
					codeBlocker.WriteLine("+ 3");
				},
			}));

	[TestMethod]
	public void ABlockBodiedAccessorIsBracedAndIndented()
	{
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "Count",
			Keywords = { "public" },
			Getter = AccessorTemplate.Block(codeBlocker =>
			{
				codeBlocker.WriteLine("Refresh();");
				codeBlocker.WriteLine("return count;");
			}),
		};

		Assert.AreEqual(
			"""
			public int Count
			{
				get
				{
					Refresh();
					return count;
				}
			}

			""".ReplaceLineEndings("\n"),
			Render(property));
	}

	[TestMethod]
	public void APropertyWithNoAccessorsAtAllIsRejected()
	{
		// It used to render as "int Value;" — a field, not a property.
		PropertyTemplate property = new() { Type = "int", Name = "Value" };

		InvalidOperationException exception =
			Assert.ThrowsExactly<InvalidOperationException>(() => Render(property));
		Assert.Contains("Value", exception.Message);
	}

	[TestMethod]
	public void ARejectedPropertyWritesNothing()
	{
		// The declaration used to be written before the check, so a caller that caught the
		// exception and carried on was left with "public int Foo" and no terminator.
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "Foo",
			Keywords = { "public" },
			Attributes = { "Obsolete" },
			Documentation = new DocComment { Summary = "Foo." },
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int a;");

		Assert.ThrowsExactly<InvalidOperationException>(() => property.WriteTo(codeBlocker));
		Assert.AreEqual("int a;\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void APropertyWithAnAccessorOfUnknownKindWritesNothing()
	{
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "P",
			Keywords = { "public" },
			Getter = AccessorTemplate.Auto(),
			Setter = new AccessorTemplate { Kind = (AccessorKind)99 },
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int a;");

		Assert.ThrowsExactly<InvalidOperationException>(() => property.WriteTo(codeBlocker));
		Assert.AreEqual("int a;\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void AnExpressionAccessorWithNoBodyIsRejectedBeforeAnythingIsWritten()
	{
		// It used to render "get => ;", which does not compile, and raise nothing.
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "P",
			Keywords = { "public" },
			Getter = new AccessorTemplate { Kind = AccessorKind.Expression },
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int a;");

		Assert.ThrowsExactly<InvalidOperationException>(() => property.WriteTo(codeBlocker));
		Assert.AreEqual("int a;\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void AnExpressionAccessorThatWritesNoExpressionIsRejectedBeforeAnythingIsWritten()
	{
		PropertyTemplate property = new()
		{
			Type = "int",
			Name = "P",
			Getter = AccessorTemplate.Expression(_ => { }),
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int a;");

		Assert.ThrowsExactly<InvalidOperationException>(() => property.WriteTo(codeBlocker));
		Assert.AreEqual("int a;\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void AStandaloneExpressionAccessorThatWritesNoExpressionWritesNothing()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int a;");

		Assert.ThrowsExactly<InvalidOperationException>(() => AccessorTemplate.Expression(_ => { }).WriteTo(codeBlocker, "get"));
		Assert.AreEqual("int a;\n", codeBlocker.ToString());
	}

	private static void WritePlaceholderComment(CodeBlocker codeBlocker) => codeBlocker.WriteLine("// placeholder");

	private static void WriteEmptyDebugConditional(CodeBlocker codeBlocker)
	{
		codeBlocker.WriteLine("#if DEBUG");
		codeBlocker.WriteLine("#endif");
	}

	[TestMethod]
	public void AnExpressionBodyOfOnlyACommentIsRejectedBeforeAnythingIsWritten()
	{
		// Used to render "int P => ;// placeholder" (CS1525).
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int a;");

		PropertyTemplate property = new() { Type = "int", Name = "P", ExpressionBodyFactory = WritePlaceholderComment };

		Assert.ThrowsExactly<InvalidOperationException>(() => property.WriteTo(codeBlocker));
		Assert.AreEqual("int a;\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void AnExpressionAccessorOfOnlyACommentIsRejectedBeforeAnythingIsWritten()
	{
		// Used to render "get => ;// placeholder" (CS1525).
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int a;");

		PropertyTemplate property = new() { Type = "int", Name = "P", Getter = AccessorTemplate.Expression(WritePlaceholderComment) };

		Assert.ThrowsExactly<InvalidOperationException>(() => property.WriteTo(codeBlocker));
		Assert.ThrowsExactly<InvalidOperationException>(() => AccessorTemplate.Expression(WritePlaceholderComment).WriteTo(codeBlocker, "get"));
		Assert.AreEqual("int a;\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void AnExpressionBodyOfOnlyDirectivesIsRejectedBeforeAnythingIsWritten()
	{
		// Used to render "int P =>", then the directives, then a lone ";" (CS1525).
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		PropertyTemplate property = new() { Type = "int", Name = "P", ExpressionBodyFactory = WriteEmptyDebugConditional };

		Assert.ThrowsExactly<InvalidOperationException>(() => property.WriteTo(codeBlocker));
		Assert.AreEqual(string.Empty, codeBlocker.ToString());
	}

	[TestMethod]
	public void AnExpressionBodyWithCodeAroundItsCommentsAndDirectivesIsStillWritten()
	{
		Assert.AreEqual("public int X => x; // c\n", RenderExpressionBody(codeBlocker => codeBlocker.Write("x // c")));
		Assert.AreEqual(
			"int P =>\n#if DEBUG\n1\n#else\n2\n#endif\n;\n",
			Render(new PropertyTemplate { Type = "int", Name = "P", ExpressionBodyFactory = WriteDebugConditional }));
	}

	#endregion

	#region MethodTemplate

	[TestMethod]
	public void AMethodWithNoBodyIsTerminatedWithASemicolon() =>
		Assert.AreEqual(
			"public abstract void Run();\n",
			Render(new MethodTemplate { Type = "void", Name = "Run", Keywords = { "public", "abstract" }, BodyFactory = null }));

	[TestMethod]
	public void AnExpressionBodyIsSeparatedFromTheParameterList() =>
		Assert.AreEqual(
			"public void Reset() => count = 0;\n",
			Render(new MethodTemplate
			{
				Type = "void",
				Name = "Reset",
				Keywords = { "public" },
				BodyFactory = codeBlocker => codeBlocker.Write("=> count = 0;"),
			}));

	[TestMethod]
	public void ABodyThatWritesNothingRendersAsAnEmptyBlock()
	{
		// Methods and constructors used to disagree here: the constructor emitted "{ }" and the
		// method emitted nothing at all, leaving a declaration with neither body nor semicolon.
		MethodTemplate method = new()
		{
			Type = "void",
			Name = "Run",
			Keywords = { "public", "virtual" },
			BodyFactory = _ => { },
		};

		Assert.AreEqual("public virtual void Run() { }\n", Render(method));
	}

	[TestMethod]
	[DataRow(1)]
	[DataRow(2)]
	public void ABodyThatWritesOnlyBlankLinesRendersAsAnEmptyBlock(int newLines)
	{
		// A NewLine after a loop over an empty collection used to leave "public void M() " with no
		// body, which does not compile (CS0501).
		MethodTemplate method = new()
		{
			Type = "void",
			Name = "M",
			Keywords = { "public" },
			BodyFactory = codeBlocker =>
			{
				for (int i = 0; i < newLines; i++)
				{
					codeBlocker.NewLine();
				}
			},
		};

		Assert.AreEqual("public void M() { }\n", Render(method));
	}

	[TestMethod]
	public void ABodyThatWritesOnlyALineCommentIsBraced() =>
		// "void M() // nothing to reset" has no body at all (CS1002).
		Assert.AreEqual(
			"void M()\n{\n\t// nothing to reset\n}\n",
			Render(new MethodTemplate
			{
				Type = "void",
				Name = "M",
				BodyFactory = codeBlocker => codeBlocker.WriteLine("// nothing to reset"),
			}));

	[TestMethod]
	public void ABodyThatWritesOnlyLineCommentsIsBraced() =>
		Assert.AreEqual(
			"void M()\n{\n\t// a\n\t// b\n}\n",
			Render(new MethodTemplate
			{
				Type = "void",
				Name = "M",
				BodyFactory = codeBlocker =>
				{
					codeBlocker.WriteLine("// a");
					codeBlocker.WriteLine("// b");
				},
			}));

	[TestMethod]
	public void AConstructorBodyThatWritesOnlyALineCommentIsBraced() =>
		Assert.AreEqual(
			"C()\n{\n\t// no state\n}\n",
			Render(new ConstructorTemplate
			{
				Name = "C",
				BodyFactory = codeBlocker => codeBlocker.WriteLine("// no state"),
			}));

	[TestMethod]
	public void ABodyThatWritesOnlyABlockCommentIsBraced() =>
		// "void M() /* nothing to do */" has no body at all (CS1002).
		Assert.AreEqual(
			"void M()\n{\n\t/* nothing to do */\n}\n",
			Render(new MethodTemplate
			{
				Type = "void",
				Name = "M",
				BodyFactory = codeBlocker => codeBlocker.WriteLine("/* nothing to do */"),
			}));

	[TestMethod]
	public void ABodyThatWritesOnlyAMultiLineBlockCommentIsBraced() =>
		Assert.AreEqual(
			"void M()\n{\n\t/* nothing\n\t   to do */\n}\n",
			Render(new MethodTemplate
			{
				Type = "void",
				Name = "M",
				BodyFactory = codeBlocker =>
				{
					codeBlocker.WriteLine("/* nothing");
					codeBlocker.WriteLine("   to do */");
				},
			}));

	[TestMethod]
	public void ABodyThatWritesOnlyBlockAndLineCommentsIsBraced() =>
		Assert.AreEqual(
			"C()\n{\n\t/* a */ // b\n}\n",
			Render(new ConstructorTemplate
			{
				Name = "C",
				BodyFactory = codeBlocker => codeBlocker.WriteLine("/* a */ // b"),
			}));

	[TestMethod]
	public void ABodyWithCodeAfterABlockCommentStaysOnTheDeclarationLine() =>
		Assert.AreEqual(
			"int M() /* note */ => 1;\n",
			Render(new MethodTemplate
			{
				Type = "int",
				Name = "M",
				BodyFactory = codeBlocker => codeBlocker.WriteLine("/* note */ => 1;"),
			}));

	[TestMethod]
	public void ABlockCommentOpenerInsideAStringIsCode() =>
		Assert.AreEqual(
			"string M() => \"/* not a comment */\";\n",
			Render(new MethodTemplate
			{
				Type = "string",
				Name = "M",
				BodyFactory = codeBlocker => codeBlocker.WriteLine("=> \"/* not a comment */\";"),
			}));

	[TestMethod]
	public void ABodyThatWritesOnlyAnEmptyPragmaScopeIsBraced() =>
		Assert.AreEqual(
			"void M()\n{\n\t#pragma warning disable CS0168\n\t#pragma warning restore CS0168\n}\n",
			Render(new MethodTemplate
			{
				Type = "void",
				Name = "M",
				BodyFactory = codeBlocker =>
				{
					using PragmaScope pragma = new(codeBlocker, "CS0168");
				},
			}));

	[TestMethod]
	[DataRow("=> x;")]
	[DataRow("{ }")]
	[DataRow("=> x; // trailing")]
	public void AOneLineBodyWithCodeStaysOnTheDeclarationLine(string body) =>
		Assert.AreEqual(
			$"int M() {body}\n",
			Render(new MethodTemplate
			{
				Type = "int",
				Name = "M",
				BodyFactory = codeBlocker => codeBlocker.WriteLine(body),
			}));

	[TestMethod]
	public void AnExpressionBodyBetweenBlankLinesStaysOnTheDeclarationLine() =>
		Assert.AreEqual(
			"public void Reset() => count = 0;\n",
			Render(new MethodTemplate
			{
				Type = "void",
				Name = "Reset",
				Keywords = { "public" },
				BodyFactory = codeBlocker =>
				{
					codeBlocker.NewLine();
					codeBlocker.WriteLine("=> count = 0;");
					codeBlocker.NewLine();
				},
			}));

	[TestMethod]
	public void ABlankLineInsideABracedBodyIsKept()
	{
		MethodTemplate method = new()
		{
			Type = "void",
			Name = "Run",
			Keywords = { "public" },
			BodyFactory = codeBlocker =>
			{
				using Scope scope = new(codeBlocker);
				codeBlocker.WriteLine("First();");
				codeBlocker.NewLine();
				codeBlocker.WriteLine("Second();");
			},
		};

		Assert.AreEqual("public void Run()\n{\n\tFirst();\n\n\tSecond();\n}\n", Render(method));
	}

	[TestMethod]
	public void ABracedBodyDropsTheBlankLinesAroundIt()
	{
		ClassTemplate type = new() { Name = "C" };
		type.Members.Add(new MethodTemplate
		{
			Type = "void",
			Name = "A",
			BodyFactory = codeBlocker =>
			{
				using (new Scope(codeBlocker))
				{
					codeBlocker.WriteLine("x();");
				}

				codeBlocker.NewLine();
			},
		});
		type.Members.Add(new MethodTemplate
		{
			Type = "void",
			Name = "B",
			BodyFactory = codeBlocker =>
			{
				codeBlocker.NewLine();
				codeBlocker.WriteLine("{");
				codeBlocker.WriteLine("}");
			},
		});

		// One blank line between the members, and each opening brace directly under its signature.
		Assert.AreEqual(
			"class C\n{\n\tvoid A()\n\t{\n\t\tx();\n\t}\n\n\tvoid B()\n\t{\n\t}\n}\n",
			Render(type));
	}

	[TestMethod]
	public void ABracedBodyKeepsItsInteriorBlankLinesWhenItsOuterOnesAreDropped() =>
		Assert.AreEqual(
			"void Run()\n{\n\tFirst();\n\n\tSecond();\n}\n",
			Render(new MethodTemplate
			{
				Type = "void",
				Name = "Run",
				BodyFactory = codeBlocker =>
				{
					codeBlocker.NewLine();
					codeBlocker.NewLine();
					using (new Scope(codeBlocker))
					{
						codeBlocker.WriteLine("First();");
						codeBlocker.NewLine();
						codeBlocker.WriteLine("Second();");
					}

					codeBlocker.NewLine();
					codeBlocker.NewLine();
				},
			}));

	[TestMethod]
	public void ACommentOnlyBodyDropsTheBlankLinesAroundIt() =>
		Assert.AreEqual(
			"void Reset()\n{\n\t// nothing to reset\n}\n",
			Render(new MethodTemplate
			{
				Type = "void",
				Name = "Reset",
				BodyFactory = codeBlocker =>
				{
					codeBlocker.NewLine();
					codeBlocker.WriteLine("// nothing to reset");
					codeBlocker.NewLine();
				},
			}));

	[TestMethod]
	public void AMultiLineBodyIsIndentedToWhereItIsSpliced()
	{
		MethodTemplate method = new()
		{
			Type = "int",
			Name = "Add",
			Keywords = { "public" },
			Parameters =
			{
				new ParameterTemplate { Type = "int", Name = "a" },
				new ParameterTemplate { Type = "int", Name = "b" },
			},
			BodyFactory = codeBlocker =>
			{
				using Scope scope = new(codeBlocker);
				codeBlocker.WriteLine("return a + b;");
			},
		};

		// Rendered two levels in, to prove every line of the body picks up the surrounding indent
		// rather than only the first.
		string output = Render(codeBlocker =>
		{
			codeBlocker.Indent();
			codeBlocker.Indent();
			method.WriteTo(codeBlocker);
		});

		Assert.AreEqual(
			"\t\tpublic int Add(int a, int b)\n\t\t{\n\t\t\treturn a + b;\n\t\t}\n",
			output);
	}

	[TestMethod]
	public void GenericConstraintsHangOffTheDeclarationAndCarryTheBody()
	{
		MethodTemplate method = new()
		{
			Type = "TResult",
			Name = "Map",
			Keywords = { "public" },
			TypeParameters = { "TResult" },
			Parameters = { new ParameterTemplate { Type = "Func<int, TResult>", Name = "selector" } },
			Constraints = { "where TResult : struct" },
			BodyFactory = codeBlocker => codeBlocker.Write("=> selector(0);"),
		};

		Assert.AreEqual(
			"""
			public TResult Map<TResult>(Func<int, TResult> selector)
				where TResult : struct => selector(0);

			""".ReplaceLineEndings("\n"),
			Render(method));
	}

	[TestMethod]
	public void ConstraintsOnAMethodWithNoBodyCarryTheSemicolon()
	{
		MethodTemplate method = new()
		{
			Type = "void",
			Name = "Run",
			Keywords = { "public", "abstract" },
			TypeParameters = { "T" },
			Constraints = { "where T : class", "new()" },
			BodyFactory = null,
		};

		Assert.AreEqual(
			"""
			public abstract void Run<T>()
				where T : class
				new();

			""".ReplaceLineEndings("\n"),
			Render(method));
	}

	#endregion

	#region ConstructorTemplate

	[TestMethod]
	public void AConstructorWithNoBaseCallIsAnEmptyBlock() =>
		Assert.AreEqual(
			"public Widget() { }\n",
			Render(new ConstructorTemplate { Name = "Widget", Keywords = { "public" } }));

	[TestMethod]
	[DataRow(1)]
	[DataRow(2)]
	public void AConstructorBodyThatWritesOnlyBlankLinesIsAnEmptyBlock(int newLines) =>
		Assert.AreEqual(
			"public Widget() { }\n",
			Render(new ConstructorTemplate
			{
				Name = "Widget",
				Keywords = { "public" },
				BodyFactory = codeBlocker =>
				{
					for (int i = 0; i < newLines; i++)
					{
						codeBlocker.NewLine();
					}
				},
			}));

	[TestMethod]
	public void AConstructorInitialiserIsIndentedOnItsOwnLine()
	{
		ConstructorTemplate constructor = new()
		{
			Name = "Widget",
			Keywords = { "public" },
			Parameters = { new ParameterTemplate { Type = "int", Name = "count" } },
			BaseParameters = { "count" },
		};

		Assert.AreEqual(
			"""
			public Widget(int count)
				: base(count) { }

			""".ReplaceLineEndings("\n"),
			Render(constructor));
	}

	[TestMethod]
	public void AConstructorCanChainToThis()
	{
		ConstructorTemplate constructor = new()
		{
			Name = "Widget",
			Keywords = { "public" },
			BaseParameters = { "0" },
			ChainsToThis = true,
		};

		Assert.AreEqual(
			"""
			public Widget()
				: this(0) { }

			""".ReplaceLineEndings("\n"),
			Render(constructor));
	}

	[TestMethod]
	public void AConstructorCanChainToThisWithNoArguments()
	{
		ConstructorTemplate constructor = new()
		{
			Name = "Widget",
			Keywords = { "public" },
			Parameters = { new ParameterTemplate { Type = "int", Name = "count" } },
			ChainsToThis = true,
		};

		Assert.AreEqual(
			"""
			public Widget(int count)
				: this() { }

			""".ReplaceLineEndings("\n"),
			Render(constructor));
	}

	#endregion

	#region OperatorTemplate

	[TestMethod]
	public void AnOperatorIsNamedByItsSymbol()
	{
		OperatorTemplate op = new()
		{
			Type = "Money",
			Keywords = { "public", "static" },
			Symbol = "+",
			Parameters =
			{
				new ParameterTemplate { Type = "Money", Name = "left" },
				new ParameterTemplate { Type = "Money", Name = "right" },
			},
			BodyFactory = codeBlocker => codeBlocker.Write("=> new(left.Amount + right.Amount);"),
		};

		Assert.AreEqual(
			"public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);\n",
			Render(op));
	}

	[TestMethod]
	public void AnOperatorOfUnknownKindWritesNothing()
	{
		OperatorTemplate unknown = new()
		{
			Kind = (OperatorKind)99,
			Type = "Money",
			Symbol = "+",
			Keywords = { "public", "static" },
			Attributes = { "Obsolete" },
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int a;");

		Assert.ThrowsExactly<InvalidOperationException>(() => unknown.WriteTo(codeBlocker));
		Assert.AreEqual("int a;\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void AClassOfUnknownKindWritesNothing()
	{
		ClassTemplate unknown = new()
		{
			Kind = (TypeKind)99,
			Name = "C",
			Keywords = { "public" },
			Attributes = { "Obsolete" },
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int a;");

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => unknown.WriteTo(codeBlocker));
		Assert.AreEqual("int a;\n", codeBlocker.ToString());
	}

	private static PropertyTemplate InvalidProperty() => new() { Name = "P", Type = "int" };

	[TestMethod]
	public void AClassWithAnInvalidMemberWritesNothing()
	{
		ClassTemplate type = new() { Name = "C" };
		type.Members.Add(new FieldTemplate { Name = "a", Type = "int" });
		type.Members.Add(InvalidProperty());
		type.Members.Add(new FieldTemplate { Name = "b", Type = "int", Keywords = { "private" } });

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		InvalidOperationException exception =
			Assert.ThrowsExactly<InvalidOperationException>(() => codeBlocker.AddClass(type));
		Assert.Contains("'P'", exception.Message);
		Assert.AreEqual(string.Empty, codeBlocker.ToString());
	}

	public static IEnumerable<object[]> PropertiesWhoseExpressionWritesNothing =>
	[
		["empty expression body", new PropertyTemplate { Type = "int", Name = "P", ExpressionBodyFactory = _ => { } }],
		["empty expression accessor", new PropertyTemplate { Type = "int", Name = "P", Getter = AccessorTemplate.Expression(_ => { }) }],
		["comment-only expression body", new PropertyTemplate { Type = "int", Name = "P", ExpressionBodyFactory = WritePlaceholderComment }],
		["comment-only expression accessor", new PropertyTemplate { Type = "int", Name = "P", Getter = AccessorTemplate.Expression(WritePlaceholderComment) }],
	];

	[TestMethod]
	[DynamicData(nameof(PropertiesWhoseExpressionWritesNothing))]
	public void AClassWithAPropertyWhoseExpressionWritesNothingWritesNothing(string description, PropertyTemplate property)
	{
		// The empty expression used to be found only when the property was written, inside the
		// class's body, so the class was closed without it and every member after it (#183).
		ClassTemplate type = new()
		{
			Name = "C",
			Members = { new FieldTemplate { Type = "int", Name = "f" }, property, new FieldTemplate { Type = "int", Name = "g" } },
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		Assert.ThrowsExactly<InvalidOperationException>(() => codeBlocker.AddClass(type), description);
		Assert.AreEqual(string.Empty, codeBlocker.ToString(), description);
	}

	[TestMethod]
	public void ASourceFileWithAnEmptyExpressionBodyWritesNothing()
	{
		SourceFileTemplate file = new()
		{
			Namespace = "Contoso",
			Classes =
			{
				new ClassTemplate { Name = "Good" },
				new ClassTemplate { Name = "Bad", Members = { new PropertyTemplate { Type = "int", Name = "P", ExpressionBodyFactory = _ => { } } } },
			},
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		Assert.ThrowsExactly<InvalidOperationException>(() => codeBlocker.AddSourceFile(file));
		Assert.AreEqual(string.Empty, codeBlocker.ToString());
	}

	[TestMethod]
	public void ANullMethodParameterIsRefusedBeforeTheClassIsWritten()
	{
		MethodTemplate method = new() { Name = "M", Type = "void", BodyFactory = _ => { } };
		method.Parameters.Add(null!);
		ClassTemplate type = new() { Name = "C", Members = { method } };

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		InvalidOperationException exception =
			Assert.ThrowsExactly<InvalidOperationException>(() => codeBlocker.AddClass(type));
		Assert.Contains("'M'", exception.Message);
		Assert.Contains("Parameters", exception.Message);
		Assert.AreEqual(string.Empty, codeBlocker.ToString());
	}

	[TestMethod]
	public void ANullPositionalParameterIsRefusedBeforeTheRecordIsWritten()
	{
		ClassTemplate record = new() { Name = "R", Kind = TypeKind.Record };
		record.PositionalParameters.Add(null!);

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		InvalidOperationException exception =
			Assert.ThrowsExactly<InvalidOperationException>(() => codeBlocker.AddClass(record));
		Assert.Contains("'R'", exception.Message);
		Assert.Contains("PositionalParameters", exception.Message);
		Assert.AreEqual(string.Empty, codeBlocker.ToString());
	}

	[TestMethod]
	public void ANullConstructorParameterIsRefusedBeforeTheClassIsWritten()
	{
		ConstructorTemplate constructor = new() { Name = "C" };
		constructor.Parameters.Add(null!);
		ClassTemplate type = new() { Name = "C", Members = { constructor } };

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		Assert.ThrowsExactly<InvalidOperationException>(() => codeBlocker.AddClass(type));
		Assert.AreEqual(string.Empty, codeBlocker.ToString());
	}

	[TestMethod]
	public void ANullOperatorParameterIsRefusedBeforeTheOperatorIsWritten()
	{
		OperatorTemplate op = new() { Type = "Money", Symbol = "-", Keywords = { "public", "static" } };
		op.Parameters.Add(null!);

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int a;");

		Assert.ThrowsExactly<InvalidOperationException>(() => op.WriteTo(codeBlocker));
		Assert.AreEqual("int a;\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void ANullMethodParameterIsRefusedWhenTheMethodIsWrittenOnItsOwn()
	{
		MethodTemplate method = new() { Name = "M", Type = "void", BodyFactory = _ => { } };
		method.Parameters.Add(null!);

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int a;");

		Assert.ThrowsExactly<InvalidOperationException>(() => method.WriteTo(codeBlocker));
		Assert.AreEqual("int a;\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void ANullMemberOrNestedClassIsRefusedWithTheTypeNamed()
	{
		ClassTemplate withNullMember = new() { Name = "A" };
		withNullMember.Members.Add(null!);
		ClassTemplate withNullNested = new() { Name = "B" };
		withNullNested.NestedClasses.Add(null!);

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		InvalidOperationException memberException =
			Assert.ThrowsExactly<InvalidOperationException>(() => codeBlocker.AddClass(withNullMember));
		Assert.Contains("'A'", memberException.Message);
		InvalidOperationException nestedException =
			Assert.ThrowsExactly<InvalidOperationException>(() => codeBlocker.AddClass(withNullNested));
		Assert.Contains("'B'", nestedException.Message);
		Assert.AreEqual(string.Empty, codeBlocker.ToString());
	}

	[TestMethod]
	public void AnInvalidMemberOfANestedClassLeavesTheOuterClassUnwritten()
	{
		ClassTemplate inner = new() { Name = "Inner", Members = { InvalidProperty() } };
		ClassTemplate outer = new()
		{
			Name = "Outer",
			Members = { new FieldTemplate { Name = "a", Type = "int" } },
			NestedClasses = { inner },
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int x;");

		Assert.ThrowsExactly<InvalidOperationException>(() => codeBlocker.AddClass(outer));
		Assert.AreEqual("int x;\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void ANestedClassOfUnknownKindLeavesTheOuterClassUnwritten()
	{
		ClassTemplate outer = new() { Name = "Outer", NestedClasses = { new ClassTemplate { Name = "Inner", Kind = (TypeKind)99 } } };

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => codeBlocker.AddClass(outer));
		Assert.AreEqual(string.Empty, codeBlocker.ToString());
	}

	[TestMethod]
	public void AnOperatorOfUnknownKindLeavesItsClassUnwritten()
	{
		ClassTemplate type = new()
		{
			Name = "C",
			Members = { new OperatorTemplate { Kind = (OperatorKind)99, Type = "C", Symbol = "+" } },
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		Assert.ThrowsExactly<InvalidOperationException>(() => codeBlocker.AddClass(type));
		Assert.AreEqual(string.Empty, codeBlocker.ToString());
	}

	[TestMethod]
	public void AClassWithValidMembersIsStillWritten() =>
		Assert.AreEqual(
			"class C\n{\n\tint a;\n\n\tint P { get; }\n}\n",
			Render(new ClassTemplate
			{
				Name = "C",
				Members =
				{
					new FieldTemplate { Name = "a", Type = "int" },
					new PropertyTemplate { Name = "P", Type = "int", Getter = AccessorTemplate.Auto() },
				},
			}));

	[TestMethod]
	public void AConversionIsNamedByItsResultType()
	{
		OperatorTemplate implicitConversion = new()
		{
			Kind = OperatorKind.Implicit,
			Type = "decimal",
			Keywords = { "public", "static" },
			Parameters = { new ParameterTemplate { Type = "Money", Name = "money" } },
			BodyFactory = codeBlocker => codeBlocker.Write("=> money.Amount;"),
		};

		OperatorTemplate explicitConversion = new()
		{
			Kind = OperatorKind.Explicit,
			Type = "Money",
			Keywords = { "public", "static" },
			Parameters = { new ParameterTemplate { Type = "decimal", Name = "amount" } },
			BodyFactory = codeBlocker => codeBlocker.Write("=> new(amount);"),
		};

		Assert.AreEqual(
			"public static implicit operator decimal(Money money) => money.Amount;\n",
			Render(implicitConversion));
		Assert.AreEqual(
			"public static explicit operator Money(decimal amount) => new(amount);\n",
			Render(explicitConversion));
	}

	#endregion

	#region ClassTemplate

	[TestMethod]
	public void TheKindSuppliesTheDeclarationKeyword()
	{
		static string RenderKind(TypeKind kind) =>
			Render(new ClassTemplate { Kind = kind, Name = "X", Keywords = { "public" } });

		Assert.AreEqual("public class X\n{\n}\n", RenderKind(TypeKind.Class));
		Assert.AreEqual("public struct X\n{\n}\n", RenderKind(TypeKind.Struct));
		Assert.AreEqual("public interface X\n{\n}\n", RenderKind(TypeKind.Interface));
		Assert.AreEqual("public record X\n{\n}\n", RenderKind(TypeKind.Record));
		Assert.AreEqual("public record struct X\n{\n}\n", RenderKind(TypeKind.RecordStruct));
		Assert.AreEqual("public enum X\n{\n}\n", RenderKind(TypeKind.Enum));
	}

	[TestMethod]
	public void ATypeBodyClosesWithoutASemicolon()
	{
		// Every type used to close with "};" — legal, but not what anyone writes by hand.
		string output = Render(new ClassTemplate
		{
			Name = "Widget",
			Keywords = { "public" },
			Members = { new FieldTemplate { Type = "int", Name = "count", Keywords = { "private" } } },
		});

		Assert.AreEqual(
			"""
			public class Widget
			{
				private int count;
			}

			""".ReplaceLineEndings("\n"),
			output);
	}

	[TestMethod]
	public void BaseTypeAndInterfacesShareOneClause()
	{
		ClassTemplate type = new()
		{
			Name = "Widget",
			Keywords = { "public" },
			BaseClass = "WidgetBase",
			Interfaces = { "IWidget", "IDisposable" },
		};

		Assert.AreEqual(
			"public class Widget : WidgetBase, IWidget, IDisposable\n{\n}\n",
			Render(type));
	}

	[TestMethod]
	public void MembersAreGroupedByKindButKeepTheirOrderWithinAKind()
	{
		ClassTemplate type = new()
		{
			Name = "Widget",
			Keywords = { "public" },
			Members =
			{
				new MethodTemplate { Type = "void", Name = "Second", Keywords = { "public" }, BodyFactory = _ => { } },
				new FieldTemplate { Type = "int", Name = "b", Keywords = { "private" } },
				new MethodTemplate { Type = "void", Name = "First", Keywords = { "public" }, BodyFactory = _ => { } },
				new FieldTemplate { Type = "int", Name = "a", Keywords = { "private" } },
			},
		};

		Assert.AreEqual(
			"""
			public class Widget
			{
				private int b;

				private int a;

				public void Second() { }

				public void First() { }
			}

			""".ReplaceLineEndings("\n"),
			Render(type));
	}

	[TestMethod]
	public void SortingCanBeTurnedOffToKeepDeclarationOrder()
	{
		ClassTemplate type = new()
		{
			Name = "Widget",
			Keywords = { "public" },
			SortMembers = false,
			Members =
			{
				new MethodTemplate { Type = "void", Name = "Run", Keywords = { "public" }, BodyFactory = _ => { } },
				new FieldTemplate { Type = "int", Name = "a", Keywords = { "private" } },
			},
		};

		Assert.AreEqual(
			"""
			public class Widget
			{
				public void Run() { }

				private int a;
			}

			""".ReplaceLineEndings("\n"),
			Render(type));
	}

	[TestMethod]
	public void EnumMembersAreListedWithoutBlankLinesBetweenThem()
	{
		ClassTemplate type = new()
		{
			Kind = TypeKind.Enum,
			Name = "Size",
			Keywords = { "public" },
			BaseClass = "byte",
			Members =
			{
				new EnumMemberTemplate { Name = "Small", DefaultValue = "1" },
				new EnumMemberTemplate { Name = "Large" },
			},
		};

		Assert.AreEqual(
			"""
			public enum Size : byte
			{
				Small = 1,
				Large,
			}

			""".ReplaceLineEndings("\n"),
			Render(type));
	}

	[TestMethod]
	public void APositionalRecordWithNoBodyIsDeclaredWithASemicolon()
	{
		ClassTemplate type = new()
		{
			Kind = TypeKind.RecordStruct,
			Name = "Pair",
			TypeParameters = { "T" },
			Keywords = { "public", "readonly" },
			PositionalParameters =
			{
				new ParameterTemplate { Type = "T", Name = "First" },
				new ParameterTemplate { Type = "T", Name = "Second" },
			},
		};

		Assert.AreEqual("public readonly record struct Pair<T>(T First, T Second);\n", Render(type));
	}

	[TestMethod]
	public void APositionalRecordWithMembersStillGetsABody()
	{
		ClassTemplate type = new()
		{
			Kind = TypeKind.Record,
			Name = "Pair",
			Keywords = { "public" },
			PositionalParameters = { new ParameterTemplate { Type = "int", Name = "First" } },
			Members = { new FieldTemplate { Type = "int", Name = "cached", Keywords = { "private" } } },
		};

		Assert.AreEqual(
			"""
			public record Pair(int First)
			{
				private int cached;
			}

			""".ReplaceLineEndings("\n"),
			Render(type));
	}

	[TestMethod]
	public void TypeConstraintsGoOnTheirOwnIndentedLines()
	{
		ClassTemplate type = new()
		{
			Name = "Repository",
			TypeParameters = { "T" },
			Keywords = { "public" },
			Constraints = { "where T : class, new()" },
		};

		Assert.AreEqual(
			"""
			public class Repository<T>
				where T : class, new()
			{
			}

			""".ReplaceLineEndings("\n"),
			Render(type));
	}

	[TestMethod]
	public void NestedTypesAreIndentedInsideTheirParent()
	{
		ClassTemplate type = new()
		{
			Name = "Outer",
			Keywords = { "public" },
			NestedClasses =
			{
				new ClassTemplate
				{
					Name = "Inner",
					Keywords = { "private" },
					Members = { new FieldTemplate { Type = "int", Name = "x", Keywords = { "private" } } },
				},
			},
		};

		Assert.AreEqual(
			"""
			public class Outer
			{
				private class Inner
				{
					private int x;
				}
			}

			""".ReplaceLineEndings("\n"),
			Render(type));
	}

	#endregion

	#region SourceFileTemplate

	[TestMethod]
	public void AnEmptySourceFileWritesNothing() =>
		Assert.AreEqual(string.Empty, Render(codeBlocker => codeBlocker.AddSourceFile(new SourceFileTemplate())));

	[TestMethod]
	public void ASourceFileSkipsBlankUsings()
	{
		SourceFileTemplate file = new()
		{
			Namespace = "Contoso",
			Usings = { "System", "", "System.Linq" },
		};

		Assert.AreEqual(
			"namespace Contoso;\n\nusing System;\nusing System.Linq;\n\n",
			Render(codeBlocker => codeBlocker.AddSourceFile(file)));
	}

	[TestMethod]
	public void ASourceFileWithAnInvalidClassWritesNothing()
	{
		SourceFileTemplate file = new()
		{
			Namespace = "Contoso",
			Usings = { "System" },
			Comments = { "// <auto-generated />" },
			Classes =
			{
				new ClassTemplate { Name = "Good" },
				new ClassTemplate { Name = "Bad", Members = { InvalidProperty() } },
			},
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		Assert.ThrowsExactly<InvalidOperationException>(() => codeBlocker.AddSourceFile(file));
		Assert.AreEqual(string.Empty, codeBlocker.ToString());
	}

	[TestMethod]
	public void AClassWithAMemberWhoseDocCommentCannotBeWrittenWritesNothing()
	{
		// It used to write "public class C { void A() { } }" up to B, then throw.
		MethodTemplate b = new() { Type = "void", Name = "B", BodyFactory = _ => { }, Documentation = new DocComment() };
		b.Documentation.Params.Add(new DocTag { Name = "a\nb", Text = "t" });
		ClassTemplate type = new()
		{
			Name = "C",
			Keywords = { "public" },
			Members =
			{
				new MethodTemplate { Type = "void", Name = "A", BodyFactory = _ => { } },
				b,
			},
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);
		codeBlocker.WriteLine("int x;");

		Assert.ThrowsExactly<ArgumentException>(() => codeBlocker.AddClass(type));
		Assert.AreEqual("int x;\n", codeBlocker.ToString());
	}

	[TestMethod]
	public void ASourceFileWhoseLaterClassHasADocCommentThatCannotBeWrittenWritesNothing()
	{
		ClassTemplate second = new() { Name = "Second", Documentation = new DocComment { Summary = "s" } };
		second.Documentation.SeeAlso.Add("A\nB");
		SourceFileTemplate file = new()
		{
			Namespace = "Contoso",
			Usings = { "System" },
			Classes = { new ClassTemplate { Name = "First" }, second },
		};

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		Assert.ThrowsExactly<ArgumentException>(() => codeBlocker.AddSourceFile(file));
		Assert.AreEqual(string.Empty, codeBlocker.ToString());
	}

	[TestMethod]
	public void ANestedClassWhoseDocCommentCannotBeWrittenLeavesTheOuterClassUnwritten()
	{
		ClassTemplate inner = new() { Name = "Inner", Documentation = new DocComment { InheritDoc = true, InheritDocCref = "X\r\nY" } };
		ClassTemplate outer = new() { Name = "Outer", NestedClasses = { inner } };

		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		Assert.ThrowsExactly<ArgumentException>(() => codeBlocker.AddClass(outer));
		Assert.AreEqual(string.Empty, codeBlocker.ToString());
	}

	[TestMethod]
	public void AddClassesWithAnInvalidClassWritesNothing()
	{
		using CodeBlocker codeBlocker = CodeBlocker.Create(CodeBlocker.DefaultIndentString, NewLines.Lf);

		Assert.ThrowsExactly<InvalidOperationException>(() => codeBlocker.AddClasses(
			[new ClassTemplate { Name = "Good" }, new ClassTemplate { Name = "Bad", Members = { InvalidProperty() } }]));
		Assert.AreEqual(string.Empty, codeBlocker.ToString());
	}

	[TestMethod]
	public void ASourceFileWritesItsPreambleThenItsTypes()
	{
		SourceFileTemplate file = new()
		{
			FileName = "Widget.g.cs",
			Namespace = "Contoso",
			Usings = { "System" },
			Comments = { "// <auto-generated />" },
			Classes = { new ClassTemplate { Name = "Widget", Keywords = { "public" } } },
		};

		Assert.AreEqual(
			"// <auto-generated />\nnamespace Contoso;\n\nusing System;\n\npublic class Widget\n{\n}\n\n",
			Render(codeBlocker => codeBlocker.AddSourceFile(file)));
	}

	[TestMethod]
	public void ASourceFileWritesItsGlobalAttributesAfterItsUsingsAndBeforeItsNamespace()
	{
		SourceFileTemplate file = new()
		{
			Namespace = "Contoso",
			Usings = { "System" },
			Attributes = { "assembly: CLSCompliant(true)" },
			Classes = { new ClassTemplate { Name = "Widget", Keywords = { "public" } } },
		};

		Assert.AreEqual(
			"using System;\n\n[assembly: CLSCompliant(true)]\n\nnamespace Contoso;\n\npublic class Widget\n{\n}\n\n",
			Render(codeBlocker => codeBlocker.AddSourceFile(file)));
	}

	[TestMethod]
	public void ASourceFileWithNoNamespaceWritesItsGlobalAttributesAfterItsUsings()
	{
		SourceFileTemplate file = new()
		{
			Usings = { "System" },
			Attributes = { "assembly: CLSCompliant(true)" },
			Classes = { new ClassTemplate { Name = "Widget", Keywords = { "public" } } },
		};

		Assert.AreEqual(
			"using System;\n\n[assembly: CLSCompliant(true)]\n\npublic class Widget\n{\n}\n\n",
			Render(codeBlocker => codeBlocker.AddSourceFile(file)));
	}

	#endregion

	#region Null handling

	[TestMethod]
	public void EveryTemplateRejectsANullCodeBlocker()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => new ParameterTemplate().WriteTo(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => new FieldTemplate().WriteTo(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => new EnumMemberTemplate().WriteTo(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => new PropertyTemplate().WriteTo(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => new MethodTemplate().WriteTo(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => new ConstructorTemplate().WriteTo(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => new OperatorTemplate().WriteTo(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => new ClassTemplate().WriteTo(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => new AccessorTemplate().WriteTo(null!, "get"));
	}

	#endregion

	#region Verbatim string literals in bodies

	private static string RenderInClass(MemberTemplate member)
	{
		ClassTemplate type = new() { Name = "C", Members = { member } };
		return Render(type);
	}

	[TestMethod]
	public void AMethodBodyKeepsAVerbatimStringsContinuationLinesAsWritten()
	{
		MethodTemplate method = new()
		{
			Name = "M",
			Type = "string",
			BodyFactory = b =>
			{
				b.WriteLine("{");
				b.Indent();
				b.WriteLine("return @\"line1\nline2\";");
				b.Outdent();
				b.WriteLine("}");
			},
		};

		Assert.AreEqual(
			"class C\n{\n\tstring M()\n\t{\n\t\treturn @\"line1\nline2\";\n\t}\n}\n",
			RenderInClass(method));
	}

	[TestMethod]
	public void AMethodBodyKeepsAVerbatimStringsContinuationLinesAsWrittenAfterASlashPairInAFormatSpecifier()
	{
		MethodTemplate method = new()
		{
			Name = "M",
			Type = "string",
			BodyFactory = b =>
			{
				b.WriteLine("{");
				b.Indent();
				b.WriteLine("return $@\"{d:yyyy//MM}\nline2\";");
				b.Outdent();
				b.WriteLine("}");
			},
		};

		Assert.AreEqual(
			"class C\n{\n\tstring M()\n\t{\n\t\treturn $@\"{d:yyyy//MM}\nline2\";\n\t}\n}\n",
			RenderInClass(method));
	}

	[TestMethod]
	public void ABlockAccessorKeepsAnInterpolatedVerbatimStringsContinuationLinesAsWritten()
	{
		PropertyTemplate property = new()
		{
			Type = "string",
			Name = "P",
			Getter = AccessorTemplate.Block(b => b.WriteLine("return $@\"{name}\n  \"\"{name}\"\"\n{{done}}\";")),
		};

		Assert.AreEqual(
			"class C\n{\n\tstring P\n\t{\n\t\tget\n\t\t{\n\t\t\treturn $@\"{name}\n  \"\"{name}\"\"\n{{done}}\";\n\t\t}\n\t}\n}\n",
			RenderInClass(property));
	}

	[TestMethod]
	public void AnExpressionBodyKeepsAVerbatimStringsContinuationLinesAsWritten()
	{
		PropertyTemplate property = new()
		{
			Type = "string",
			Name = "P",
			ExpressionBodyFactory = b =>
			{
				b.WriteLine("Join(");
				b.Indent();
				b.WriteLine("@$\"a{Format(\"}\", @\"x\ny\")}\nb\",");
				b.WriteLine("c)");
				b.Outdent();
			},
		};

		Assert.AreEqual(
			"class C\n{\n\tstring P => Join(\n\t\t@$\"a{Format(\"}\", @\"x\ny\")}\nb\",\n\t\tc);\n}\n",
			RenderInClass(property));
	}

	[TestMethod]
	public void QuotesInCommentsCharactersAndRegularStringsDoNotStartAVerbatimString()
	{
		MethodTemplate method = new()
		{
			Name = "M",
			Type = "void",
			BodyFactory = b =>
			{
				b.WriteLine("{");
				b.Indent();
				b.WriteLine("// say @\"");
				b.WriteLine("/* or @\" */ char q = '\"';");
				b.WriteLine("string s = \"@\\\"\" + @\"\";");
				b.WriteLine("Call();");
				b.Outdent();
				b.WriteLine("}");
			},
		};

		Assert.AreEqual(
			"class C\n{\n\tvoid M()\n\t{\n\t\t// say @\"\n\t\t/* or @\" */ char q = '\"';\n\t\tstring s = \"@\\\"\" + @\"\";\n\t\tCall();\n\t}\n}\n",
			RenderInClass(method));
	}

	[TestMethod]
	public void ARawStringLiteralIsStillReindented()
	{
		MethodTemplate method = new()
		{
			Name = "M",
			Type = "string",
			BodyFactory = b =>
			{
				b.WriteLine("{");
				b.Indent();
				b.WriteLine("return \"\"\"");
				b.WriteLine("\"@ text");
				b.WriteLine("\"\"\";");
				b.Outdent();
				b.WriteLine("}");
			},
		};

		Assert.AreEqual(
			"class C\n{\n\tstring M()\n\t{\n\t\treturn \"\"\"\n\t\t\"@ text\n\t\t\"\"\";\n\t}\n}\n",
			RenderInClass(method));
	}

	#endregion
}

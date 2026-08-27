// Copyright (c) 2023-2026 ktsu-dev contributors

namespace CodeBlocker.Tests;

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
}

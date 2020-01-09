using System;
using System.Collections.Generic;
using static LangProj.Parsers;

namespace LangProj
{
    class Program
    {
        static void Main(string[] args)
        {
            IType
                Int = TypeSingleton.Integer,
                Bool = TypeSingleton.Boolean,
                Str = TypeSingleton.String,
                Func = new TypeFunction(new TypeTuple(), TypeSingleton.Void),
                Tuple = new TypeTuple(),
                Union = new TypeUnion(),
                Void = TypeSingleton.Void,
                Any = TypeSingleton.Any;

            Tokenizer
                Indentation = Tokenizers.Indentation,
                MathOperator = Tokenizers.Predicate("&|+-".Contains),
                MathOperator2 = Tokenizers.Predicate("*/%".Contains),
                ComparisonOperator = Tokenizers.Predicate("=<>".Contains),
                PrefixOperator = Tokenizers.Match("!"),
                String = Tokenizers.Quote('"', '"'),
                Integer = Tokenizers.Predicate(char.IsDigit),
                Symbol = Tokenizers.Predicate(char.IsLetterOrDigit);
            
            var compiler = new CompilerHelper(Indentation, MathOperator, MathOperator2, PrefixOperator, ComparisonOperator, String, Integer, Symbol);
            Func<string, Token> GetToken = compiler.GetToken;
            
            var SymbolParser = Tokens(Symbol);

            Parser<TypeName> TypeName;
            Parser<IEnumerable<TypeName>> TypeNames = null;
            Parser<(string, TypeName)> Variable;
            Parser<IEnumerable<(string, TypeName)>> Variables;

            TypeName = parser => new TypeName(SymbolParser.Invoke(parser), TypeNames(parser));
            TypeNames = ListOf(GetToken("{"), TypeName, GetToken(","), GetToken("}"));
            Variable = parser => (SymbolParser.Invoke(parser), TypeName(parser));
            Variables = ListOf(GetToken("["), Variable, GetToken(","), GetToken("]"));

            var expr = new ExprBuilders(new ExprWriters());
            
            // Parser to Analyzer mappings
            compiler.ParserRules.SetRules(
                ParserRules.Rule(expr.CheckedCast, Prefix(GetToken("->"), TypeName), Expression),
                ParserRules.Rule(expr.Sequence, Sequence(Indentation, Expression)),
                ParserRules.Rule(expr.Function, Variables, Expression),
                ParserRules.Rule(expr.Function, ListOf(GetToken("{"), Variable, GetToken(","), GetToken("}")), Variables, Expression),
                ParserRules.Rule(expr.VariableAssign, Prefix(GetToken("let"), Suffix(SymbolParser, GetToken("="))), Expression),
                ParserRules.Rule(expr.Identity, Prefix(GetToken("("), Suffix(Expression, GetToken(")")))),
                ParserRules.Rule(expr.LiteralString, Tokens(String)),
                ParserRules.Rule(expr.LiteralInt, Tokens(Integer)),
                ParserRules.Rule(expr.LiteralBooleanTrue, GetToken("true")),
                ParserRules.Rule(expr.LiteralBooleanFalse, GetToken("false")),
                ParserRules.Rule(expr.If, Prefix(GetToken("if"), Expression), Expression, Prefix(GetToken("else"), Expression)),
                ParserRules.Rule(expr.This, GetToken("this")),
                ParserRules.Rule(expr.FunctionOperatorSingle, Tokens(PrefixOperator), Expression),
                ParserRules.Rule(expr.Variable, SymbolParser)
            );
            compiler.ParserRules.SetInfixRules(
                ParserRules.Rule(expr.ContextVariable, weight: 6, Prefix(GetToken("."), SymbolParser)),
                ParserRules.Rule(expr.FunctionSetGenerics, weight: 5, TypeNames),
                ParserRules.Rule(expr.FunctionOperator, weight: 4, Tokens(MathOperator2), WeightedExpression(4)),
                ParserRules.Rule(expr.FunctionOperator, weight: 3, Tokens(MathOperator), WeightedExpression(3)),
                ParserRules.Rule(expr.FunctionOperator, weight: 2, Tokens(ComparisonOperator), WeightedExpression(2)),
                ParserRules.Rule(expr.BooleanAnd, weight: 1, Prefix(GetToken("and"), WeightedExpression(1))),
                ParserRules.Rule(expr.BooleanOr, weight: 1, Prefix(GetToken("or"), WeightedExpression(1))),
                ParserRules.Rule(expr.FunctionCall, weight: 6, ListOf(GetToken("("), Expression, GetToken(","), GetToken(")")))
            );

            compiler.AddType("int", Int);
            compiler.AddType("bool", Bool);
            compiler.AddType("string", Str);
            compiler.AddType("fun", Func);
            compiler.AddType("tuple", Tuple);
            compiler.AddType("union", Union);
            compiler.AddType("any", Any);
            compiler.AddType("void", Void);

            // Set of core functions.
            // TODO Optimize: compiler should 'unwrap' all short functions
            compiler.AddFunction("print", VMCommand.Print, Void, Any);
            compiler.AddFunction("<", VMCommand.CompareLessThan, Bool, Int, Int);
            compiler.AddFunction("+", VMCommand.MathAddition, Int, Int, Int);
            compiler.AddFunction("-", VMCommand.MathSubstraction, Int, Int, Int);
            compiler.AddFunction("*", VMCommand.MathMultiplication, Int, Int, Int);
            compiler.AddFunction("/", VMCommand.MathDivision, Int, Int, Int);
            compiler.AddFunction("!", VMCommand.BooleanNot, Bool, Bool);
            compiler.AddFunction("==", VMCommand.EqualInt, Bool, Bool, Bool);
            compiler.AddFunction("==", VMCommand.EqualInt, Bool, Int, Int);

            try
            {
                compiler.RunFile("Example");
            }
            catch (CompilationException e)
            {
                Console.WriteLine("Compilation Error: " + e.Message);
            }
        }
    }
}

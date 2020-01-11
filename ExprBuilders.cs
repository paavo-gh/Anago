using System;
using System.Collections.Generic;
using System.Linq;

namespace LangProj
{
    /// <summary>
    /// Converts parsed content into expression.
    /// </summary>
    public delegate Expr ExprBuilder(AnalyzerContext context);
    public delegate Expr ExprBuilder<A>(AnalyzerContext context, A a);
    public delegate Expr ExprBuilder<A, B>(AnalyzerContext context, A a, B b);
    public delegate Expr ExprBuilder<A, B, C>(AnalyzerContext context, A a, B b, C c);

    public class ExprBuilders
    {
        ExprWriters exprWriter;

        public ExprBuilders(ExprWriters exprWriter)
        {
            this.exprWriter = exprWriter;
        }

        public Expr Sequence(AnalyzerContext analyzer, IEnumerable<ExprBuilderObj> exprs)
        {
            var exprList = exprs.Select(expr => expr.Build(analyzer)).ToArray();
            return new ExprImpl(exprList.Last().Type, exprWriter.Sequence(exprList));
        }

        public Expr CheckedCast(AnalyzerContext analyzer, TypeName type, ExprBuilderObj expr)
        {
            var typeCast = type.Resolve(analyzer.Context);
            return new ExprImpl(typeCast, analyzer.Analyze(expr, typeCast));
        }

        public Expr Variable(AnalyzerContext analyzer, string varName)
        {
            var variable = analyzer.Context.GetVariableAssignableTo(varName, analyzer.ExpectedType ?? TypeSingleton.Any);
            if (variable == null)
                analyzer.Fail("No such variable: " + varName + " " + analyzer.ExpectedType);
            if (variable.ConstantValue != null)
                return variable.ConstantValue;
            return new ExprImpl(variable.Type, exprWriter.ReadVariable(analyzer.Context.GetReferenceIndex(variable)));
        }

        public Expr ContextVariable(AnalyzerContext analyzer, ExprBuilderObj leftExpr, string varName)
        {
            var type = new TypeContext(new Variable(varName, analyzer.ExpectedType ?? TypeSingleton.Any));
            var left = analyzer.Analyze(leftExpr, type);
            var contextType = left.Type.ConcreteType as TypeContext;
            var variable = contextType.Variables.GetAssignableTo(varName, analyzer.ExpectedType);
            if (variable == null)
                analyzer.Fail("Unknown context variable name: " + varName);
            var variable2 = contextType.Variables.GetAssignableTo(varName, analyzer.ExpectedType);

            var refIndex = contextType.Variables.GetReferenceIndex(variable) + 1; // TODO +1 is for the base context
            var expr = new ExprImpl(variable.Type, exprWriter.ReadContextVariable(refIndex));
            return new ExprImpl(variable.Type, exprWriter.Sequence(left, expr));
        }

        public Expr VariableAssign(AnalyzerContext analyzer, string variableName, ExprBuilderObj rightExpr)
        {
            var right = analyzer.Analyze(rightExpr); //new ExprLazy(() => analyzer.Analyze(rightExpr));
            var variable = new Variable(variableName, () => right.Type, right.IsConstant ? right : null);
            analyzer.Context.AddVariable(variable);

            // In case of a function: New type is defined mapping the given variable name 
            // to the return type of the function.
            // TODO For testing?
            analyzer.Context.AddType(variable.Name, () => {
                var funType = variable.Type.ConcreteType as TypeFunction;
                if (funType == null)
                    throw new CompilationException("Not a type: " + variable.Name);
                // Generic types
                var genType = variable.Type as TypeGenericContext;
                if (genType != null)
                    return new TypeGenericContext(funType.ReturnType, genType.GenericParameters);
                return funType.ReturnType;
            });
            //Console.WriteLine("Type defined: " + variable.Name);

            return new ExprImpl(TypeSingleton.Void, exprWriter.WriteVariable(analyzer.Context.GetReferenceIndex(variable), right));
        }

        public Expr LiteralString(AnalyzerContext analyzer, string val)
            => new ExprImpl(TypeSingleton.String, exprWriter.StringLiteral(val), constant: true);

        public Expr LiteralInt(AnalyzerContext analyzer, string val)
            => new ExprImpl(TypeSingleton.Integer, exprWriter.IntLiteral(int.Parse(val)), constant: true);

        public Expr LiteralBooleanTrue(AnalyzerContext analyzer)
            => new ExprImpl(TypeSingleton.Boolean, exprWriter.BoolLiteralTrue, constant: true);

        public Expr LiteralBooleanFalse(AnalyzerContext analyzer)
            => new ExprImpl(TypeSingleton.Boolean, exprWriter.BoolLiteralFalse, constant: true);

        public Expr Function(AnalyzerContext analyzer, IEnumerable<(string, TypeName)> valueArgs, ExprBuilderObj bodyExpr)
            => Function(analyzer, null, valueArgs, bodyExpr);

        // TODO Make single argument and use analyzer.ExpectedType
        public Expr Function(AnalyzerContext analyzer, IEnumerable<(string, TypeName)> typeArgs, IEnumerable<(string, TypeName)> valueArgs, ExprBuilderObj bodyExpr)
        {
            return new ExprLazy(() => {

                analyzer = analyzer.SubContext();
                var context = analyzer.Context;

                var args = valueArgs.Select(t => new Variable(t.Item1, t.Item2.Resolve(context)));
                if (typeArgs != null)
                {
                    var genericTypes = new List<TypeGeneric>();
                    foreach (var arg in typeArgs.Select(t => (t.Item1, t.Item2.Resolve(context))))
                    {
                        var genType = new TypeGeneric(arg.Item2);
                        genericTypes.Add(genType);
                        context.AddType(arg.Item1, genType);
                    }

                    foreach (var arg in args)
                        context.AddVariable(arg);
                    var body = analyzer.Analyze(bodyExpr);
                    var argType = new TypeTuple(args.Select(v => v.Type));
                    var type = new TypeFunction(argType, body.Type);
                    return new ExprImpl(
                        new TypeGenericContext(type, genericTypes),
                        new ExprImpl(type, exprWriter.Function(argType, body))
                    );
                }
                else
                {
                    foreach (var arg in args)
                        context.AddVariable(arg);
                    var body = analyzer.Analyze(bodyExpr);
                    var argType = new TypeTuple(args.Select(v => v.Type));
                    var type = new TypeFunction(argType, body.Type);
                    return new ExprImpl(type, exprWriter.Function(argType, body));
                }
            });
        }

        public Expr FunctionSetGenerics(AnalyzerContext analyzer, ExprBuilderObj exprLeft, IEnumerable<TypeName> args)
        {
            var left = analyzer.Analyze(exprLeft);
            var dict = new Dictionary<IType, IType>();
            var from = left.Type.GenericParameters.GetEnumerator();
            var to = args.GetEnumerator();
            while (from.MoveNext())
            {
                if (to.MoveNext())
                    dict[from.Current] = to.Current.Resolve(analyzer.Context);
                else
                    analyzer.Fail("Too many generic arguments", exprLeft);
            }
            if (to.MoveNext())
                analyzer.Fail("Too few generic arguments", exprLeft);
            
            return new ExprImpl(left.Type.ConvertGeneric(dict.GetValueOrDefault), left);
        }

        public Expr FunctionCall(AnalyzerContext analyzer, ExprBuilderObj leftExpr, IEnumerable<ExprBuilderObj> args)
        {   
            // When function type is known, pass expected type for the parameters
/*
            var left = analyzer.Analyze(leftExpr);
            var argList = new List<Expr>();
            var functionType = left.Type.ConcreteType as TypeFunction;
            var argType = functionType.ArgType as TypeTuple;
            var iterator = argType.Types.GetEnumerator();
            foreach (var arg in args)
            {
                if (iterator.MoveNext())
                    argList.Add(analyzer.Analyze(arg, iterator.Current));
            }
*/
            return FunctionCall(analyzer, args, expectedType => analyzer.Analyze(leftExpr, expectedType));
        }

        public Expr FunctionOperator(AnalyzerContext analyzer, ExprBuilderObj exprLeft, string op, ExprBuilderObj exprRight)
            => FunctionCallByName(analyzer, op, exprLeft, exprRight);

        public Expr FunctionOperatorSingle(AnalyzerContext analyzer, string op, ExprBuilderObj exprRight)
            => FunctionCallByName(analyzer, op, exprRight);

        Expr FunctionCallByName(AnalyzerContext analyzer, string op, params ExprBuilderObj[] args)
            => FunctionCall(analyzer, args, expectedType => Variable(analyzer.WithExpectedType(expectedType), op));

        Expr FunctionCall(AnalyzerContext analyzer, IEnumerable<ExprBuilderObj> args, Func<IType, Expr> functionResolver)
        {
            // Parameter overloading
            var argList = new List<Expr>();
            foreach (var arg in args)
            {
                argList.Add(analyzer.Analyze(arg));
            }
            var returnType = analyzer.ExpectedType ?? TypeSingleton.Any;
            var expectedType = new TypeFunction(new TypeTuple(argList.Select(arg => arg.Type)), returnType);

            var left = functionResolver(expectedType);
            
            // Optimize: if (expr.IsConstant && argList.TrueForAll(arg => arg.IsConstant && function is pure))
            return new ExprImpl((left.Type.ConcreteType as TypeFunction).ReturnType, exprWriter.FunctionCall(left, argList));
        }

        public Expr If(AnalyzerContext analyzer, ExprBuilderObj cond, ExprBuilderObj trueClause, ExprBuilderObj falseClause)
        {
            var condition = analyzer.Analyze(cond, TypeSingleton.Boolean);
            var ifTrue = analyzer.Analyze(trueClause);
            var ifFalse = falseClause != null ? analyzer.Analyze(falseClause) : new ExprImpl(TypeSingleton.Void, exprWriter.VoidLiteral);
            return new ExprImpl(
                TypeUnion.Create(ifTrue.Type, ifFalse.Type),
                exprWriter.If(condition, ifTrue, ifFalse));
        }

        public Expr This(AnalyzerContext analyzer)
            => new ExprImpl(analyzer.Context.Type, exprWriter.This);

        public Expr Identity(AnalyzerContext analyzer, ExprBuilderObj expr)
            => analyzer.Analyze(expr);
        
        public Expr BooleanAnd(AnalyzerContext analyzer, ExprBuilderObj exprLeft, ExprBuilderObj exprRight)
            => new ExprImpl(
                TypeSingleton.Boolean,
                exprWriter.And(
                    analyzer.Analyze(exprLeft, TypeSingleton.Boolean), 
                    analyzer.Analyze(exprRight, TypeSingleton.Boolean)));

        public Expr BooleanOr(AnalyzerContext analyzer, ExprBuilderObj exprLeft, ExprBuilderObj exprRight)
            => new ExprImpl(
                TypeSingleton.Boolean,
                exprWriter.Or(
                    analyzer.Analyze(exprLeft, TypeSingleton.Boolean), 
                    analyzer.Analyze(exprRight, TypeSingleton.Boolean)));
    }
}
namespace LangProj
{
    public class AnalyzerContext
    {
        public Context Context { get; private set; }
        public IType ExpectedType { get; private set; }

        public AnalyzerContext()
        {
            this.Context = new Context(parent: null);
        }

        private AnalyzerContext(Context context)
        {
            this.Context = context;
        }

        public Expr Analyze(ExprBuilderObj builder) => Analyze(builder, null);

        public Expr Analyze(ExprBuilderObj builder, IType expectedType)
        {
            if (ExpectedType == expectedType)
                return builder.Build(this);
            var expr = builder.Build(WithExpectedType(expectedType));
            if (expectedType != null && !expectedType.AssignableFrom(expr.Type))
                Fail("Unexpected type: " + expr.Type + ", expected: " + expectedType, builder);
            return expr;
        }

        public void Fail(string msg)
            => throw new CompilationException(msg);

        public void Fail(string msg, ExprBuilderObj expr)
            => throw new CompilationException($"{msg} at ({expr.FirstToken.Row},{expr.FirstToken.Column}): {expr.FirstToken.Content}");

        public AnalyzerContext SubContext()
            => new AnalyzerContext(new Context(Context));

        public AnalyzerContext WithExpectedType(IType expectedType)
            => new AnalyzerContext(Context) { ExpectedType = expectedType };
    }
}
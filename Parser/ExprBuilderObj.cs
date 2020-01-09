namespace LangProj
{
    public class ExprBuilderObj
    {
        ExprBuilder builder;

        public ExprBuilderObj(ExprBuilder builder, Token firstToken)
        {
            this.builder = builder;
            this.FirstToken = firstToken;
        }

        public Token FirstToken { get; private set; }

        public Expr Build(AnalyzerContext contextAnalyzer) => builder(contextAnalyzer);
    }
}
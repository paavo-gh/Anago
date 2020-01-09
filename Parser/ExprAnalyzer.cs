using System;

namespace LangProj
{
    public delegate ExprBuilder ExprAnalyzer(ParserContext parser);

    public class ExprInfixAnalyzer
    {
        public readonly int Weight;
        public readonly Func<ParserContext, ExprBuilderObj, ExprBuilder> Analyze;

        public ExprInfixAnalyzer(Func<ParserContext, ExprBuilderObj, ExprBuilder> analyze, int weight)
        {
            this.Analyze = analyze;
            this.Weight = weight;
        }
    }
}
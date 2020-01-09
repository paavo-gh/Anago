using System.Collections.Generic;

namespace LangProj
{
    public class ParserRules
    {
        ExprAnalyzer[] analyzers;
        ExprInfixAnalyzer[] infixAnalyzers;

        public IEnumerable<ExprAnalyzer> Analyzers => analyzers;
        public IEnumerable<ExprInfixAnalyzer> InfixAnalyzers => infixAnalyzers;

        public void SetRules(params ExprAnalyzer[] rules) => this.analyzers = rules;

        public void SetInfixRules(params ExprInfixAnalyzer[] rules) => this.infixAnalyzers = rules;

        public static ExprInfixAnalyzer Rule<A>(ExprBuilder<ExprBuilderObj, A> builder, int weight, Parser<A> a)
        {
            return new ExprInfixAnalyzer((parser, left) => {
                var aVal = a(parser);
                if (aVal == null)
                    return null;
                return analyzer => builder(analyzer, left, aVal);
            }, weight);
        }

        public static ExprInfixAnalyzer Rule<A, B>(ExprBuilder<ExprBuilderObj, A, B> builder, int weight, Parser<A> a, Parser<B> b)
        {
            return new ExprInfixAnalyzer((parser, left) => {
                var aVal = a(parser);
                if (aVal == null)
                    return null;
                var bVal = b(parser);
                return analyzer => builder(analyzer, left, aVal, bVal);
            }, weight);
        }

        public static ExprAnalyzer Rule(ExprBuilder builder, Token token)
        {
            return parser => parser.TryConsume(token) ? builder : null;
        }

        public static ExprAnalyzer Rule<A>(ExprBuilder<A> builder, Parser<A> a)
        {
            return parser => {
                var aVal = a(parser);
                if (aVal == null)
                    return null;
                return analyzer => builder(analyzer, aVal);
            };
        }

        public static ExprAnalyzer Rule<A, B>(ExprBuilder<A, B> builder, Parser<A> a, Parser<B> b)
        {
            return parser => {
                var aVal = a(parser);
                if (aVal == null)
                    return null;
                var bVal = b(parser);
                return analyzer => builder(analyzer, aVal, bVal);
            };
        }

        public static ExprAnalyzer Rule<A, B, C>(ExprBuilder<A, B, C> builder, Parser<A> a, Parser<B> b, Parser<C> c)
        {
            return parser => {
                var aVal = a(parser);
                if (aVal == null)
                    return null;
                var bVal = b(parser);
                var cVal = c(parser);
                return analyzer => builder(analyzer, aVal, bVal, cVal);
            };
        }
    }
}
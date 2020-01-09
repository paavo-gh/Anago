namespace LangProj
{
    public class ParserContext
    {
        ParserRules rules;
        TokenizerContext reader;

        public ParserContext(ParserRules rules, TokenizerContext reader)
        {
            this.rules = rules;
            this.reader = reader;

            reader.Next();
        }

        public ExprBuilderObj Parse(int weight = 0)
        {
            if (reader.Current == null)
                Fail("Unexpected end of file");

            return ParseInfix(ParsePrefix(), weight);
        }

        ExprBuilderObj ParsePrefix()
        {
            var firstToken = reader.Current;
            foreach (var analyzer in rules.Analyzers)
            {
                var result = analyzer(this);
                if (result != null)
                {
                    return new ExprBuilderObj(result, firstToken);
                }
            }
            Fail("Unexpected symbol: " + reader.Current.Content);
            return null;
        }

        ExprBuilderObj ParseInfix(ExprBuilderObj left, int weight)
        {
            var firstToken = reader.Current;
            if (reader.Current == null)
                return left;

            foreach (var analyzer in rules.InfixAnalyzers)
            {
                if (weight < analyzer.Weight)
                {
                    var result = analyzer.Analyze(this, left);
                    if (result != null)
                        return ParseInfix(new ExprBuilderObj(result, firstToken), weight);
                }
            }
            return left;
        }

        public bool TryConsume(Tokenizer tokenizer, out Token content)
        {
            content = null;
            if (reader.Current == null || reader.Current.Tokenizer != tokenizer)
                return false;
            content = reader.Current;
            reader.Next();
            return true;
        }

        public bool TryConsume(Token token)
        {
            if (reader.Current != null && token.Matches(reader.Current))
            {
                reader.Next();
                return true;
            }
            return false;
        }

        public void Consume(Token token)
        {
            if (reader.Current == null)
                Fail("Unexpected end of file, expected: " + token.Content);
            if (token.Matches(reader.Current))
                reader.Next();
            else
                Fail("Unexpected symbol: " + reader.Current.Content + ", expected: " + token.Content);
        }

        public void Fail(string msg)
            => throw new CompilationException(msg);
    }
}
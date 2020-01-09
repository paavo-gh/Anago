using System;
using System.Collections.Generic;

namespace LangProj
{
    public static class Parsers
    {
        public static Parser<T> Prefix<T>(Token token, Parser<T> provider)
        {
            return parser => parser.TryConsume(token) ? provider(parser) : default(T);
        }

        public static Parser<T> Suffix<T>(Parser<T> provider, Token token)
        {
            return parser =>
            {
                var val = provider(parser);
                if (val == null)
                    return default(T);
                parser.Consume(token);
                return val;
            };
        }

        public static Parser<IEnumerable<T>> ListOf<T>(Token prefixToken, Parser<T> provider, Token separatorToken, Token suffixToken)
        {
            return parser =>
            {
                if (!parser.TryConsume(prefixToken))
                    return null;
                if (parser.TryConsume(suffixToken))
                    return Array.Empty<T>();
                
                var list = new List<T>();
                do
                {
                    list.Add(provider(parser));
                }
                while (parser.TryConsume(separatorToken));
                parser.Consume(suffixToken);
                return list;
            };
        }

        public static Parser<IEnumerable<T>> Sequence<T>(Tokenizer prefix, Parser<T> provider)
        {
            return parser =>
            {
                if (parser.TryConsume(prefix, out var token))
                {
                    var list = new List<T>();
                    do
                    {
                        while (parser.TryConsume(token));
                        list.Add(provider(parser));
                    }
                    while (parser.TryConsume(token));
                    return list;
                }
                return null;
            };
        }

        public static Parser<ExprBuilderObj> Expression = parser => parser.Parse();

        public static Parser<ExprBuilderObj> WeightedExpression(int weight) => parser => parser.Parse(weight);

        public static Parser<string> Tokens(Tokenizer tokenizer) => parser => parser.TryConsume(tokenizer, out var token) ? token.Content : null;
    }
}
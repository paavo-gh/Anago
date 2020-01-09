using System;
using System.Text;

namespace LangProj
{
    public delegate string Tokenizer(CharReader tokenizer);

    public static class Tokenizers
    {
        public static Tokenizer Predicate(Predicate<char> predicate)
            => tokenizer => {
                int count = 0;
                while (tokenizer.GetLength(count + 1) == count + 1 && predicate(tokenizer.Get(count)))
                    count++;
                if (count == 0)
                    return null;
                return tokenizer.ReadAndConsume(count);
            };
        
        public static Tokenizer Match(params string[] symbols)
            => tokenizer => {
                for (int i = 0; i < symbols.Length; i++)
                {
                    if (tokenizer.StartsWith(symbols[i]))
                    {
                        tokenizer.Consume(symbols[i].Length);
                        return symbols[i];
                    }
                }
                return null;
            };
        
        public static Tokenizer Quote(char start, char end, char escape = '\\')
            => tokenizer => {
                if (tokenizer.Get(0) != start)
                    return null;
                int index = 1;
                var stringBuilder = new StringBuilder();
                while (tokenizer.GetLength(index + 1) == index + 1 && tokenizer.Get(index) != end)
                {
                    if (tokenizer.Get(index) == escape)
                        index++;
                    stringBuilder.Append(tokenizer.Get(index));
                    index++;
                }
                tokenizer.Consume(index + 1);
                return stringBuilder.ToString();
            };

        private static readonly string IndentationCharSet = " \t";

        public static Tokenizer Indentation
            = tokenizer => {
                if (tokenizer.Get(0) != '\n')
                    return null;
                while (tokenizer.GetLength(1) == 1)
                {
                    int index = 1;
                    while (tokenizer.GetLength(index + 1) == index + 1 && IndentationCharSet.Contains(tokenizer.Get(index)))
                        index++;

                    // Ignore empty lines
                    if (tokenizer.Get(index) == '\n')
                        tokenizer.Consume(index);
                    else
                        return tokenizer.ReadAndConsume(index).Substring(1);
                }
                return null;
            };
    }
}
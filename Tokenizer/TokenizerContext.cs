using System.Collections.Generic;

namespace LangProj
{
    public class TokenizerContext
    {
        CharReader reader;
        IEnumerable<Tokenizer> tokenizers;
        Tokenizer tokenizerWhiteSpace = Tokenizers.Predicate(char.IsWhiteSpace);
        public Token Current { get; private set; }

        public TokenizerContext(ICharReader reader, IEnumerable<Tokenizer> tokenizers)
        {
            this.reader = new CharReader(reader);
            this.tokenizers = tokenizers;
        }

        public bool Next()
        {
            Current = null;
            if (reader.GetLength(1) == 0)
                return false;
            do
            {
                foreach (var tokenizer in tokenizers)
                {
                    var str = tokenizer(reader);
                    if (str != null)
                    {
                        Current = new Token(str, tokenizer, reader.Row, reader.Column);
                        return true;
                    }
                    if (reader.GetLength(1) == 0) // Indentation strips line breaks
                        return false;
                }
            }
            while (tokenizerWhiteSpace(reader) != null);

            return false;
        }
    }
}
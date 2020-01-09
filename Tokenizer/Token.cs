namespace LangProj
{
    public class Token
    {
        public readonly string Content;
        public readonly Tokenizer Tokenizer;
        public readonly int Row, Column;

        public Token(string content, Tokenizer tokenizer, int row = 0, int column = 0)
        {
            this.Content = content;
            this.Tokenizer = tokenizer;
            this.Row = row;
            this.Column = column;
        }

        public bool Matches(Token token) => Content == token.Content;
    }
}
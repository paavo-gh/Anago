using System;
using System.Text;

namespace LangProj
{
    public interface ICharReader
    {
        void Read(Action<char> callback);
    }

    class StringReader : ICharReader
    {
        string content;
        int offset;

        public StringReader(string str) => content = str;

        public void Read(Action<char> callback)
        {
            if (offset < content.Length)
                callback(content[offset++]);
        }
    }

    public class CharReader
    {
        ICharReader reader;
        StringBuilder content = new StringBuilder();
        public int Row { get; private set; }
        public int Column { get; private set; }

        public CharReader(ICharReader reader)
        {
            this.reader = reader;
            this.Row = 1;
            this.Column = 1;
        }

        public void Consume(int count)
        {
            var len = GetLength(count);
            for (int i = 0; i < count; i++)
            {
                if (content[i] == '\n')
                {
                    Row++;
                    Column = 1;
                }
                else
                    Column++;
            }
            content.Remove(0, len);
        }

        public char Get(int index)
        {
            GetLength(index + 1);
            return content[index];
        }

        public string ReadAndConsume(int count)
        {
            count = GetLength(count);
            var str = content.ToString().Substring(0, count);
            Consume(count);
            return str;
        }

        public bool StartsWith(string str)
        {
            return GetLength(str.Length) == str.Length && content.ToString().StartsWith(str);
        }

        public int GetLength(int count)
        {
            int len = content.Length;
            while (len < count)
            {
                reader.Read(chr => content.Append(chr));
                if (len == content.Length)
                    break;
                len = content.Length;
            }
            return Math.Min(count, len);
        }

        public static ICharReader File(string fileName)
            => new StringReader(System.IO.File.ReadAllText(fileName));
        
        public static ICharReader String(string str)
            => new StringReader(str);
    }
}
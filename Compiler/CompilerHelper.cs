using System;
using System.Collections.Generic;

namespace LangProj
{
    public class CompilerHelper
    {
        ExprWriters expressions = new ExprWriters();
        List<Tokenizer> tokenizers = new List<Tokenizer>();
        List<string> symbols = new List<string>();
        ParserRules rules = new ParserRules();
        public ParserRules ParserRules => rules;
        AnalyzerContext analyzer = new AnalyzerContext();
        VMWriter vmw = new VMWriter();
        int coreFunctions = 0;

        public CompilerHelper(params Tokenizer[] tokenizers)
        {
            this.tokenizers.Add(CustomTokenizer);
            this.tokenizers.AddRange(tokenizers);
        }

        /// <summary>
        /// Turns the given string into a token. The string is added to a list of 
        /// custom symbols, in case it's not covered by any of the tokenizers, 
        /// to ensure it's parsed as a whole token.
        /// </summary>
        public Token GetToken(string str)
        {
            var tokenizer = new TokenizerContext(CharReader.String(str), tokenizers);
            if (tokenizer.Next())
            {
                var token = tokenizer.Current;
                if (!tokenizer.Next())
                    return token;
            }

            symbols.Add(str);
            symbols.Sort((a, b) => b.Length - a.Length);
            return new Token(str, CustomTokenizer);
        }

        string CustomTokenizer(CharReader tokenizer)
        {
            for (int i = 0; i < symbols.Count; i++)
            {
                if (tokenizer.StartsWith(symbols[i]))
                {
                    tokenizer.Consume(symbols[i].Length);
                    return symbols[i];
                }
            }
            return null;
        }

        public void AddType(string name, IType type)
        {
            analyzer.Context.AddType(name, type);
        }

        public void AddFunction(string name, VMCommand command, IType returnType, params IType[] arguments)
        {
            if (command.ParameterCount != arguments.Length)
                throw new Exception("Mismatching core function parameter count: " + name);
            var fun = new ExprImpl(
                new TypeFunction(new TypeTuple(arguments), returnType), 
                expressions.Function(new TypeTuple(arguments), new CoreFunction(command)));
            analyzer.Context.AddVariable(new Variable(name, fun.Type));
            fun.WriteTo(vmw);
            vmw.Write(VMCommand.CurrentContext);
            vmw.Write(VMCommand.AssignContextVariable, coreFunctions++);
        }

        public void RunFile(string fileName)
        {
            var reader = CharReader.File(fileName);
            var tokenReader = new TokenizerContext(reader, tokenizers);
            analyzer
                .Analyze(new ParserContext(ParserRules, tokenReader).Parse())
                .WriteTo(vmw);

            vmw.Run();
        }
    }

    class CoreFunction : Expr
    {
        VMCommand command;

        public CoreFunction(VMCommand command)
        {
            this.command = command;
        }

        public IType Type => TypeSingleton.Void;

        public bool IsConstant => false;

        public void WriteTo(IVMWriter writer)
        {
            for (int i = command.ParameterCount; i > 0; i--)
            {
                writer.Write(VMCommand.CurrentContext);
                writer.Write(VMCommand.ReadContextVariable, i);
            }
            writer.Write(command);
        }
    }
}
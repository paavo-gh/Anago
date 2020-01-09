using System;
using System.Collections.Generic;

namespace LangProj
{
    public interface IVMWriter
    {
        void Write(VMCommand command, params int[] args);
        int WriteSymbol(string symbol);
        int WriteFunction(Action<IVMWriter> writer);
        Action WriteJump();
    }

    public class VMWriter : IVMWriter
    {
        int index;
        List<List<int>> content = new List<List<int>>() { new List<int>() };
        List<string> symbolTable = new List<string>();

        public void Run()
        {
            new DummyVM().Run(content.ConvertAll(c => c.ToArray()).ToArray(), symbolTable.ToArray());
        }

        public int WriteFunction(Action<IVMWriter> callback)
        {
            var writer = new VMWriter()
            {
                index = content.Count,
                content = content,
                symbolTable = symbolTable
            };
            content.Add(new List<int>());
            callback(writer);
            return writer.index;
        }

        public void Write(VMCommand command, params int[] args)
        {
            content[index].Add(command.Id);
            foreach (var arg in args)
                content[index].Add(arg);
            
            if (DummyVM.Debug)
                Console.WriteLine(index + ": " + command.Name + " " + string.Join(" ", args));
        }

        public Action WriteJump()
        {
            int addr = content[index].Count;
            content[index].Add(0);
            return () => content[index][addr] = content[index].Count;
        }

        public int WriteSymbol(string symbol)
        {
            int index = symbolTable.IndexOf(symbol);
            if (index < 0)
            {
                index = symbolTable.Count;
                symbolTable.Add(symbol);
            }
            return index;
        }
    }
}
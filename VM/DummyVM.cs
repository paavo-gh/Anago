using System;
using System.Collections.Generic;

namespace LangProj
{
    public class DummyVM
    {
        Stack<object> stack = new Stack<object>();
        Stack<int> callStack = new Stack<int>();
        Stack<object[]> contextStack = new Stack<object[]>();
        string[] symbolTable;
        public static readonly bool Debug = false;

        public int ReturnAddress => callStack.Peek();
        public object[] Context => contextStack.Peek();
        public int Address;
        int[][] functions;
        int[] content;

        const int FunctionAddress = 0;
        const int FunctionBaseContext = 1;

        public int ReadNext() => content[Address++];

        void PrintValue(object val, string prefix = "")
        {
            if (val is object[])
                System.Console.WriteLine(prefix + "[" + string.Join(",", val as object[]) + "]");
            else
                System.Console.WriteLine(prefix + val);
        }

        public object StackPop()
        {
            var val = stack.Pop();
            //if (Debug) PrintValue(val, "POP ");
            //if (Debug) Console.WriteLine("  POP");
            return val;
        }

        public void StackPush(object val)
        {
            stack.Push(val);
            //if (Debug) PrintValue(val, "  PUSH ");
        }
        public object[] PopObject() => (object[]) StackPop();
        public int PopInt() => (int) StackPop();

        public void FunctionCall()
        {
            var functionInstance = PopObject();
            callStack.Push(Address);
            callStack.Push(Array.IndexOf(functions, content)); // TODO
            StackPush(functionInstance[FunctionBaseContext]);
            content = functions[(int) functionInstance[FunctionAddress]];
            Address = 0;
            contextStack.Push(new object[10]); // TODO
        }
        
        public void FunctionReturn()
        {
            content = functions[callStack.Pop()];
            Address = callStack.Pop();
            contextStack.Pop();
        }

        public void DefineFunction()
        {
            var instance = new object[2];
            instance[FunctionAddress] = ReadNext();
            // Functions may need reference to their base context
            instance[FunctionBaseContext] = Context;
            stack.Push(instance);
        }

        public void CurrentContext()
        {
            StackPush(Context);
        }

        public void AssignContextVariable()
        {
            var obj = PopObject();
            obj[ReadNext()] = StackPop();
        }

        public void ReadContextVariable()
        {
            StackPush(PopObject()[ReadNext()]);
        }

        public void PushInt() => StackPush(ReadNext());

        public void PushSymbol() => StackPush(symbolTable[ReadNext()]);

        public void Jump() => Address = ReadNext();

        public void JumpIf()
        {
            var addr = ReadNext();
            if (PopInt() != 0)
                Address = addr;
        }

        public void Print() => PrintValue(StackPop());

        public void CompareLessThan() => StackPush(PopInt() < PopInt() ? 1 : 0);

        public void MathAddition() => StackPush(PopInt() + PopInt());

        public void MathSubstraction() => StackPush(PopInt() - PopInt());

        public void MathMultiplication() => StackPush(PopInt() * PopInt());

        public void MathDivision() => StackPush(PopInt() / PopInt());

        public void BooleanNot() => StackPush(PopInt() == 1 ? 0 : 1);

        public void EqualInt() => StackPush(PopInt() == PopInt() ? 1 : 0);

        public void Pop() => StackPop();

        Action[] commands = new Action[byte.MaxValue];

        public DummyVM()
        {
            commands[VMCommand.AssignContextVariable] = AssignContextVariable;
            commands[VMCommand.CurrentContext] = CurrentContext;
            commands[VMCommand.DefineFunction] = DefineFunction;
            commands[VMCommand.FunctionCall] = FunctionCall;
            commands[VMCommand.FunctionReturn] = FunctionReturn;
            commands[VMCommand.Print] = Print;
            commands[VMCommand.PushInt] = PushInt;
            commands[VMCommand.PushSymbol] = PushSymbol;
            commands[VMCommand.ReadContextVariable] = ReadContextVariable;
            commands[VMCommand.Jump] = Jump;
            commands[VMCommand.JumpIf] = JumpIf;
            commands[VMCommand.CompareLessThan] = CompareLessThan;
            commands[VMCommand.MathAddition] = MathAddition;
            commands[VMCommand.MathSubstraction] = MathSubstraction;
            commands[VMCommand.MathMultiplication] = MathMultiplication;
            commands[VMCommand.MathDivision] = MathDivision;
            commands[VMCommand.BooleanNot] = BooleanNot;
            commands[VMCommand.EqualInt] = EqualInt;
            commands[VMCommand.Pop] = Pop;
        }

        public void Run(int[][] functions, string[] symbolTable)
        {
            this.symbolTable = symbolTable;
            this.content = functions[0];
            this.functions = functions;
            contextStack.Push(new object[50]); // TODO

            while (Address < content.Length)
            {
                //if (Debug)
                //    Console.WriteLine(Address + " -> " + commands[content[Address]].Method.Name + ", " + stack.Count);
                var next = ReadNext();
                commands[next].Invoke();
            }
        }
    }
}
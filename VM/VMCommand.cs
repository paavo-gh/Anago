namespace LangProj
{
    public class VMCommand
    {
        public readonly byte Id;
        public readonly string Name;
        public readonly int ParameterCount;

        private VMCommand(byte id, string name, int parameterCount = 0)
        {
            this.Id = id;
            this.Name = name;
            this.ParameterCount = parameterCount;
        }

        public readonly static VMCommand
            FunctionCall = new VMCommand(1, "FunctionCall"),
            CurrentContext = new VMCommand(2, "CurrentContext"),
            AssignContextVariable = new VMCommand(3, "AssignContextVariable", 1),
            ReadContextVariable = new VMCommand(4, "ReadContextVariable", 1),
            PushInt = new VMCommand(5, "PushInt", 1),
            Print = new VMCommand(6, "Print", 1),
            FunctionReturn = new VMCommand(7, "FunctionReturn"),
            DefineFunction = new VMCommand(8, "DefineFunction", 1),
            PushSymbol = new VMCommand(9, "PushSymbol", 1),
            Pop = new VMCommand(10, "Pop"),
            JumpIf = new VMCommand(11, "JumpIf"),
            Jump = new VMCommand(12, "Jump"),
            CompareLessThan = new VMCommand(13, "CompareLessThan", 2),
            MathAddition = new VMCommand(14, "MathAddition", 2),
            MathSubstraction = new VMCommand(15, "MathSubstraction", 2),
            MathMultiplication = new VMCommand(16, "MathMultiplication", 2),
            MathDivision = new VMCommand(17, "MathDivision", 2),
            BooleanNot = new VMCommand(18, "BooleanNot", 1),
            EqualInt = new VMCommand(19, "EqualInt", 2);

        public static implicit operator int(VMCommand v) => v.Id;
    }
}
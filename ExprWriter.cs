using System.Collections.Generic;
using System.Linq;

namespace LangProj
{
    /// <summary>
    /// Converts expressions into VM code.
    /// </summary>
    public delegate void ExprWriter(IVMWriter writer);

    public class ExprWriters
    {
        public ExprWriter
            VoidLiteral = writer => writer.Write(VMCommand.PushInt, 0),
            BoolLiteralTrue = writer => writer.Write(VMCommand.PushInt, 1),
            BoolLiteralFalse = writer => writer.Write(VMCommand.PushInt, 0),
            This = writer => writer.Write(VMCommand.CurrentContext);

        public ExprWriter IntLiteral(int val)
            => writer => writer.Write(VMCommand.PushInt, val);
        
        public ExprWriter StringLiteral(string val)
            => writer => {
                int address = writer.WriteSymbol(val);
                writer.Write(VMCommand.PushSymbol, address);
            };
        
        public ExprWriter FunctionCall(Expr functionExpr, params Expr[] args)
            => FunctionCall(functionExpr, (IEnumerable<Expr>) args);
        
        public ExprWriter FunctionCall(Expr functionExpr, IEnumerable<Expr> args)
            => writer => {
                foreach (var arg in args)
                    arg.WriteTo(writer);
                functionExpr.WriteTo(writer);
                writer.Write(VMCommand.FunctionCall);
            };

        public ExprWriter Function(TypeTuple argType, Expr body)
            => writer => {
                var address = writer.WriteFunction(w =>
                {
                    w.Write(VMCommand.CurrentContext);
                    w.Write(VMCommand.AssignContextVariable, 0);
                    for (var count = argType.Types.Count(); count > 0; count--)
                    {
                        w.Write(VMCommand.CurrentContext);
                        w.Write(VMCommand.AssignContextVariable, count);
                    }
                    body.WriteTo(w);
                    w.Write(VMCommand.FunctionReturn);
                });
                writer.Write(VMCommand.DefineFunction, address);
            };
        
        public ExprWriter Sequence(params Expr[] exprs)
            => Sequence((IEnumerable<Expr>) exprs);
        
        public ExprWriter Sequence(IEnumerable<Expr> exprs)
            => writer => {
                foreach (var expr in exprs)
                    expr.WriteTo(writer);
            };

        public ExprWriter If(Expr condition, Expr ifTrue, Expr ifFalse)
            => writer => {
                condition.WriteTo(writer);
                writer.Write(VMCommand.JumpIf);
                var jump1 = writer.WriteJump();
                if (ifFalse != null)
                    ifFalse.WriteTo(writer);
                writer.Write(VMCommand.Jump);
                var jump2 = writer.WriteJump();
                jump1();
                ifTrue.WriteTo(writer);
                jump2();
            };

        public ExprWriter And(Expr left, Expr right)
            => writer => {
                left.WriteTo(writer);
                writer.Write(VMCommand.JumpIf);
                var jump1 = writer.WriteJump();
                writer.Write(VMCommand.PushInt, 0);
                writer.Write(VMCommand.Jump);
                var jump2 = writer.WriteJump();
                jump1();
                right.WriteTo(writer);
                jump2();
            };
        
        public ExprWriter Or(Expr left, Expr right)
            => writer => {
                left.WriteTo(writer);
                writer.Write(VMCommand.JumpIf);
                var jump1 = writer.WriteJump();
                right.WriteTo(writer);
                writer.Write(VMCommand.Jump);
                var jump2 = writer.WriteJump();
                jump1();
                writer.Write(VMCommand.PushInt, 1);
                jump2();
            };
        
        public ExprWriter ReadContextVariable(int index)
            => writer => writer.Write(VMCommand.ReadContextVariable, index);
        
        public ExprWriter ReadVariable(IEnumerable<int> indexList)
            => writer => {
                writer.Write(VMCommand.CurrentContext);
                foreach (var index in indexList)
                    writer.Write(VMCommand.ReadContextVariable, index);
            };
        
        public ExprWriter WriteVariable(IEnumerable<int> indexList, Expr valueExpr)
            => writer => {
                valueExpr.WriteTo(writer);
                writer.Write(VMCommand.CurrentContext);
                foreach (var index in indexList.SkipLast(1))
                    writer.Write(VMCommand.ReadContextVariable, index);
                writer.Write(VMCommand.AssignContextVariable, indexList.Last());
            };
    }
}
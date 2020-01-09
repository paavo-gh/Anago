using System;

namespace LangProj
{
    public interface Expr
    {
        /// <summary>
        /// Type of the expression.
        /// </summary>
        IType Type { get; }

        /// <summary>
        /// Writes VM code for the expression.
        /// </summary>
        void WriteTo(IVMWriter writer);

        /// <summary>
        /// For optimization (TODO).
        /// </summary>
        bool IsConstant { get; }
    }

    public class ExprImpl : Expr
    {
        IType type;
        ExprWriter writer;

        public ExprImpl(IType type, ExprWriter writer, bool constant = false)
        {
            this.type = type;
            this.writer = writer;
            this.IsConstant = constant;
        }

        public ExprImpl(IType type, Expr expr) : this(type, expr.WriteTo) {}

        public IType Type => type;

        public bool IsConstant { get; private set; }

        public void WriteTo(IVMWriter writer) => this.writer(writer);
    }

    /// <summary>
    /// Expression that is compiled when the type is requested.
    /// </summary>
    public class ExprLazy : Expr
    {
        Func<Expr> exprInitializer;
        Expr expr;

        public ExprLazy(Func<Expr> exprInitializer)
        {
            this.exprInitializer = exprInitializer;
        }

        Expr Expression => expr ?? (expr = exprInitializer());
 
        public IType Type => Expression.Type;

        public bool IsConstant => false; // TODO Expression.IsConstant;

        public void WriteTo(IVMWriter writer) => Expression.WriteTo(writer);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace LangProj
{
    public class Variable
    {
        public readonly string Name;
        LazyType type;
        public IType Type => type.Type;
        /// <summary>
        /// If set, all occurrences of this variable will be replaced by the constant value.
        /// </summary>
        public readonly Expr ConstantValue;

        public Variable(string name, IType type, Expr constantValue = null)
        {
            this.Name = name;
            this.type = new LazyType(type);
            this.ConstantValue = constantValue;
        }

        public Variable(string name, Func<IType> typeInitializer, Expr constantValue = null)
        {
            this.Name = name;
            this.type = new LazyType(typeInitializer);
            this.ConstantValue = constantValue;
        }

        public override string ToString()
            => Name + " " + (Type is TypeContext ? "obj" : Type.ToString()); // Avoiding infinite recursion
    }

    public static class VariableExtensions
    {
        public static Variable Get(this IEnumerable<Variable> self, string name) => self.Get(name, null);

        public static Variable Get(this IEnumerable<Variable> self, string name, IType assignableFrom)
        {
            foreach (var variable in self)
                if (variable.Name == name && (assignableFrom == null || variable.Type.AssignableFrom(assignableFrom)))
                    return variable;
            return null;
        }

        public static Variable GetAssignableTo(this IEnumerable<Variable> self, string name, IType assignableTo)
        {
            foreach (var variable in self)
                if (variable.Name == name && (assignableTo == null || assignableTo.AssignableFrom(variable.Type)))
                    return variable;
            return null;
        }

        public static int GetReferenceIndex(this IEnumerable<Variable> self, Variable variable)
        {
            int index = 0;
            foreach (var v in self)
            {
                if (v == variable)
                    return index;
                index++;
            }
            return -1;
        }
    }
}
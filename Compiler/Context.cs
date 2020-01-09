using System;
using System.Collections.Generic;
using System.Linq;

namespace LangProj
{
    public class Context
    {
        Context parent;
        List<(string, LazyType)> namedTypes = new List<(string, LazyType)>();
        private List<Variable> variables = new List<Variable>();
        /// <summary>
        /// Interface for the context. Excludes the base context.
        /// </summary>
        public IEnumerable<Variable> LocalVariables => variables.Skip(1);

        public Context(Context parent)
        {
            this.parent = parent;

            if (parent != null)
                variables.Add(new Variable("base", parent.Type));
        }

        public void AddVariable(Variable variable) => variables.Add(variable);

        public void AddType(string name, IType type)
            => namedTypes.Add((name, new LazyType(type)));

        public void AddType(string name, Func<IType> typeInitializer)
            => namedTypes.Add((name, new LazyType(typeInitializer)));

        public IType GetType(string name)
        {
            for (int i = 0; i < namedTypes.Count; i++)
                if (namedTypes[i].Item1 == name)
                    return namedTypes[i].Item2.Type;
            return parent?.GetType(name);
        }

        public IType Type => new TypeContext(this);

        //public Variable GetVariable(string name) => GetVariable(name, null);

        //public Variable GetVariable(string name, IType assignableFrom)
        //    => variables.Get(name, assignableFrom) ?? parent?.GetVariable(name, assignableFrom);

        public Variable GetVariableAssignableTo(string name, IType assignableTo)
            => variables.GetAssignableTo(name, assignableTo) ?? parent?.GetVariableAssignableTo(name, assignableTo);
        
        public IEnumerable<int> GetReferenceIndex(Variable variable)
        {
            var list = new List<int>();
            var context = this;
            do
            {
                var index = context.variables.GetReferenceIndex(variable);
                if (index < 0)
                    list.Add(0);
                else
                {
                    list.Add(index);
                    return list;
                }
                context = context.parent;
            }
            while (context != null);

            throw new CompilationException("Variable does not belong to the context: " + variable);
        }
    }
}
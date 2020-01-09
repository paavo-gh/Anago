using System.Collections.Generic;
using System.Linq;

namespace LangProj
{
    public class TypeName
    {
        string typeName;
        TypeName[] genericParameters;

        public TypeName(string typeName, IEnumerable<TypeName> genericParameters)
        {
            this.typeName = typeName;
            this.genericParameters = genericParameters?.ToArray();
        }

        /// <summary>
        /// Translates the type name into a type from the given context.
        /// </summary>
        public IType Resolve(Context context)
        {
            var type = context.GetType(typeName);
            if (type == null)
                throw new CompilationException("Unknown type: " + typeName);
            if (genericParameters == null)
                return type;
            return type.CreateGenericType(genericParameters.Select(p => p.Resolve(context)));
        }
    }
}
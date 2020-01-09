using System;
using System.Collections.Generic;
using System.Linq;

namespace LangProj
{
    public interface IType
    {
        bool AssignableFrom(IType other);
        /// <summary>
        /// Returns the concrete type when wrapped inside a generic type.
        /// <summary>
        IType ConcreteType { get; }
        /// <summary>
        /// Creates a new type with all generic types converted to the given concrete types.
        /// </summary>
        IType ConvertGeneric(Func<IType, IType> converter);
        /// <summary>
        /// Creates a new type with the given generic type parameters.
        /// </summary>
        IType CreateGenericType(IEnumerable<IType> types);

        IEnumerable<TypeGeneric> GenericParameters { get; }
    }

    public static class ITypeExtensions
    {
        public static bool Matches(this IType a, IType b)
            => a == b || (a.AssignableFrom(b) && b.AssignableFrom(a));
    }

    public class LazyType
    {
        Func<IType> typeInitializer;
        IType type;

        public LazyType(Func<IType> typeInitializer) => this.typeInitializer = typeInitializer;

        public LazyType(IType type) => this.type = type;

        public IType Type => type ?? (type = typeInitializer());
    }

    public abstract class AbstractType : IType
    {
        public bool AssignableFrom(IType other) => assignableFrom(other.ConcreteType);

        protected virtual bool assignableFrom(IType other) => this == other;

        public virtual IType ConvertGeneric(Func<IType, IType> converter) => this;

        public virtual IType CreateGenericType(IEnumerable<IType> types)
        {
            throw new NotImplementedException("Generics not supported on this type");
        }

        public IType ConcreteType => this;

        public IEnumerable<TypeGeneric> GenericParameters => Array.Empty<TypeGeneric>();
    }

    public class TypeUnion : AbstractType
    {
        IType[] types;

        public TypeUnion()
            => this.types = Array.Empty<IType>();

        public TypeUnion(IEnumerable<IType> types)
            => this.types = types.ToArray();
        
        public static IType Create(params IType[] types)
            => Create((IEnumerable<IType>) types);

        public static IType Create(IEnumerable<IType> types)
        {
            return new TypeUnion(types); // TODO clear duplicates
        }

        protected override bool assignableFrom(IType other)
        {
            return types.Any(type => type.AssignableFrom(other));
        }

        public override IType ConvertGeneric(Func<IType, IType> converter)
            => new TypeUnion(types.Select(type => type.ConvertGeneric(converter)));

        public override IType CreateGenericType(IEnumerable<IType> types) => new TypeUnion(types);

        public override string ToString() => "union{" + string.Join(",", (IEnumerable<IType>) types) + "}";
    }

    public class TypeGeneric : IType
    {
        IType constraintType;

        public TypeGeneric(IType constraintType)
            => this.constraintType = constraintType;

        public bool AssignableFrom(IType other)
            => constraintType.AssignableFrom(other.ConcreteType);

        public IType ConvertGeneric(Func<IType, IType> converter) => converter(this);

        public IType ConcreteType => constraintType;

        public IType CreateGenericType(IEnumerable<IType> types)
        {
            throw new CompilationException("Generics not supported on this type: " + this);
        }

        public IEnumerable<TypeGeneric> GenericParameters => Array.Empty<TypeGeneric>();
    }

    public class TypeGenericContext : IType
    {
        IType wrappedType;
        public IEnumerable<TypeGeneric> GenericParameters { get; private set; }

        public TypeGenericContext(IType wrappedType, IEnumerable<TypeGeneric> types)
        {
            this.wrappedType = wrappedType;
            this.GenericParameters = types;
        }

        public IType ConcreteType => wrappedType;

        public bool AssignableFrom(IType other)
            => wrappedType.AssignableFrom(other.ConcreteType);

        public IType ConvertGeneric(Func<IType, IType> converter) => wrappedType.ConvertGeneric(converter);

        public IType CreateGenericType(IEnumerable<IType> types)
        {
            throw new CompilationException("Generics not supported on this type");
        }
    }

    class TypeAny : AbstractType
    {
        protected override bool assignableFrom(IType other) => true;

        public override string ToString() => "any";
    }

    public class TypeSingleton : AbstractType
    {
        public static readonly IType
            Any = new TypeAny(),
            Void = new TypeSingleton("void"),
            Boolean = new TypeSingleton("bool"),
            Integer = new TypeSingleton("int"),
            String = new TypeSingleton("string");

        string name;

        private TypeSingleton(string name)
            => this.name = name;

        public override string ToString() => name;
    }

    public class TypeFunction : AbstractType
    {
        /// <summary>
        /// Tuple can be used to support multiple parameters.
        /// </summary>
        public readonly IType ArgType;
        public readonly IType ReturnType;

        public TypeFunction(IType argType, IType returnType)
        {
            this.ArgType = argType;
            this.ReturnType = returnType;
        }

        protected override bool assignableFrom(IType other)
        {
            var otherType = other as TypeFunction;
            if (otherType != null)
                return otherType.ArgType.AssignableFrom(ArgType) && ReturnType.AssignableFrom(otherType.ReturnType);
            return false;
        }

        public override IType ConvertGeneric(Func<IType, IType> converter)
            => new TypeFunction(ArgType.ConvertGeneric(converter), ReturnType.ConvertGeneric(converter));

        public override IType CreateGenericType(IEnumerable<IType> types)
        {
            var t = types.GetEnumerator();
            if (!t.MoveNext())
                throw new CompilationException("Unsupported number of generic parameters");
            var p1 = t.Current;
            var p2 = t.MoveNext() ? t.Current : TypeSingleton.Void;
            return new TypeFunction(p1, p2);
        }

        public override string ToString() => "fun{" + ArgType + "," + ReturnType + "}";
    }

    public class TypeTuple : AbstractType
    {
        private IType[] types;

        public TypeTuple(IEnumerable<IType> types)
        {
            this.types = types.ToArray();
        }

        public TypeTuple(params IType[] types) : this((IEnumerable<IType>) types) {}

        public IEnumerable<IType> Types => types;

        protected override bool assignableFrom(IType other)
        {
            var otherType = other as TypeTuple;
            if (otherType == null || otherType.types.Length != types.Length)
                return false;

            for (int i = 0; i < types.Length; i++)
                if (!types[i].AssignableFrom(otherType.types[i]))
                    return false;
            return true;
        }

        public override IType ConvertGeneric(Func<IType, IType> converter)
            => new TypeTuple(types.Select(type => type.ConvertGeneric(converter)));

        public override IType CreateGenericType(IEnumerable<IType> types) => new TypeTuple(types);

        public override string ToString() => "tuple{" + string.Join(",", Types) + "}";
    }

    public class TypeContext : AbstractType
    {
        public IEnumerable<Variable> Variables { get; private set; }

        public TypeContext(Context context) : this(context.LocalVariables) {}

        public TypeContext(IEnumerable<Variable> variables) => this.Variables = variables.ToArray();

        public TypeContext(params Variable[] variables) => this.Variables = variables;

        protected override bool assignableFrom(IType other)
        {
            var otherType = other as TypeContext;
            if (otherType == null)
                return false;
            if (Variables != otherType.Variables)
                foreach (var variable in Variables)
                    if (otherType.Variables.GetAssignableTo(variable.Name, variable.Type) == null)
                        return false;
            return true;
        }

        public override IType ConvertGeneric(Func<IType, IType> converter)
            => new TypeContext(Variables.Select(variable => new Variable(variable.Name, variable.Type.ConvertGeneric(converter))));

        public override string ToString()
            => "(" + string.Join(", ", Variables) + ")";
    }
}
using BoDi;

namespace SpecFlowConnector.SpecFlowProxies;

public static class BoDiReflectionExtensions
{
    public static bool ReflectionIsRegistered<T>(this object container, string name)
    {
        return container.ReflectionCallMethod<bool>(nameof(IObjectContainer.IsRegistered),
            new[] {typeof(string)}, name);
    }

    public static T ReflectionResolve<T>(this object container)
    {
        return container.ReflectionCallMethod<T>(nameof(IObjectContainer.Resolve),
            new[] {typeof(Type), typeof(string)},
            typeof(T), null!);
    }

    public static void ReflectionRegisterTypeAs<TType, TInterface>(this object container)
        where TType : class, TInterface
    {
        ReflectionRegisterTypeAs(container, typeof(TType), typeof(TInterface));
    }

    public static void ReflectionRegisterTypeAs(this object container, Type implementationType, Type interfaceType)
    {
        container.ReflectionCallMethod(nameof(IObjectContainer.RegisterTypeAs),
            new[] {typeof(Type), typeof(Type), typeof(string)},
            implementationType, interfaceType, null!);
    }
}

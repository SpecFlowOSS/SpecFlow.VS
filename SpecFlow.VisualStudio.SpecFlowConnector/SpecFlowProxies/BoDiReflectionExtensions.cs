namespace SpecFlowConnector.SpecFlowProxies;

public static class BoDiReflectionExtensions
{
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

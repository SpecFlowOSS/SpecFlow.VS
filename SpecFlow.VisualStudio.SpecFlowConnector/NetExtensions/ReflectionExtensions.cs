namespace SpecFlowConnector;

public static class ReflectionExtensions
{
    public static T ReflectionCallMethod<T>(this object obj, string methodName, params object[]? args)
    {
        return ReflectionCallMethod<T>(obj, methodName, args?.Select(a => a.GetType()).ToArray() ?? Type.EmptyTypes, args);
    }

    public static void ReflectionCallMethod(this object obj, string methodName, Type[] parameterTypes,
        params object[]? args)
    {
        ReflectionCallMethod<object>(obj, methodName, parameterTypes, args);
    }

    public static T ReflectionCallMethod<T>(this object obj, string methodName, Type[] parameterTypes,
        params object[]? args)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        var objType = obj.GetType();
        var methodInfo = objType.GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);
        if (methodInfo == null)
            throw new ArgumentException($"Cannot find method {methodName} on type {objType.FullName}");
        var invoke = methodInfo.Invoke(obj, args);
        
        if (invoke is T result) return result;
            
        if(methodInfo.ReturnType != typeof(void))
            throw new InvalidCastException($"'{invoke?.GetType()}' is not {typeof(T)}");

        return default!;
    }
}

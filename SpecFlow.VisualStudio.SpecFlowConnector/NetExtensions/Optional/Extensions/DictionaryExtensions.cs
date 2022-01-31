namespace CodingHelmet.Optional.Extensions;

public static class DictionaryExtensions
{
    public static Option<TValue> TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) =>
        dictionary.TryGetValue(key, out TValue value)
            ? new Some<TValue>(value)
            : new None<TValue>();
}

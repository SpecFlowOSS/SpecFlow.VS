﻿// ReSharper disable once CheckNamespace

public static class OptionAdapters
{
    public static Either<TLeft, TRight> Map<TLeft, TRight>(this Option<TRight> option, Func<TLeft> none) =>
        option is Some<TRight> some
            ? some.Content
            : none();

    public static Option<T> AsOption<T>(this T? @this) => @this;

    public static Option<T> Validate<T>(this T @this, Func<T, bool> predicate) =>
        predicate(@this)
            ? @this
            : None.Value;
}

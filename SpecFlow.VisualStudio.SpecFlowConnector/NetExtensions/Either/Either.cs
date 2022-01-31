public abstract class Either<TLeft, TRight>
{
    public static implicit operator Either<TLeft, TRight>(TLeft left)
    {
        return new Left<TLeft,TRight>(left);
    }

    public static implicit operator Either<TLeft, TRight>(TRight left)
    {
        return new Right<TLeft, TRight>(left);
    }
}

public class Left<TLeft, TRight> : Either<TLeft, TRight>
{
    TLeft Value { get; }

    public Left(TLeft value)
    {
        this.Value = value;
    }

    public static implicit operator TLeft(Left<TLeft, TRight> left)
        => left.Value;
}

public class Right<TLeft, TRight> : Either<TLeft, TRight>
{
    TRight Value { get; }

    public Right(TRight value)
    {
        this.Value = value;
    }

    public static implicit operator TRight(Right<TLeft, TRight> right)
        => right.Value;
}

public static class EitherAdapters
{
    public static Either<TLeft, TNewRight> Map<TLeft, TRight, TNewRight>(this Either<TLeft, TRight> either,
        Func<TRight, TNewRight> map) =>
        either is Right<TLeft, TRight> right
            ? (Either<TLeft, TNewRight>) map(right)
            : (TLeft) (Left<TLeft, TRight>) either;

    public static Either<TLeft, TNewRight> Map<TLeft, TRight, TNewRight>(this Either<TLeft, TRight> either,
        Func<TRight, Either<TLeft, TNewRight>> map) =>
        either is Right<TLeft, TRight> right
            ? (Either<TLeft, TNewRight>)map(right)
            : (TLeft)(Left<TLeft, TRight>)either;

    public static Either<TLeft, TRight> Tie<TLeft, TRight>(this Either<TLeft, TRight> either,
        Action<TRight> tie)
    {
        if (either is Right<TLeft, TRight> right)
            tie(right);
        return either;
    }

    public static TRight Reduce<TLeft, TRight>(this Either<TLeft, TRight> either, Func<TLeft, TRight> map)
    {
        return either is Left<TLeft, TRight> left
            ? map(left)
            : (Right < TLeft, TRight >) either;
    }
}
namespace Simply.Reactive.Wpf.Monads
{
    internal interface IMaybe<out T>
    {
        T Value { get; }
        bool HasValue { get; }
    }
}

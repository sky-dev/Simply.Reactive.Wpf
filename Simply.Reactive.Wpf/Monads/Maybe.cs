namespace Simply.Reactive.Wpf.Monads
{
    internal class Maybe<T> : IMaybe<T>
    {
        public static readonly Maybe<T> Nothing = new Maybe<T>();

        public T Value { get; private set; }
        public bool HasValue { get; private set; }

        private Maybe()
        {
            HasValue = false;
        }

        public Maybe(T value)
        {
            Value = value;
            HasValue = true;
        }

        public IMaybe<T> AsIMaybe()
        {
            return this;
        }

        public override string ToString()
        {
            return !HasValue ? string.Format("Maybe<{0}>.Nothing", typeof(T).FullName) : string.Format("Maybe<{0}>({1})", typeof(T).FullName, Value);
        }
    }
}

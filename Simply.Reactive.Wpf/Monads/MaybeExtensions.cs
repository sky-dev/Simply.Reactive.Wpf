using System;
using System.Collections.Generic;
using System.Linq;

namespace Simply.Reactive.Wpf.Monads
{
    internal static class MaybeExtensions
    {
        public static T Select<T>(this IMaybe<T> m, T otherwise)
        {
            return m.HasValue ? m.Value : otherwise;
        }

        public static IMaybe<T> Where<T>(this IMaybe<T> m, Func<T, bool> predicate)
        {
            if (!m.HasValue || !predicate(m.Value))
                return Maybe<T>.Nothing;
            return m;
        }

        public static Maybe<T> ToMaybe<T>(this T value)
        {
            return new Maybe<T>(value);
        }

        public static IMaybe<U> Bind<T, U>(this IMaybe<T> m, Func<T, IMaybe<U>> k)
        {
            return m.HasValue ? k(m.Value) : Maybe<U>.Nothing;
        }

        public static IMaybe<T> SelectMany<T>(this IMaybe<IMaybe<T>> m)
        {
            return m.SelectMany(m2 => m2);
        }

        public static IMaybe<V> SelectMany<T, U, V>(this IMaybe<T> m, Func<T, IMaybe<U>> k, Func<T, U, V> select)
        {
            return m.Bind(x => k(x).Bind(y => @select(x, y).ToMaybe()));
        }

        public static IMaybe<U> SelectMany<T, U>(this IMaybe<T> m, Func<T, IMaybe<U>> k)
        {
            return m.Bind(k);
        }

        public static Maybe<T> FirstOrNothing<T>(this IEnumerable<T> source)
        {
            var val = source.FirstOrDefault();
            return Utility.IsDefault(val) ? Maybe<T>.Nothing : val.ToMaybe();
        }

        public static Maybe<T> FirstOrNothing<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var val = source.FirstOrDefault(predicate);
            return Utility.IsDefault(val) ? Maybe<T>.Nothing : val.ToMaybe();
        }
    }
}

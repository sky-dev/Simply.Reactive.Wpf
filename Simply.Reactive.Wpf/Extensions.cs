using System;
using System.Reactive.Linq;

namespace Simply.Reactive.Wpf
{
    internal static class Extensions
    {
        public static IObservable<T> DisposeOnNext<T>(this IObservable<T> observable) where T : IDisposable
        {
            var previous = default(T);
            return observable.Select(v =>
                {
                    if (!Utility.IsDefault(previous))
                    {
                        previous.Dispose();
                    }
                    previous = v;
                    return v;
                });
        }
    }
}

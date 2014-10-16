using System;
using System.Collections.Generic;

namespace Simply.Reactive.Wpf
{
    internal static class Utility
    {
        public static bool IsDefault<T>(T obj)
        {
            return EqualityComparer<T>.Default.Equals(obj, default(T));
        }

        public static object GetDefaultValue(Type t)
        {
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }
    }
}

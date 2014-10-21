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

        public static object GetBindingDefaultValue(Type propertyType)
        {
            // For strings return string.Empty.  MainWindow.Title hates null.
            if (propertyType == typeof(string))
                return string.Empty;

            // If the type is nullable (such as a checkbox), don't return null, return false.  Helps with type converters expecting the underlying type.
            var nullableUnderlyingType = Nullable.GetUnderlyingType(propertyType);
            var isNullable = nullableUnderlyingType != null;
            if (isNullable)
                return GetBindingDefaultValue(nullableUnderlyingType);

            return GetDefaultValue(propertyType);
        }

        public static object GetDefaultValue(Type t)
        {
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }

        public static bool IsNullable(Type t)
        {
            return Nullable.GetUnderlyingType(t) != null;
        }
    }
}

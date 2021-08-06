using System;
using System.Diagnostics.CodeAnalysis;

namespace WorkSharp.Wws.Internal
{
    static class ValidationHelper
    {
        public static void EnsureNonNull<T>(string paramName, [NotNull] T? value) where T : class
        {
            if (value == null)
                throw new InvalidOperationException(paramName + " must not be null!");
        }
    }
}
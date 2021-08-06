// Copyright 2021 WGBH Educational Foundation
// Licensed under the Apache License, Version 2.0

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
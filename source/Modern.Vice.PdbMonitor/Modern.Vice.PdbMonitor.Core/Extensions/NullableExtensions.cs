using System.Runtime.CompilerServices;

namespace System;
public static class NullableExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ValueOrThrow<T>(this Nullable<T> value, string? message = null,
        [CallerArgumentExpression("value")] string caller = "")
        where T : struct
    {
        return value ?? throw new Exception(message ?? $"{caller} can not be null");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ValueOrThrow<T>(this T? value, string? message = null,
       [CallerArgumentExpression("value")] string caller = "")
       where T : class
    {
        return value ?? throw new Exception(message ?? $"{caller} can not be null");
    }
}

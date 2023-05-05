using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Modern.Vice.PdbMonitor.Core.Common;
public class RhReferenceEqualityComparer<T> : IEqualityComparer<T?>, IEqualityComparer
    where T : class
{
    public static RhReferenceEqualityComparer<T> Instance { get; } = new RhReferenceEqualityComparer<T>();
    public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

    public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

    public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);

    public int GetHashCode(T obj) => GetHashCode((object)obj);
}

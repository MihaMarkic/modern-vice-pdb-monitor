namespace System;
public static class Extensions
{
    public static bool AreEquals<T>(this IList<T> a, IList<T> b, IEqualityComparer<T>? comparer = null)
    {
        var actualComparer = comparer ?? EqualityComparer<T>.Default;
        if (a.Count != b.Count)
        {
            return false;
        }
        for (int i = 0; i < a.Count; i++)
        {
            if (!actualComparer.Equals(a[i], b[i]))
            {
                return false;
            }
        }
        return true;
    }
}

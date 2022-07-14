using System.Text;

namespace System;

public static class StringExtension
{
    // TODO switch to pooling as thread static instances might clutter memory
    [ThreadStatic]
    readonly static StringBuilder stringBuilder = new StringBuilder(1024);
    /// <summary>
    /// Converts given string with tabs to string with spaces.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="tabWidth"></param>
    /// <returns></returns>
    /// <threadsafety>Method is thread safe.</threadsafety>
    public static string? ConvertTabsToSpaces(this string? source, int tabWidth)
{
        if (!string.IsNullOrWhiteSpace(source) && source.Contains('\t'))
        {
            stringBuilder.Clear();
            // 20 is magic number for additional space based on tabs
            foreach (char c in source)
            {
                if (c != '\t')
                {
                    stringBuilder.Append(c);
                }
                else
                {
                    int spacesCount = CalculateRequiredSpacesForTab(stringBuilder.Length, tabWidth);
                    stringBuilder.Append(' ', spacesCount);
                }
            }
            return stringBuilder.ToString();
        }
        else
        {
            return source;
        }
    }

    internal static int CalculateRequiredSpacesForTab(int position, int tabWidth)
    {
        return tabWidth - position % tabWidth;
    }
}
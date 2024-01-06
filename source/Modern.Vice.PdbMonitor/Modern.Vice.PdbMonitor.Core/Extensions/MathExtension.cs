namespace Modern.Vice.PdbMonitor.Core.Extensions;
public static class MathExtension
{
    /// <summary>
    /// Returns digits number of given input parameter.
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static int CalculateNumberOfDigits(this int number)
    {
        var result = 0;
        do
        {
            result++;
            number /= 10;
        } while (number > 0);
        return result;
    }
}

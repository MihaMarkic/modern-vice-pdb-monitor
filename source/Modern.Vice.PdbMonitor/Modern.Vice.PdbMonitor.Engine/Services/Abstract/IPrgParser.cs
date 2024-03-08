namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract;
public interface IPrgParser
{
    /// <summary>
    /// Provides SYS address.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    ushort GetStartAddress(string path);
    /// <summary>
    /// Provides first executable address.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    ushort GetEntryAddress(string path);
}

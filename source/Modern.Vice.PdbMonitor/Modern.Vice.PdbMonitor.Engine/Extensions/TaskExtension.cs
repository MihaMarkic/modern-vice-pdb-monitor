namespace System.Threading.Tasks;

public static class TaskExtension
{
    public static async Task<T> AwaitWithTimeoutAsync<T>(this Task<T> task, TimeSpan timeout, CancellationToken ct = default)
    {
        bool success = await Task.WhenAny(task, Task.Delay(timeout, ct)) == task;
        if (!success)
        {
            throw new TimeoutException();
        }
        return task.Result;
    }
}

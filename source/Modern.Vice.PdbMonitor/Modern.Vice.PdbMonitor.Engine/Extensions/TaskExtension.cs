namespace System.Threading.Tasks
{
    public static class TaskExtension
    {
        public static async Task<(bool Success, T? Result)> AwaitWithTimeout<T>(this Task<T> task, TimeSpan timeout, CancellationToken ct = default)
        {
            bool success = await Task.WhenAny(task, Task.Delay(timeout, ct)) == task;
            if (success)
            {
                return (true, task.Result);
            }
            else
            {
                return (false, default);
            }
        }
    }
}

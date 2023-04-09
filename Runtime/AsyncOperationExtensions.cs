using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenUGD.Barcode
{
    public static class AsyncOperationExtensions
    {
        public static Task<T> AsTask<T>(this T asyncOperation) where T : AsyncOperation
        {
            var taskCompletionSource = new TaskCompletionSource<T>();
            asyncOperation.completed += operation => taskCompletionSource.SetResult((T)operation);
            return taskCompletionSource.Task;
        }

        public static async Task DelayAsync(this TimeSpan timeSpan, CancellationToken cancellationToken = default)
        {
            long totalMilliseconds = (long)timeSpan.TotalMilliseconds;
            long startTime = Environment.TickCount;
            while (startTime + totalMilliseconds > Environment.TickCount)
            {
                if (!cancellationToken.IsCancellationRequested)
                    await Task.Yield();
                else break;
            }
        }
    }
}

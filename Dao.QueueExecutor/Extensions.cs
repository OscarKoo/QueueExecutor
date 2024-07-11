using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dao.QueueExecutor
{
    public static class Extensions
    {
        public static bool TestWait(this SemaphoreSlim source) => source.CurrentCount >= 1 && source.Wait(0);

        public static T MustGet<T>(this T source)
        {
            var result = source;
            if (result != null)
                return result;

            SpinWait.SpinUntil(() =>
            {
                result = source;
                return result != null;
            });
            return result;
        }

        public static void InvokeAllAsyncEvents<T>(this Func<T, Task> func, T data)
        {
            var handlers = func.GetInvocationList();
            foreach (var handler in handlers)
            {
                ((Func<T, Task>)handler)(data).GetAwaiter().GetResult();
            }
        }

        public static async Task InvokeAllEventsAsync<T>(this Func<T, Task> func, T data)
        {
            var handlers = func.GetInvocationList();
            foreach (var handler in handlers)
            {
                await ((Func<T, Task>)handler)(data).ConfigureAwait(false);
            }
        }

        public static async Task InvokeAllEventsAsync(this Func<Task> func)
        {
            var handlers = func.GetInvocationList();
            foreach (var handler in handlers)
            {
                await ((Func<Task>)handler)().ConfigureAwait(false);
            }
        }

        internal static void Bind<T>(this QueueBatchExecutor<T> executor, Func<IList<T>, Task> func, ref int bindCount)
        {
            if (Interlocked.CompareExchange(ref bindCount, 0, 0) == 0)
            {
                executor.Execute += func;
                Interlocked.Increment(ref bindCount);
            }
        }

        internal static void Unbind<T>(this QueueBatchExecutor<T> executor, Func<IList<T>, Task> func, ref int bindCount)
        {
            if (Interlocked.CompareExchange(ref bindCount, 0, 0) > 0)
            {
                if (Interlocked.CompareExchange(ref bindCount, 0, 0) == 1)
                    executor.Execute -= func;

                Interlocked.Decrement(ref bindCount);
            }
        }
    }
}
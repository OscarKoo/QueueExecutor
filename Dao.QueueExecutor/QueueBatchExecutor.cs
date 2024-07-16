using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dao.QueueExecutor
{
    public class QueueBatchExecutor<T>
    {
        readonly SemaphoreSlim locker;
        readonly QueueMode mode;
        readonly ConcurrentQueue<T> queue;
        volatile bool incoming;

        public QueueBatchExecutor(int concurrentCount = 1) : this(concurrentCount, QueueMode.Batch)
        {
        }

        internal QueueBatchExecutor(int concurrentCount, QueueMode mode)
        {
            if (concurrentCount < 1)
                concurrentCount = 1;

            this.locker = new SemaphoreSlim(concurrentCount, concurrentCount);

            this.mode = mode;
            this.queue = mode != QueueMode.None ? new ConcurrentQueue<T>() : null;
        }

        public event Func<IList<T>, Task> Execute;
        public event Action<Exception> OnException;

        public void Push(T item)
        {
            if (this.mode != QueueMode.None)
                this.queue.Enqueue(item);
            this.incoming = true;

            var enter = this.locker.TestWait();
            if (!enter)
                return;

            Task.Run(() => Entry(false));
        }

        void Entry(bool wait)
        {
            if (wait)
                this.locker.Wait();

            Dequeue();
        }

        void Dequeue()
        {
            this.incoming = false;

            try
            {
                var items = this.mode == QueueMode.Batch ? new List<T>() : null;
                var exec = Execute.MustGet();

                if (this.mode != QueueMode.None)
                {
                    while (this.queue.TryDequeue(out var item))
                    {
                        if (this.mode == QueueMode.Batch)
                        {
                            items.Add(item);
                        }
                        else
                        {
                            try
                            {
                                exec.InvokeAllAsyncEvents(new[] { item });
                            }
                            catch (Exception ex)
                            {
                                HandleException(ex);
                            }
                        }
                    }

                    if (this.mode == QueueMode.Batch && items.Count > 0)
                        exec.InvokeAllAsyncEvents(items);
                }
                else
                {
                    exec.InvokeAllAsyncEvents(null);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                this.locker.Release();
            }

            if (this.incoming)
                Entry(true);
        }

        void HandleException(Exception ex)
        {
            var onEx = OnException;
            if (onEx == null)
                return;

            try
            {
                onEx(ex);
            }
            catch (Exception)
            {
            }
        }
    }
}
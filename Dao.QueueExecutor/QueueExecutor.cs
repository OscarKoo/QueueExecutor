using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Dao.QueueExecutor
{
    public class QueueExecutor<TQueue, TResponse>
    {
        readonly ConcurrentQueue<TQueue> queue = new ConcurrentQueue<TQueue>();
        readonly SemaphoreSlim locker;
        volatile bool incoming;

        public QueueExecutor(int concurrentCount = 1)
        {
            if (concurrentCount < 1)
                concurrentCount = 1;

            this.locker = new SemaphoreSlim(concurrentCount, concurrentCount);
        }

        /// <summary>
        /// Set the executor to execute the queue.
        /// </summary>
        public event Func<TQueue, Task<TResponse>> Execute;
        /// <summary>
        /// Set the callback to handle the response of the executor
        /// </summary>
        public event Func<TQueue, TResponse, Task> Executed;
        public event Action<Exception> OnException;

        /// <summary>
        /// Push anything to the queue, and wait for at least one executor to execute.
        /// </summary>
        /// <param name="data"></param>
        public void Push(TQueue data)
        {
            this.queue.Enqueue(data);

            var enter = this.locker.TestWait();
            this.incoming = true;
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

        Func<TQueue, Task<TResponse>> GetHandler()
        {
            while (true)
            {
                var result = Execute;
                if (result != null)
                    return result;

                Thread.Sleep(15);
            }
        }

        void Dequeue()
        {
            this.incoming = false;

            Func<TQueue, Task<TResponse>> execute = null;
            Func<TQueue, TResponse, Task> executed = null;
            Action<Exception> onException = null;

            try
            {
                while (this.queue.TryDequeue(out var data))
                {
                    try
                    {
                        if (execute == null)
                            execute = GetHandler();
                        var response = execute(data).GetAwaiter().GetResult();

                        if (executed == null)
                            executed = Executed;
                        if (executed != null)
                            executed(data, response).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        if (onException == null)
                            onException = OnException;
                        if (onException != null)
                        {
                            try
                            {
                                onException(ex);
                            }
                            catch (Exception e) { }
                        }
                    }
                }
            }
            finally
            {
                this.locker.Release();
            }

            if (this.incoming)
                Entry(true);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dao.QueueExecutor
{
    public class QueueExecutor<T>
    {
        readonly QueueBatchExecutor<T> executor;

        int bindCount;
        Func<T, Task> execute;

        public QueueExecutor(int concurrentCount = 1)
        {
            if (concurrentCount < 1)
                concurrentCount = 1;

            this.executor = new QueueBatchExecutor<T>(concurrentCount, QueueMode.Single);
        }

        /// <summary>
        ///     Set the executor to execute the queue.
        /// </summary>
        public event Func<T, Task> Execute
        {
            add
            {
                this.execute += value;
                this.executor.Bind(OnExecute, ref this.bindCount);
            }
            remove
            {
                this.executor.Unbind(OnExecute, ref this.bindCount);
                this.execute -= value;
            }
        }

        public event Action<Exception> OnException
        {
            add => this.executor.OnException += value;
            remove => this.executor.OnException -= value;
        }

        async Task OnExecute(IList<T> items)
        {
            var exec = this.execute.MustGet();
            await exec.InvokeAllEventsAsync(items[0]).ConfigureAwait(false);
        }

        /// <summary>
        ///     Push anything to the queue, and wait for at least one executor to execute.
        /// </summary>
        /// <param name="item"></param>
        public void Push(T item) => this.executor.Push(item);
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dao.QueueExecutor
{
    public class Catcher
    {
        readonly QueueBatchExecutor<byte> executor;

        int bindCount;
        Func<Task> onCatch;

        public Catcher(int concurrentCount = 1)
        {
            if (concurrentCount < 1)
                concurrentCount = 1;

            this.executor = new QueueBatchExecutor<byte>(concurrentCount, QueueMode.None);
        }

        /// <summary>
        ///     Set the catcher to catch the ball.
        /// </summary>
        public event Func<Task> Catch
        {
            add
            {
                this.onCatch += value;
                this.executor.Bind(OnExecute, ref this.bindCount);
            }
            remove
            {
                this.executor.Unbind(OnExecute, ref this.bindCount);
                this.onCatch -= value;
            }
        }

        public event Action<Exception> OnException
        {
            add => this.executor.OnException += value;
            remove => this.executor.OnException -= value;
        }

        async Task OnExecute(IList<byte> data)
        {
            var exec = this.onCatch.MustGet();
            await exec.InvokeAllEventsAsync().ConfigureAwait(false);
        }

        /// <summary>
        ///     Throw a ball in any time, and there would be at least one catcher to catch the ball.
        /// </summary>
        public void Throw() => this.executor.Push(0);
    }
}
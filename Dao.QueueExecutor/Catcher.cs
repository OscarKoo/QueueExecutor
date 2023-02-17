using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dao.QueueExecutor
{
    public class Catcher
    {
        readonly SemaphoreSlim locker;
        volatile bool incoming;

        public Catcher(int concurrentCount = 1)
        {
            if (concurrentCount < 1)
                concurrentCount = 1;

            this.locker = new SemaphoreSlim(concurrentCount, concurrentCount);
        }

        /// <summary>
        /// Set the catcher to catch the ball.
        /// </summary>
        public event Func<Task> Catch;
        public event Action<Exception> OnException;

        /// <summary>
        /// Throw a ball in any time, and there would be at least one catcher to catch the ball.
        /// </summary>
        public void Throw()
        {
            this.incoming = true;
            if (this.locker.CurrentCount < 1)
                return;

            Entry(false);
        }

        void Entry(bool wait)
        {
            if (wait)
                this.locker.Wait();
            else if (!this.locker.Wait(0))
                return;

            Handle();
        }

        async Task<Func<Task>> GetHandler()
        {
            while (true)
            {
                var result = Catch;
                if (result != null)
                    return result;

                await Task.Delay(15).ConfigureAwait(false);
            }
        }

        async Task Handle()
        {
            this.incoming = false;

            try
            {
                var @catch = await GetHandler().ConfigureAwait(false);
                await @catch().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var onException = OnException;
                if (onException != null)
                {
                    try
                    {
                        onException(ex);
                    }
                    catch (Exception e) { }
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
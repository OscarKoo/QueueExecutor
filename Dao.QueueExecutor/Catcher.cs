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

            Task.Run(Handle);
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
            Func<Task> @catch = null;
            Action<Exception> onException = null;

            do
            {
                this.incoming = false;
                await this.locker.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (@catch == null)
                        @catch = await GetHandler();
                    await @catch();
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
                finally
                {
                    this.locker.Release();
                }
            } while (this.incoming);
        }
    }
}
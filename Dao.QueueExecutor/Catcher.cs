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

            Handle();
        }

        Func<Task> GetHandler()
        {
            while (true)
            {
                var result = Catch;
                if (result != null)
                    return result;

                Thread.Sleep(15);
            }
        }

        void Handle()
        {
            this.incoming = false;

            try
            {
                var @catch = GetHandler();
                @catch().GetAwaiter().GetResult();
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
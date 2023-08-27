using System.Threading;

namespace Dao.QueueExecutor
{
    public static class Extensions
    {
        public static bool TestWait(this SemaphoreSlim source) => source.CurrentCount >= 1 && source.Wait(0);
    }
}
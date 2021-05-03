using System;
using System.Threading.Tasks;

namespace Peon.Utility
{
    public static class TaskExtension
    {
        public static void SafeWait(this Task task)
        {
            try
            {
                if (!task.IsCanceled)
                    task.Wait();
            }
            catch (AggregateException errors)
            {
                errors.Handle(e => e is TaskCanceledException);
            }
        }

        public static void WaitUntil(Func<bool> pred, uint timeout, uint frequency = 25)
        {
            uint counter = 0;
            while (!pred() && counter <= timeout)
            {
                counter += frequency;
                Task.Delay((int)frequency).Wait();
            }
        }
    }
}

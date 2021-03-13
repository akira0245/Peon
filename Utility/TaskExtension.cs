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
    }
}

using System.Threading.Tasks;

namespace TestHelpers
{
    public static class TaskExtensions
    {
        public static async Task AwaitIgnoringExceptions(
            this Task task)
        {
            try
            {
                await task;
            }
            catch
            {
                /* Ignore */
            }
        }
    }
}
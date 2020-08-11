using System.Threading.Tasks;

namespace AzReplicate.Core.Extensions
{
    public static class TaskExtensions
    {
        public static void Ignore(this Task task) { }
    }
}

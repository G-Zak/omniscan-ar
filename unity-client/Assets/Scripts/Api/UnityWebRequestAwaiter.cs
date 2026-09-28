using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace OmniScan.Api
{
    public static class UnityWebRequestAwaiterExtensions
    {
        public static TaskAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
        {
            var tcs = new TaskCompletionSource<object>();
            asyncOp.completed += _ => tcs.TrySetResult(null);
            if (asyncOp.isDone)
            {
                tcs.TrySetResult(null);
            }
            return ((Task)tcs.Task).GetAwaiter();
        }
    }
}

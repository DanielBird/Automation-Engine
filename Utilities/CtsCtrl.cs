using System.Threading;

namespace Utilities
{
    public static class CtsCtrl
    {
        public static void Clear(ref CancellationTokenSource cts)
        {
            if(cts == null) return;
            cts.Cancel();
            cts.Dispose();
            cts = null; 
        }
    }
}
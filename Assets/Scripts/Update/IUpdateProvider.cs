using System.Threading;
using System.Threading.Tasks;

namespace LEADKhongPhanh.Update
{
    public interface IUpdateProvider
    {
        /// <summary>
        /// Fetches and validates the latest manifest.
        /// Unity-backed implementations must be invoked from Unity's main
        /// thread. Cancellation completes with a Cancelled result.
        /// </summary>
        Task<UpdateManifestResult> GetLatestManifestAsync(
            CancellationToken cancellationToken);
    }
}

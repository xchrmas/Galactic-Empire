// The asynchronous UniTask loop provides us with a clean, testable alternative.

using System.Threading;
using Cysharp.Threading.Tasks;

namespace GalacticEmpire.Feature.Station.Application
{
    /// <summary>Drives the resource economy — production ticks and wallet access.</summary>
    public interface IResourceService
    {
        // What the empire currently owns
        GalacticEmpire.Core.ResourceWallet GetCurrentWallet();

        // Runs until the CancellationToken fires-VContainer handles cleanup automatically
        UniTask StartProductionLoopAsync(CancellationToken ct);

        // Single tick-handy for tests and manual triggers
        void Tick();
    }
}

// Resources are shared across Fleet, Station and Battle — so this lives
// in Station.Application for now, but will move to SharedKernel when needed.

namespace GalacticEmpire.Feature.Station.Application
{
    /// <summary>How we read and write the empire's resource wallet.</summary>
    public interface IResourceRepository
    {
        // Current snapshot - never null, starts as Empty wallet
        GalacticEmpire.Core.ResourceWallet Get();

        // Call this after every production tick or purchase
        void Save(GalacticEmpire.Core.ResourceWallet wallet);
    }
}

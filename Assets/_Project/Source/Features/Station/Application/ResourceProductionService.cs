// The core of the station economy. Every built module produces resources
// on fixed interval-no MonoBehaviour no Update(), just a UniTask loop.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GalacticEmpire.Core;

namespace GalacticEmpire.Feature.Station.Application
{
    // Ticks resource production based on what modules are built on the station.
    public sealed class ResourceProductionService : IResourceService
    {
        private readonly IResourceRepository _resourceRepository;
        private readonly IStationRepository  _stationRepository;
        private readonly GameConfigSO _config;

        public ResourceProductionService(
            IResourceRepository resourceRepository,
            IStationRepository stationRepository,
            GameConfigSO config)
        {
            _resourceRepository = resourceRepository;
            _stationRepository  = stationRepository;
            _config = config;
        }

        /// <summary>What the empire has right now.</summary>
        public ResourceWallet GetCurrentWallet() => _resourceRepository.Get();

        /// <summary>
        /// Runs indefinitely until cancelled-VContainer issues a token when unloading the scene.
        /// We'll never have to worry about manual cleanup.
        /// </summary>
        public async UniTask StartProductionLoopAsync(CancellationToken ct)
        {
            GELogger.Info(LogCategory.Economy, "Production loop is running.");

            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_config.BaseProductionRate),
                    cancellationToken: ct);

                Tick();
            }

            GELogger.Info(LogCategory.Economy, "Production loop stopped cleanly.");
        }

        //One tick — ask each module what it produces and add it to the wallet.
        public void Tick()
        {
            var station = _stationRepository.Get();

            if (station == null)
            {
                return;
            }

            var wallet = _resourceRepository.Get();

            // Walk every resource type — new types in the enum are picked up automatically
            foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
            {
                float production = station.GetTotalProduction(resource);

                if (production <= 0f)
                {
                    continue;
                }

                wallet = wallet.Add(resource, production);

                GELogger.Info(LogCategory.Economy,
                    $"+{production:F1} {resource} → total {wallet.Get(resource):F1}");
            }

            _resourceRepository.Save(wallet);
        }
    }
}

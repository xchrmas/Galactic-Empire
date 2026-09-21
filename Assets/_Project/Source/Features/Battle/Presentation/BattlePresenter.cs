// Drives a real-time battle tick by tick and shows/hides BattleHUDScreen
// around it. Deliberately NOT a Renderer/Presenter pair (MASTER.md Section 5)
// - that pattern is for spatial/grid visualization (one GameObject per item).
// A battle has no spatial layout to render, only two health readouts and a
// log, so this is a Presenter without a Renderer - the same kind of
// deliberate deviation FleetManagementScreen documents for itself (a screen
// without a Renderer/Presenter pair, because fleet data isn't spatial either).
//
// Owns the UniTask tick loop the same way GameEntryPoint.RunEconomyLoopAsync
// owns the production loop - Application (BattleService/CombatTickService)
// stays synchronous and Unity-free, Presentation owns the timing.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Battle.Application;
using GalacticEmpire.Feature.Battle.Domain;
using GalacticEmpire.Feature.Fleet.Application;
using GalacticEmpire.Feature.Fleet.Domain;
using GalacticEmpire.Feature.UI.Screens;
using UnityEngine;
using VContainer;

namespace GalacticEmpire.Feature.Battle.Presentation
{
    /// <summary>Runs a battle tick by tick in real time and drives BattleHUDScreen.</summary>
    public sealed class BattlePresenter : MonoBehaviour
    {
        [SerializeField] private BattleHUDScreen _battleHUDScreen;

        // Small pause after the fight ends so the player can read the outcome
        // line before the HUD fades away - not a GameConfigSO constant since
        // it's a pure UI pacing choice, not a gameplay rule.
        [SerializeField] private float _outcomeDisplaySeconds = 2f;

        private IBattleService _battleService;
        private CombatTickService _combatTickService;
        private IEncounterService _encounterService;
        private IFleetService _fleetService;
        private GameConfigSO _config;

        private CancellationTokenSource _cts;
        private bool _battleInProgress;

        // VContainer injects this via method injection - same pattern GalaxyMapPresenter uses
        [Inject]
        public void Construct(
            IBattleService battleService,
            CombatTickService combatTickService,
            IEncounterService encounterService,
            IFleetService fleetService,
            GameConfigSO config)
        {
            _battleService = battleService;
            _combatTickService = combatTickService;
            _encounterService = encounterService;
            _fleetService = fleetService;
            _config = config;

            if (_battleHUDScreen == null)
                Debug.LogError("[BattlePresenter] BattleHUDScreen not assigned.");
        }

        /// <summary>
        /// True while a battle is being driven - callers (GalaxyMapScreen) should
        /// avoid starting a second encounter while one is already running.
        /// </summary>
        public bool IsBattleInProgress => _battleInProgress;

        /// <summary>Starts a real-time battle between the player's fleet and a sector's NPC garrison.</summary>
        public void StartEncounter(FleetEntity playerFleet, EnemyFleetEntity enemyFleet, Guid sectorId)
        {
            if (_battleInProgress)
            {
                GELogger.Warning(LogCategory.Battle, "Tried to start a battle while one is already in progress.");
                return;
            }

            RunEncounterAsync(playerFleet, enemyFleet, sectorId).Forget();
        }

        private async UniTaskVoid RunEncounterAsync(FleetEntity playerFleet, EnemyFleetEntity enemyFleet, Guid sectorId)
        {
            _battleInProgress = true;
            _cts = new CancellationTokenSource();

            var defenderFleet = enemyFleet.ToFleetEntity();
            var battle = _battleService.CreateBattle(playerFleet, defenderFleet, sectorId);

            await _battleHUDScreen.ShowAsync();
            _battleHUDScreen.SetFleetNames(playerFleet.Name, enemyFleet.Name);
            RefreshHUD(battle);
            _battleHUDScreen.AppendLogLine($"{playerFleet.Name} engages {enemyFleet.Name}!");

            try
            {
                while (!battle.IsFinished)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_config.BattleTickRate),
                        cancellationToken: _cts.Token);

                    var previous = battle;
                    battle = _combatTickService.Tick(battle);

                    LogTickDelta(previous, battle);
                    RefreshHUD(battle);
                }

                var result = _battleService.ApplyBattleResult(battle);

                // Sync from the battle's final AttackerFleet/DefenderFleet, not
                // result.WinnerFleet/LoserFleet - those are null on a Draw, but
                // the final battle state always has both fleets (destroyed or not).
                var finalPlayerFleet = battle.AttackerFleet.Id == playerFleet.Id
                    ? battle.AttackerFleet
                    : battle.DefenderFleet.Id == playerFleet.Id
                        ? battle.DefenderFleet
                        : null;

                if (finalPlayerFleet != null)
                    _fleetService.SyncFleetAfterBattle(finalPlayerFleet);

                ShowOutcome(result, playerFleet);

                if (!result.IsDraw && result.WinnerFleetId == playerFleet.Id)
                    _encounterService.ResolveEncounter(sectorId);

                await UniTask.Delay(
                    TimeSpan.FromSeconds(_outcomeDisplaySeconds),
                    cancellationToken: _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Presenter was destroyed mid-battle - nothing to persist, just exit quietly.
            }
            finally
            {
                await _battleHUDScreen.HideAsync();
                _battleInProgress = false;
            }
        }

        private void RefreshHUD(BattleEntity battle)
        {
            float attackerPercent = SafeHullPercent(battle.AttackerFleet);
            float defenderPercent = SafeHullPercent(battle.DefenderFleet);

            _battleHUDScreen.RefreshFleetStatus(
                attackerPercent, battle.AttackerFleet.ShipCount,
                defenderPercent, battle.DefenderFleet.ShipCount);
        }

        private void LogTickDelta(BattleEntity previous, BattleEntity current)
        {
            LogFleetDelta(previous.AttackerFleet, current.AttackerFleet);
            LogFleetDelta(previous.DefenderFleet, current.DefenderFleet);
        }

        private void LogFleetDelta(FleetEntity before, FleetEntity after)
        {
            if (after.ShipCount < before.ShipCount)
            {
                _battleHUDScreen.AppendLogLine($"{after.Name} lost a ship!");
                return;
            }

            float damage = before.TotalHull - after.TotalHull;
            if (damage > 0.01f)
                _battleHUDScreen.AppendLogLine($"{after.Name} took {damage:F0} damage.");
        }

        private void ShowOutcome(BattleResult result, FleetEntity playerFleet)
        {
            if (result.IsDraw)
            {
                _battleHUDScreen.AppendLogLine("Both fleets destroyed.");
                _battleHUDScreen.ShowOutcome("DRAW - both fleets destroyed");
                return;
            }

            bool playerWon = result.WinnerFleetId == playerFleet.Id;
            _battleHUDScreen.AppendLogLine(playerWon ? "Victory!" : "Defeat...");
            _battleHUDScreen.ShowOutcome(playerWon ? "VICTORY" : "DEFEAT");
        }

        private static float SafeHullPercent(FleetEntity fleet)
        {
            if (fleet.ShipCount == 0) return 0f;

            float maxHull = 0f;
            float currentHull = 0f;

            foreach (var ship in fleet.Ships)
            {
                maxHull += ship.MaxHull;
                currentHull += ship.Hull;
            }

            return maxHull > 0f ? currentHull / maxHull : 0f;
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}

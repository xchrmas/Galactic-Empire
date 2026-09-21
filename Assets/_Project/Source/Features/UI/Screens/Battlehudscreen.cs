// Battle HUD - shown automatically by BattlePresenter when a fight starts,
// hidden automatically when it ends. Unlike Galaxy/Station/Fleet overlays,
// nothing toggles this manually - the player doesn't choose to open a fight.
//
// No Renderer/Presenter spatial pattern here (see MASTER.md Section 5) -
// there's no grid/map to visualize, just two fleet health readouts and a
// log, same "ScreenBase + programmatically built VisualElements" shape
// FleetManagementScreen already uses for non-spatial data.

using GalacticEmpire.Feature.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GalacticEmpire.Feature.UI.Screens
{
    /// <summary>Live health bars and battle log for an active real-time battle.</summary>
    public sealed class BattleHUDScreen : ScreenBase
    {
        [SerializeField] private UIDocument _document;

        // How many log lines to keep on screen - oldest lines drop off the top,
        // unbounded growth would slow down ScrollView layout during a long fight.
        [SerializeField] private int _maxLogLines = 30;

        private Label _attackerName;
        private Label _attackerShipCount;
        private VisualElement _attackerHealthFill;
        private Label _defenderName;
        private Label _defenderShipCount;
        private VisualElement _defenderHealthFill;
        private ScrollView _logList;
        private Label _outcomeLabel;

        private bool _uiInitialized;

        protected override void OnShow()
        {
            InitializeUIIfNeeded();
            ClearLog();
        }

        private void InitializeUIIfNeeded()
        {
            if (_uiInitialized)
                return;

            if (_document == null)
            {
                Debug.LogError("[BattleHUDScreen] UIDocument not assigned.");
                return;
            }

            var root = _document.rootVisualElement;

            if (root == null)
            {
                Debug.LogError("[BattleHUDScreen] rootVisualElement is null. UIDocument may not be enabled yet.");
                return;
            }

            _attackerName = root.Q<Label>("attacker-name");
            _attackerShipCount = root.Q<Label>("attacker-ship-count");
            _attackerHealthFill = root.Q<VisualElement>("attacker-health-fill");
            _defenderName = root.Q<Label>("defender-name");
            _defenderShipCount = root.Q<Label>("defender-ship-count");
            _defenderHealthFill = root.Q<VisualElement>("defender-health-fill");
            _logList = root.Q<ScrollView>("battle-log");
            _outcomeLabel = root.Q<Label>("battle-outcome");

            _uiInitialized = true;
        }

        /// <summary>Sets both fleet names once when the fight starts - names don't change per tick.</summary>
        public void SetFleetNames(string attackerName, string defenderName)
        {
            if (_attackerName != null) _attackerName.text = attackerName;
            if (_defenderName != null) _defenderName.text = defenderName;
        }

        /// <summary>Refreshes health bars and ship counts - call every battle tick.</summary>
        public void RefreshFleetStatus(
            float attackerHullPercent, int attackerShipCount,
            float defenderHullPercent, int defenderShipCount)
        {
            SetHealthBar(_attackerHealthFill, attackerHullPercent);
            SetHealthBar(_defenderHealthFill, defenderHullPercent);

            if (_attackerShipCount != null) _attackerShipCount.text = $"Ships: {attackerShipCount}";
            if (_defenderShipCount != null) _defenderShipCount.text = $"Ships: {defenderShipCount}";
        }

        /// <summary>Appends one line to the battle log - call whenever BattlePresenter detects a damage event.</summary>
        public void AppendLogLine(string line)
        {
            if (_logList == null) return;

            var entry = new Label(line)
            {
                style = { fontSize = 11, color = new Color(190f / 255f, 210f / 255f, 230f / 255f), marginBottom = 2 }
            };

            _logList.Add(entry);

            while (_logList.childCount > _maxLogLines)
                _logList.RemoveAt(0);

            _logList.ScrollTo(entry);
        }

        /// <summary>Shows the final outcome line - call once when the battle finishes.</summary>
        public void ShowOutcome(string outcomeText)
        {
            if (_outcomeLabel == null) return;

            _outcomeLabel.text = outcomeText;
            _outcomeLabel.style.display = DisplayStyle.Flex;
        }

        private void ClearLog()
        {
            _logList?.Clear();

            if (_outcomeLabel != null)
                _outcomeLabel.style.display = DisplayStyle.None;
        }

        private static void SetHealthBar(VisualElement fill, float percent)
        {
            if (fill == null) return;

            percent = Mathf.Clamp01(percent);
            fill.style.width = Length.Percent(percent * 100f);

            // Green above half, amber below, red near death - quick at-a-glance read
            fill.style.backgroundColor = percent > 0.5f
                ? new Color(80f / 255f, 200f / 255f, 120f / 255f)
                : percent > 0.2f
                    ? new Color(230f / 255f, 180f / 255f, 60f / 255f)
                    : new Color(220f / 255f, 70f / 255f, 70f / 255f);
        }
    }
}

// Station builder screen - shows cell info when the player clicks a grid cell.
// Empty cells offer a list of buildable modules; occupied cells show module
// info and a remove option.

using System;
using System.Linq;
using GalacticEmpire.Feature.Station.Application;
using GalacticEmpire.Feature.Station.Domain;
using GalacticEmpire.Feature.Station.Infrastructure;
using GalacticEmpire.Feature.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GalacticEmpire.Feature.UI.Screens
{
    /// <summary>Station builder overlay - cell info panel, module list, place/remove.</summary>
    public sealed class StationBuilderScreen : ScreenBase
    {
        [SerializeField] private UIDocument _document;

        private VisualElement _cellPanel;
        private Label _cellTitle;
        private Label _cellStatus;
        private VisualElement _buildSection;
        private ScrollView _moduleList;
        private VisualElement _moduleInfoSection;
        private Label _moduleName;
        private Label _moduleProduction;
        private Button _removeButton;
        private Label _feedback;

        private IStationService _stationService;
        private StationModuleSO[] _availableModules = Array.Empty<StationModuleSO>();
        private bool _uiInitialized;

        private GridCell _selectedCell;

        // Raised after the screen finishes its show/hide fade - StationBuilderPresenter
        // uses these to move the camera to/from the station builder view
        public event Action OnShown;
        public event Action OnHidden;

        // Raised whenever a module gets placed or removed, so the renderer can redraw
        public event Action OnStationChanged;

        public void Initialize(IStationService stationService, StationModuleSO[] availableModules)
        {
            _stationService = stationService;
            _availableModules = availableModules ?? Array.Empty<StationModuleSO>();
        }

        protected override void OnShow()
        {
            InitializeUIIfNeeded();
            OnShown?.Invoke();
        }

        protected override void OnHide()
        {
            OnHidden?.Invoke();
        }

        private void InitializeUIIfNeeded()
        {
            if (_uiInitialized)
                return;

            if (_document == null)
            {
                Debug.LogError("[StationBuilderScreen] UIDocument not assigned.");
                return;
            }

            var root = _document.rootVisualElement;

            if (root == null)
            {
                Debug.LogError("[StationBuilderScreen] rootVisualElement is null. UIDocument may not be enabled yet.");
                return;
            }

            _cellPanel = root.Q<VisualElement>("cell-panel");
            _cellTitle = root.Q<Label>("cell-title");
            _cellStatus = root.Q<Label>("cell-status");
            _buildSection = root.Q<VisualElement>("build-section");
            _moduleList = root.Q<ScrollView>("module-list");
            _moduleInfoSection = root.Q<VisualElement>("module-info-section");
            _moduleName = root.Q<Label>("module-name");
            _moduleProduction = root.Q<Label>("module-production");
            _removeButton = root.Q<Button>("remove-button");
            _feedback = root.Q<Label>("station-feedback");

            if (_removeButton != null)
                _removeButton.clicked += HandleRemovePressed;

            _uiInitialized = true;
        }

        /// <summary>Call this from StationBuilderPresenter when the player clicks a grid cell.</summary>
        public void ShowCellInfo(GridCell cell)
        {
            _selectedCell = cell;

            if (_cellPanel == null)
                return;

            _cellPanel.style.display = DisplayStyle.Flex;
            _cellTitle.text = $"Cell ({cell.X},{cell.Y})";
            _feedback.text = "";

            if (cell.IsEmpty)
                ShowBuildOptions();
            else
                ShowModuleInfo(cell);
        }

        private void ShowBuildOptions()
        {
            _cellStatus.text = "Empty";
            _buildSection.style.display = DisplayStyle.Flex;
            _moduleInfoSection.style.display = DisplayStyle.None;

            _moduleList.Clear();

            foreach (var moduleConfig in _availableModules)
            {
                var button = new Button(() => HandleBuildPressed(moduleConfig))
                {
                    text = $"{moduleConfig.ModuleName} ({moduleConfig.BuildCostMetal} Metal + {moduleConfig.BuildCostEnergy} Energy)"
                };
                button.style.height = 30;
                button.style.fontSize = 11;
                button.style.marginBottom = 4;
                button.style.backgroundColor = new Color(20f / 255f, 60f / 255f, 120f / 255f, 0.8f);
                button.style.color = new Color(180f / 255f, 220f / 255f, 255f / 255f);

                _moduleList.Add(button);
            }
        }

        private void ShowModuleInfo(GridCell cell)
        {
            var station = _stationService.GetStation();
            var module = station.Modules.FirstOrDefault(m => m.Id == cell.ModuleId);

            _cellStatus.text = module != null ? module.Type.ToString() : "Occupied";
            _buildSection.style.display = DisplayStyle.None;
            _moduleInfoSection.style.display = DisplayStyle.Flex;

            if (module == null)
                return;

            _moduleName.text = module.Name;
            _moduleProduction.text = $"Production: {module.ProductionRate} {module.ProducedResource}/sec (Lv.{module.Level})";
        }

        private void HandleBuildPressed(StationModuleSO moduleConfig)
        {
            try
            {
                var entity = moduleConfig.CreateEntity();
                _stationService.PlaceModule(new PlaceModuleCommand(entity, _selectedCell.X, _selectedCell.Y));

                _feedback.text = $"{moduleConfig.ModuleName} placed.";
                OnStationChanged?.Invoke();
                _cellPanel.style.display = DisplayStyle.None;
            }
            catch (InvalidOperationException ex)
            {
                _feedback.text = ex.Message;
            }
        }

        private void HandleRemovePressed()
        {
            try
            {
                _stationService.RemoveModule(new RemoveModuleCommand(_selectedCell.X, _selectedCell.Y));

                _feedback.text = "Module removed.";
                OnStationChanged?.Invoke();
                _cellPanel.style.display = DisplayStyle.None;
            }
            catch (InvalidOperationException ex)
            {
                _feedback.text = ex.Message;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_removeButton != null)
                _removeButton.clicked -= HandleRemovePressed;
        }
    }
}
using System;
using System.Collections.Generic;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using GenieClient.Avalonia.ViewModels;

namespace GenieClient.Avalonia.Services
{
    public class GenieDockFactory : Factory
    {
        private ProportionalDock _mainLayout;
        private DocumentDock _documentDock;
        private ToolDock _rightToolDock;

        public GameDocumentViewModel MainGamePanel { get; private set; }

        /// <summary>
        /// Fired when a panel is closed by the dock framework (user clicked X on tab).
        /// </summary>
        public event Action<string> PanelClosedByUser;

        public override IRootDock CreateLayout()
        {
            MainGamePanel = new GameDocumentViewModel();

            _documentDock = new DocumentDock
            {
                Id = "DocumentsPane",
                Title = "Documents",
                Proportion = double.NaN,
                IsCollapsable = false,
                CanCreateDocument = false,
                VisibleDockables = CreateList<IDockable>(MainGamePanel)
            };
            _documentDock.ActiveDockable = MainGamePanel;

            _rightToolDock = new ToolDock
            {
                Id = "RightPane",
                Title = "Panels",
                Proportion = 0.25,
                Alignment = Alignment.Right,
                IsCollapsable = false,
                VisibleDockables = CreateList<IDockable>(),
                GripMode = GripMode.Visible
            };

            _mainLayout = new ProportionalDock
            {
                Id = "MainLayout",
                Title = "MainLayout",
                Orientation = Orientation.Horizontal,
                Proportion = double.NaN,
                VisibleDockables = CreateList<IDockable>(
                    _documentDock,
                    new ProportionalDockSplitter { Id = "MainSplitter" },
                    _rightToolDock
                )
            };

            var rootDock = new RootDock
            {
                Id = "Root",
                Title = "Root",
                IsCollapsable = false,
                VisibleDockables = CreateList<IDockable>(_mainLayout),
                ActiveDockable = _mainLayout,
                DefaultDockable = _mainLayout,
                FloatingWindowHostMode = DockFloatingWindowHostMode.Native
            };

            return rootDock;
        }

        public override void InitLayout(IDockable layout)
        {
            ContextLocator = new Dictionary<string, Func<object>>
            {
                ["main-game"] = () => MainGamePanel
            };

            HostWindowLocator = new Dictionary<string, Func<IHostWindow>>
            {
                [nameof(IDockWindow)] = () => new HostWindow()
            };

            DefaultHostWindowLocator = () => new HostWindow();

            base.InitLayout(layout);
        }

        public override void OnDockableClosed(IDockable dockable)
        {
            base.OnDockableClosed(dockable);
            if (dockable is OutputPanelViewModel panel)
                PanelClosedByUser?.Invoke(panel.WindowId);
        }

        /// <summary>
        /// Ensures the right tool dock is still part of the layout.
        /// If it was collapsed when the last panel was removed, re-add it.
        /// </summary>
        private void EnsureRightToolDock()
        {
            if (_rightToolDock.Owner != null)
                return; // still in layout

            // Recreate with fresh state
            _rightToolDock = new ToolDock
            {
                Id = "RightPane",
                Title = "Panels",
                Proportion = 0.25,
                Alignment = Alignment.Right,
                IsCollapsable = false,
                VisibleDockables = CreateList<IDockable>(),
                GripMode = GripMode.Visible
            };

            _mainLayout.VisibleDockables.Add(new ProportionalDockSplitter { Id = "MainSplitter2" });
            _mainLayout.VisibleDockables.Add(_rightToolDock);
            InitDockable(new ProportionalDockSplitter { Id = "MainSplitter2" }, _mainLayout);
            InitDockable(_rightToolDock, _mainLayout);
        }

        public OutputPanelViewModel CreateAndAddPanel(string id, string title, string ifClosed)
        {
            var panel = new OutputPanelViewModel(id, title, ifClosed);

            EnsureRightToolDock();

            AddDockable(_rightToolDock, panel);
            SetActiveDockable(panel);
            SetFocusedDockable(_rightToolDock, panel);

            return panel;
        }

        public void DetachPanel(OutputPanelViewModel panel)
        {
            if (panel == null || panel.Owner == null) return;
            RemoveDockable(panel, false);
        }
    }
}

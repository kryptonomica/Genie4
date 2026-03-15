using System;
using System.Collections.Generic;
using System.Linq;
using GenieClient.Avalonia.ViewModels;
using GenieClient.Genie;

namespace GenieClient.Avalonia.Services
{
    public class WindowManager
    {
        private readonly GenieDockFactory _factory;

        // Live panels currently in the dock tree
        private readonly Dictionary<string, OutputPanelViewModel> _visible =
            new(StringComparer.OrdinalIgnoreCase);

        // All known windows (visible + hidden) for menu and recreation
        private readonly Dictionary<string, WindowInfo> _known =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> SystemWindowIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "inv", "thoughts", "death", "room", "logons", "familiar",
            "log", "debug", "percwindow", "combat", "portrait"
        };

        public event Action WindowsChanged;

        public WindowManager(GenieDockFactory factory)
        {
            _factory = factory;
            _factory.PanelClosedByUser += OnPanelClosedByUser;
        }

        private void OnPanelClosedByUser(string windowId)
        {
            // Dock framework closed this panel (user clicked X on tab)
            _visible.Remove(windowId);
            WindowsChanged?.Invoke();
        }

        public OutputPanelViewModel GetOrCreateWindow(string id, string title, string ifClosed)
        {
            if (string.IsNullOrEmpty(id)) return null;

            // Register in known windows
            if (!_known.ContainsKey(id))
                _known[id] = new WindowInfo(id, title ?? id, ifClosed, SystemWindowIds.Contains(id));

            // Already visible — return existing
            if (_visible.TryGetValue(id, out var existing))
                return existing;

            // Create fresh panel and add to dock
            var panel = _factory.CreateAndAddPanel(id, title ?? id, ifClosed);
            panel.IsSystemWindow = SystemWindowIds.Contains(id);
            _visible[id] = panel;
            WindowsChanged?.Invoke();
            return panel;
        }

        public OutputPanelViewModel FindWindow(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _visible.TryGetValue(id, out var panel);
            return panel;
        }

        public bool IsWindowVisible(string id)
        {
            return _visible.ContainsKey(id);
        }

        public OutputPanelViewModel ResolveTarget(Game.WindowTarget target, string targetWindowString)
        {
            string id = target switch
            {
                Game.WindowTarget.Thoughts => "thoughts",
                Game.WindowTarget.Combat => "combat",
                Game.WindowTarget.Inv => IsWindowVisible("inv") ? "inv" : null,
                Game.WindowTarget.Room => IsWindowVisible("room") ? "room" : null,
                Game.WindowTarget.Death => "death",
                Game.WindowTarget.Logons => "logons",
                Game.WindowTarget.Familiar => "familiar",
                Game.WindowTarget.Log => "log",
                Game.WindowTarget.Debug => "debug",
                Game.WindowTarget.ActiveSpells => "percwindow",
                Game.WindowTarget.Portrait => "portrait",
                Game.WindowTarget.Other => targetWindowString,
                _ => null
            };

            if (id == null) return null;
            return FindWindow(id);
        }

        public OutputPanelViewModel ResolveIfClosed(string ifClosed, int depth = 0)
        {
            if (depth > 10) return null;
            if (ifClosed == null) return null;
            if (ifClosed == "") return null;

            var panel = FindWindow(ifClosed);
            if (panel != null) return panel;

            if (_known.TryGetValue(ifClosed, out var info))
                return ResolveIfClosed(info.IfClosed, depth + 1);

            return null;
        }

        public void HideWindow(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (!_visible.TryGetValue(id, out var panel)) return;

            _factory.DetachPanel(panel);
            _visible.Remove(id);
            WindowsChanged?.Invoke();
        }

        public void RemoveWindow(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_known.TryGetValue(id, out var info) && info.IsSystem) return;

            if (_visible.TryGetValue(id, out var panel))
                _factory.DetachPanel(panel);

            _visible.Remove(id);
            _known.Remove(id);
            WindowsChanged?.Invoke();
        }

        public void ClearWindow(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_visible.TryGetValue(id, out var panel))
                panel.Clear();
        }

        public IReadOnlyList<WindowInfo> GetAllWindowInfo()
        {
            return _known.Values.ToList();
        }

        public class WindowInfo
        {
            public string Id { get; }
            public string Title { get; }
            public string IfClosed { get; }
            public bool IsSystem { get; }

            public WindowInfo(string id, string title, string ifClosed, bool isSystem)
            {
                Id = id;
                Title = title;
                IfClosed = ifClosed;
                IsSystem = isSystem;
            }
        }
    }
}

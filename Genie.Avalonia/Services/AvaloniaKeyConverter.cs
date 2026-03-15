using System;
using System.Collections.Generic;
using Avalonia.Input;
using GenieClient.Genie;

namespace GenieClient.Avalonia.Services
{
    public static class AvaloniaKeyConverter
    {
        private static readonly Dictionary<Key, KeyCode.Keys> s_keyMap = new()
        {
            { Key.None, KeyCode.Keys.None },
            { Key.A, KeyCode.Keys.A }, { Key.B, KeyCode.Keys.B }, { Key.C, KeyCode.Keys.C },
            { Key.D, KeyCode.Keys.D }, { Key.E, KeyCode.Keys.E }, { Key.F, KeyCode.Keys.F },
            { Key.G, KeyCode.Keys.G }, { Key.H, KeyCode.Keys.H }, { Key.I, KeyCode.Keys.I },
            { Key.J, KeyCode.Keys.J }, { Key.K, KeyCode.Keys.K }, { Key.L, KeyCode.Keys.L },
            { Key.M, KeyCode.Keys.M }, { Key.N, KeyCode.Keys.N }, { Key.O, KeyCode.Keys.O },
            { Key.P, KeyCode.Keys.P }, { Key.Q, KeyCode.Keys.Q }, { Key.R, KeyCode.Keys.R },
            { Key.S, KeyCode.Keys.S }, { Key.T, KeyCode.Keys.T }, { Key.U, KeyCode.Keys.U },
            { Key.V, KeyCode.Keys.V }, { Key.W, KeyCode.Keys.W }, { Key.X, KeyCode.Keys.X },
            { Key.Y, KeyCode.Keys.Y }, { Key.Z, KeyCode.Keys.Z },
            { Key.D0, KeyCode.Keys.D0 }, { Key.D1, KeyCode.Keys.D1 }, { Key.D2, KeyCode.Keys.D2 },
            { Key.D3, KeyCode.Keys.D3 }, { Key.D4, KeyCode.Keys.D4 }, { Key.D5, KeyCode.Keys.D5 },
            { Key.D6, KeyCode.Keys.D6 }, { Key.D7, KeyCode.Keys.D7 }, { Key.D8, KeyCode.Keys.D8 },
            { Key.D9, KeyCode.Keys.D9 },
            { Key.F1, KeyCode.Keys.F1 }, { Key.F2, KeyCode.Keys.F2 }, { Key.F3, KeyCode.Keys.F3 },
            { Key.F4, KeyCode.Keys.F4 }, { Key.F5, KeyCode.Keys.F5 }, { Key.F6, KeyCode.Keys.F6 },
            { Key.F7, KeyCode.Keys.F7 }, { Key.F8, KeyCode.Keys.F8 }, { Key.F9, KeyCode.Keys.F9 },
            { Key.F10, KeyCode.Keys.F10 }, { Key.F11, KeyCode.Keys.F11 }, { Key.F12, KeyCode.Keys.F12 },
            { Key.Enter, KeyCode.Keys.Enter }, { Key.Escape, KeyCode.Keys.Escape },
            { Key.Space, KeyCode.Keys.Space }, { Key.Tab, KeyCode.Keys.Tab },
            { Key.Back, KeyCode.Keys.Back }, { Key.Delete, KeyCode.Keys.Delete },
            { Key.Insert, KeyCode.Keys.Insert },
            { Key.Home, KeyCode.Keys.Home }, { Key.End, KeyCode.Keys.End },
            { Key.PageUp, KeyCode.Keys.PageUp }, { Key.PageDown, KeyCode.Keys.PageDown },
            { Key.Up, KeyCode.Keys.Up }, { Key.Down, KeyCode.Keys.Down },
            { Key.Left, KeyCode.Keys.Left }, { Key.Right, KeyCode.Keys.Right },
            { Key.NumPad0, KeyCode.Keys.NumPad0 }, { Key.NumPad1, KeyCode.Keys.NumPad1 },
            { Key.NumPad2, KeyCode.Keys.NumPad2 }, { Key.NumPad3, KeyCode.Keys.NumPad3 },
            { Key.NumPad4, KeyCode.Keys.NumPad4 }, { Key.NumPad5, KeyCode.Keys.NumPad5 },
            { Key.NumPad6, KeyCode.Keys.NumPad6 }, { Key.NumPad7, KeyCode.Keys.NumPad7 },
            { Key.NumPad8, KeyCode.Keys.NumPad8 }, { Key.NumPad9, KeyCode.Keys.NumPad9 },
            { Key.Add, KeyCode.Keys.Add }, { Key.Subtract, KeyCode.Keys.Subtract },
            { Key.Multiply, KeyCode.Keys.Multiply }, { Key.Divide, KeyCode.Keys.Divide },
            { Key.Decimal, KeyCode.Keys.Decimal },
        };

        public static KeyCode.Keys ConvertString(string sHotkey)
        {
            // Parse modifier+key strings like "Control+Shift+F1"
            var result = KeyCode.Keys.None;
            var parts = sHotkey.Split('+');

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                switch (trimmed.ToLowerInvariant())
                {
                    case "control":
                    case "ctrl":
                        result |= KeyCode.Keys.Control;
                        break;
                    case "alt":
                        result |= KeyCode.Keys.Alt;
                        break;
                    case "shift":
                        result |= KeyCode.Keys.Shift;
                        break;
                    default:
                        if (Enum.TryParse<Key>(trimmed, true, out var avKey) && s_keyMap.TryGetValue(avKey, out var mapped))
                            result |= mapped;
                        else if (Enum.TryParse<KeyCode.Keys>(trimmed, true, out var directKey))
                            result |= directKey;
                        break;
                }
            }

            return result;
        }
    }
}

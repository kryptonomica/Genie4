using System;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

namespace GenieClient.Genie
{
    // WinForms-specific extension — Core defines the main KeyCode class with Keys enum,
    // StringToPortableKey, and StringToKeyConverter.
    // This file adds only the WinForms StringToKey method as a static helper.
    public static class KeyCodeWinForms
    {
        public static System.Windows.Forms.Keys StringToKey(string sHotkey)
        {
            try
            {
                return (System.Windows.Forms.Keys)Conversions.ToInteger(new KeysConverter().ConvertFromString(sHotkey));
            }
            #pragma warning disable CS0168
            catch (Exception ex) // Unfortunately there is no specific error for convert errors.
            #pragma warning restore CS0168
            {
                return default;
            }
        }
    }
}

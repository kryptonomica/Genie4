using System;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

namespace GenieClient.Genie
{
    public partial class KeyCode
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

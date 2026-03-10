using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GenieClient.Genie;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace GenieClient
{
    /// <summary>
    /// Manages script lifecycle: loading, ticking, pausing, aborting, and tracking running scripts.
    /// Extracted from FormMain to Genie.Core — no WinForms dependencies.
    /// </summary>
    public class ScriptManager
    {
        private readonly Genie.Globals m_oGlobals;
        private readonly Genie.Command m_oCommand;

        private Genie.ScriptList m_oScriptList = new Genie.ScriptList();
        private Genie.ScriptList m_oScriptListNew = new Genie.ScriptList();
        private bool m_bScriptListUpdated = false;

        public ScriptManager(Genie.Globals globals, Genie.Command command)
        {
            m_oGlobals = globals;
            m_oCommand = command;
        }

        // --- Events for UI updates (FormMain subscribes) ---

        public delegate void ScriptAddedEventHandler(Script script);
        public event ScriptAddedEventHandler EventScriptAdded;

        // Forwarded from individual Script events
        public delegate void ScriptPrintTextEventHandler(string sText, GenieColor oColor, GenieColor oBgColor);
        public event ScriptPrintTextEventHandler EventScriptPrintText;

        public delegate void ScriptPrintErrorEventHandler(string sText);
        public event ScriptPrintErrorEventHandler EventScriptPrintError;

        public delegate void ScriptSendTextEventHandler(string Text, string Script, bool ToQueue, bool DoCommand);
        public event ScriptSendTextEventHandler EventScriptSendText;

        public delegate void ScriptDebugChangedEventHandler(Script sender, int iLevel);
        public event ScriptDebugChangedEventHandler EventScriptDebugChanged;

        public delegate void ScriptStatusChangedEventHandler(Script sender, Script.ScriptState state);
        public event ScriptStatusChangedEventHandler EventScriptStatusChanged;

        // --- Public properties ---

        public Genie.ScriptList ScriptList => m_oScriptList;

        public bool ScriptListUpdated
        {
            get => m_bScriptListUpdated;
            set => m_bScriptListUpdated = value;
        }

        // --- Script lifecycle methods ---

        public void TickScripts()
        {
            if (m_oScriptList.AcquireReaderLock())
            {
                try
                {
                    foreach (Script oScript in m_oScriptList)
                        oScript.TickScript();
                }
                finally
                {
                    m_oScriptList.ReleaseReaderLock();
                }
            }
            else
            {
                Debug.Print("ScriptList Reader Lock failed in TickScripts");
            }
        }

        public void RemoveExitedScripts()
        {
            if (m_oScriptList.AcquireReaderLock())
            {
                var removeList = new List<int>();
                try
                {
                    for (var i = 0; i < m_oScriptList.Count; i++)
                    {
                        if (m_oScriptList[i].ScriptDone)
                        {
                            removeList.Add(i);
                        }
                    }
                }
                finally
                {
                    m_oScriptList.ReleaseReaderLock();
                    if (removeList.Count > 0)
                    {
                        if (m_oScriptList.AcquireWriterLock())
                        {
                            try
                            {
                                for (var i = removeList.Count - 1; i > -1; i--)
                                {
                                    m_oScriptList.RemoveAt(removeList[i]);
                                }
                            }
                            finally
                            {
                                m_oScriptList.ReleaseWriterLock();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Moves scripts from the new list to the active list.
        /// Raises EventScriptAdded for each script (FormMain adds toolbar buttons).
        /// </summary>
        public void AddScripts()
        {
            if (m_oScriptListNew.Count > 0)
            {
                try
                {
                    m_oScriptList.AcquireWriterLock();

                    try
                    {
                        m_oScriptListNew.AcquireWriterLock();

                        foreach (Script oScript in m_oScriptListNew)
                        {
                            if (!Information.IsNothing(oScript))
                            {
                                m_oScriptList.Add(oScript);

                                if (!oScript.ScriptDone)
                                {
                                    EventScriptAdded?.Invoke(oScript);
                                }

                                m_bScriptListUpdated = true;
                            }
                        }

                        m_oScriptListNew.Clear();
                    }
                    catch
                    {
                        CoreError.Error("AddScriptsInner", "Unable to acquire writer lock.");
                    }
                    finally
                    {
                        m_oScriptListNew.ReleaseWriterLock();
                    }
                }
                catch
                {
                    CoreError.Error("AddScriptsOuter", "Unable to acquire writer lock.");
                }
                finally
                {
                    m_oScriptList.ReleaseWriterLock();
                }
            }
        }

        public Script LoadScript(string sScriptName, ArrayList oArgList)
        {
            if (m_oGlobals.Config.bAbortDupeScript == true)
            {
                if (m_oScriptList.AcquireReaderLock())
                {
                    try
                    {
                        foreach (Script oThisScript in m_oScriptList)
                        {
                            if ((oThisScript.FileName ?? "") == (sScriptName ?? ""))
                            {
                                oThisScript.AbortScript();
                            }
                        }
                    }
                    finally
                    {
                        m_oScriptList.ReleaseReaderLock();
                    }
                }
                else
                {
                    Debug.Print("ScriptList Reader Lock failed in LoadScript");
                }
            }

            var argcl = m_oGlobals;
            var oScript = new Script(argcl);
            oScript.EventPrintError += OnScriptPrintError;
            oScript.EventPrintText += OnScriptPrintText;
            oScript.EventSendText += OnScriptSendText;
            oScript.EventDebugChanged += OnScriptDebugChanged;
            oScript.EventStatusChanged += OnScriptStatusChanged;
            if (oScript.LoadFile(sScriptName, oArgList) == true)
            {
                return oScript;
            }
            else
            {
                return null;
            }
        }

        public void RunScript(string sText)
        {
            try
            {
                var al = new ArrayList();
                al = Utility.ParseArgs(sText, true);
                string ScriptName = Conversions.ToString(al[0].ToString().ToLower().Trim().Substring(1));
                if (ScriptName.EndsWith($".{m_oGlobals.Config.ScriptExtension}") == false)
                {
                    ScriptName += $".{m_oGlobals.Config.ScriptExtension}";
                }

                Script oScript = null;
                if (m_oScriptListNew.AcquireWriterLock())
                {
                    try
                    {
                        oScript = LoadScript(ScriptName, al);
                        if (!Information.IsNothing(oScript))
                        {
                            m_oScriptListNew.Add(oScript);
                        }
                    }
                    finally
                    {
                        m_oScriptListNew.ReleaseWriterLock();
                    }
                }
                else
                {
                    CoreError.Error("RunScript", "Unable to acquire writer lock.");
                }

                if (!Information.IsNothing(oScript))
                {
                    oScript.RunScript();
                }
            }
            catch (Exception ex)
            {
                CoreError.Error("RunScript", ex.Message, ex.ToString());
            }
        }

        public void SetScriptListVariable()
        {
            if (m_oScriptList.AcquireReaderLock())
            {
                Debug.Print("ScriptList Lock acquired by SetScriptListVariable()");
                try
                {
                    string sScriptList = string.Empty;
                    foreach (Script oScript in m_oScriptList)
                    {
                        if (!Information.IsNothing(oScript))
                        {
                            if (!oScript.ScriptDone)
                            {
                                if (sScriptList.Length > 0)
                                    sScriptList += "|";
                                sScriptList += Path.GetFileNameWithoutExtension(oScript.FileName);
                                Debug.Print(oScript.FileName);
                            }
                        }
                    }

                    if (sScriptList.Length == 0)
                        sScriptList = "none";
                    m_oGlobals.VariableList["scriptlist"] = sScriptList;
                    m_bScriptListUpdated = false;
                }
                finally
                {
                    m_oScriptList.ReleaseReaderLock();
                    Debug.Print("ScriptList Lock released by SetScriptListVariable()");
                }
            }
        }

        // --- Script command handlers ---

        public void ScriptAbort(string sScript)
        {
            try
            {
                string sExcept = string.Empty;
                int I = sScript.ToLower().IndexOf("except ");
                if (I > 0)
                {
                    sExcept = sScript.Substring(I + 7);
                    sScript = sScript.Substring(0, I).TrimEnd();
                }

                sScript += " ";
                if (sScript.ToLower().StartsWith("all "))
                {
                    sScript = string.Empty;
                }
                else
                {
                    sScript = sScript.Trim();
                }

                if (m_oScriptList.AcquireReaderLock())
                {
                    Debug.Print("ScriptList Lock acquired by ScriptAbort()");
                    try
                    {
                        foreach (Script oScript in m_oScriptList)
                        {
                            if (oScript.ScriptName.Length > 0)
                            {
                                if (sScript.Length == 0 | (oScript.ScriptName ?? "") == (sScript ?? ""))
                                {
                                    if (sExcept.Length == 0 | (oScript.ScriptName ?? "") != (sExcept ?? ""))
                                    {
                                        oScript.AbortScript();
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        m_oScriptList.ReleaseReaderLock();
                        Debug.Print("ScriptList Lock released by ScriptAbort()");
                    }
                }
                else
                {
                    CoreError.Error("ScriptAbort", "Unable to acquire reader lock.");
                }
            }
            catch (Exception ex)
            {
                CoreError.Error("ScriptAbort", ex.Message, ex.ToString());
            }
        }

        public void ScriptPause(string sScript)
        {
            try
            {
                string sExcept = string.Empty;
                int I = sScript.ToLower().IndexOf("except ");
                if (I > 0)
                {
                    sExcept = sScript.Substring(I + 7);
                    sScript = sScript.Substring(0, I).TrimEnd();
                }

                sScript += " ";
                if (sScript.ToLower().StartsWith("all "))
                {
                    sScript = string.Empty;
                }
                else
                {
                    sScript = sScript.Trim();
                }

                if (m_oScriptList.AcquireReaderLock())
                {
                    Debug.Print("ScriptList Lock acquired by ScriptPause()");
                    try
                    {
                        foreach (Script oScript in m_oScriptList)
                        {
                            if (oScript.ScriptName.Length > 0)
                            {
                                if (sScript.Length == 0 | (oScript.ScriptName ?? "") == (sScript ?? ""))
                                {
                                    if (sExcept.Length == 0 | (oScript.ScriptName ?? "") != (sExcept ?? ""))
                                    {
                                        oScript.PauseScript();
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        m_oScriptList.ReleaseReaderLock();
                        Debug.Print("ScriptList Lock released by ScriptPause()");
                    }
                }
                else
                {
                    CoreError.Error("ScriptPause", "Unable to acquire reader lock.");
                }
            }
            catch (Exception ex)
            {
                CoreError.Error("ScriptPause", ex.Message, ex.ToString());
            }
        }

        public void ScriptPauseOrResume(string sScript)
        {
            try
            {
                string sExcept = string.Empty;
                int I = sScript.ToLower().IndexOf("except ");
                if (I > 0)
                {
                    sExcept = sScript.Substring(I + 7);
                    sScript = sScript.Substring(0, I).TrimEnd();
                }

                sScript += " ";
                if (sScript.ToLower().StartsWith("all "))
                {
                    sScript = string.Empty;
                }
                else
                {
                    sScript = sScript.Trim();
                }

                if (m_oScriptList.AcquireReaderLock())
                {
                    Debug.Print("ScriptList Lock acquired by ScriptPauseOrResume()");
                    try
                    {
                        foreach (Script oScript in m_oScriptList)
                        {
                            if (oScript.ScriptName.Length > 0)
                            {
                                if (sScript.Length == 0 | (oScript.ScriptName ?? "") == (sScript ?? ""))
                                {
                                    if (sExcept.Length == 0 | (oScript.ScriptName ?? "") != (sExcept ?? ""))
                                    {
                                        if (oScript.ScriptPaused == true)
                                        {
                                            oScript.ResumeScript();
                                        }
                                        else
                                        {
                                            oScript.PauseScript();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        m_oScriptList.ReleaseReaderLock();
                        Debug.Print("ScriptList Lock released by ScriptPauseOrResume()");
                    }
                }
                else
                {
                    CoreError.Error("ScriptPauseOrResume", "Unable to acquire reader lock.");
                }
            }
            catch (Exception ex)
            {
                CoreError.Error("ScriptPauseOrResume", ex.Message, ex.ToString());
            }
        }

        public void ScriptReload(string sScript)
        {
            try
            {
                sScript += " ";
                if (sScript.ToLower().StartsWith("all "))
                {
                    sScript = string.Empty;
                }
                else
                {
                    sScript = sScript.Trim();
                }

                if (m_oScriptList.AcquireReaderLock())
                {
                    Debug.Print("ScriptList Lock acquired by ScriptReload()");
                    try
                    {
                        foreach (Script oScript in m_oScriptList)
                        {
                            if (oScript.ScriptName.Length > 0)
                            {
                                if (sScript.Length == 0 | (oScript.ScriptName ?? "") == (sScript ?? ""))
                                {
                                    oScript.ReloadScript();
                                }
                            }
                        }
                    }
                    finally
                    {
                        m_oScriptList.ReleaseReaderLock();
                        Debug.Print("ScriptList Lock released by ScriptReload()");
                    }
                }
                else
                {
                    CoreError.Error("ScriptReload", "Unable to acquire reader lock.");
                }
            }
            catch (Exception ex)
            {
                CoreError.Error("ScriptReload", ex.Message, ex.ToString());
            }
        }

        public void ScriptResume(string sScript)
        {
            try
            {
                string sExcept = string.Empty;
                int I = sScript.ToLower().IndexOf("except ");
                if (I > 0)
                {
                    sExcept = sScript.Substring(I + 7);
                    sScript = sScript.Substring(0, I).TrimEnd();
                }

                sScript += " ";
                if (sScript.ToLower().StartsWith("all "))
                {
                    sScript = string.Empty;
                }
                else
                {
                    sScript = sScript.Trim();
                }

                if (m_oScriptList.AcquireReaderLock())
                {
                    Debug.Print("ScriptList Lock acquired by ScriptResume()");
                    try
                    {
                        foreach (Script oScript in m_oScriptList)
                        {
                            if (oScript.ScriptName.Length > 0)
                            {
                                if (sScript.Length == 0 | (oScript.ScriptName ?? "") == (sScript ?? ""))
                                {
                                    if (sExcept.Length == 0 | (oScript.ScriptName ?? "") != (sExcept ?? ""))
                                    {
                                        oScript.ResumeScript();
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        m_oScriptList.ReleaseReaderLock();
                        Debug.Print("ScriptList Lock released by ScriptResume()");
                    }
                }
                else
                {
                    CoreError.Error("ScriptResume", "Unable to acquire reader lock.");
                }
            }
            catch (Exception ex)
            {
                CoreError.Error("ScriptResume", ex.Message, ex.ToString());
            }
        }

        public void ScriptDebugLevel(int iDebugLevel, string sScript)
        {
            try
            {
                if ((sScript.ToLower() ?? "") == "all")
                {
                    sScript = string.Empty;
                }

                if (m_oScriptList.AcquireReaderLock())
                {
                    Debug.Print("ScriptList Lock acquired by ScriptDebugLevel()");
                    try
                    {
                        foreach (Script oScript in m_oScriptList)
                        {
                            if (oScript.ScriptName.Length > 0)
                            {
                                if (sScript.Length == 0 | (oScript.ScriptName ?? "") == (sScript ?? ""))
                                {
                                    oScript.DebugLevel = iDebugLevel;
                                }
                            }
                        }
                    }
                    finally
                    {
                        m_oScriptList.ReleaseReaderLock();
                        Debug.Print("ScriptList Lock released by ScriptDebugLevel()");
                    }
                }
                else
                {
                    CoreError.Error("ScriptDebugLevel", "Unable to acquire reader lock.");
                }
            }
            catch (Exception ex)
            {
                CoreError.Error("ScriptDebugLevel", ex.Message, ex.ToString());
            }
        }

        // --- Game event forwarding to scripts ---

        public void TriggerPromptForScripts()
        {
            try
            {
                if (m_oScriptList.AcquireReaderLock())
                {
                    try
                    {
                        foreach (Script oScript in m_oScriptList)
                            oScript.TriggerPrompt();
                    }
                    finally
                    {
                        m_oScriptList.ReleaseReaderLock();
                    }
                }
                else
                {
                    CoreError.Error("TriggerPrompt", "Unable to acquire reader lock.");
                }
            }
            catch (Exception ex)
            {
                CoreError.Error("TriggerPrompt", ex.Message, ex.ToString());
            }
        }

        public void TriggerMoveForScripts()
        {
            try
            {
                if (m_oScriptList.AcquireReaderLock())
                {
                    try
                    {
                        foreach (Script oScript in m_oScriptList)
                            oScript.TriggerMove();
                    }
                    finally
                    {
                        m_oScriptList.ReleaseReaderLock();
                    }
                }
                else
                {
                    CoreError.Error("TriggerMove", "Unable to acquire reader lock.");
                }
            }
            catch (Exception ex)
            {
                CoreError.Error("TriggerMove", ex.Message, ex.ToString());
            }
        }

        public void SetRoundTimeForScripts(int iTime)
        {
            if (m_oScriptList.AcquireReaderLock())
            {
                try
                {
                    foreach (Script oScript in m_oScriptList)
                        oScript.SetRoundTime(iTime);
                }
                finally
                {
                    m_oScriptList.ReleaseReaderLock();
                }
            }
            else
            {
                CoreError.Error("RoundTime", "Unable to acquire reader lock.");
            }
        }

        public void SetBufferEndForScripts()
        {
            if (m_oScriptList.AcquireReaderLock())
            {
                try
                {
                    foreach (Script oScript in m_oScriptList)
                        oScript.SetBufferEnd();
                }
                finally
                {
                    m_oScriptList.ReleaseReaderLock();
                }
            }
            else
            {
                CoreError.Error("SetBufferEnd", "Unable to acquire reader lock.");
            }
        }

        // --- Script event forwarding (Script → ScriptManager → FormMain) ---

        private void OnScriptPrintError(string sText)
        {
            EventScriptPrintError?.Invoke(sText);
        }

        private void OnScriptPrintText(string sText, GenieColor oColor, GenieColor oBgColor)
        {
            EventScriptPrintText?.Invoke(sText, oColor, oBgColor);
        }

        private void OnScriptSendText(string Text, string Script, bool ToQueue, bool DoCommand)
        {
            EventScriptSendText?.Invoke(Text, Script, ToQueue, DoCommand);
        }

        private void OnScriptDebugChanged(Script sender, int iLevel)
        {
            EventScriptDebugChanged?.Invoke(sender, iLevel);
        }

        private void OnScriptStatusChanged(Script sender, Script.ScriptState state)
        {
            m_bScriptListUpdated = true;
            EventScriptStatusChanged?.Invoke(sender, state);
        }
    }
}

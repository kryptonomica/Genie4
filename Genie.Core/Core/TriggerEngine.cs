using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using GenieClient.Genie;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace GenieClient
{
    /// <summary>
    /// Trigger matching and execution engine.
    /// Extracted from FormMain to Genie.Core — no WinForms dependencies.
    /// </summary>
    public class TriggerEngine
    {
        private readonly Genie.Globals m_oGlobals;
        private readonly Genie.Command m_oCommand;
        private readonly ScriptManager m_oScriptManager;

        private Match m_oRegMatch;

        public TriggerEngine(Genie.Globals globals, Genie.Command command, ScriptManager scriptManager)
        {
            m_oGlobals = globals;
            m_oCommand = command;
            m_oScriptManager = scriptManager;
        }

        public bool TriggersEnabled { get; set; } = true;

        /// <summary>
        /// Raised for error/debug text output. FormMain subscribes to route to AddText.
        /// Parameters: (text, windowName)
        /// </summary>
        public delegate void EchoTextEventHandler(string sText, string sWindow);
        public event EchoTextEventHandler EventEchoText;

        /// <summary>
        /// Match triggers against incoming text and forward to scripts.
        /// BufferWait: scripts wait for end of buffer before actions. False for #parse.
        /// </summary>
        public void ParseTriggers(string sText, bool bBufferWait = true)
        {
            if (TriggersEnabled == true)
            {
                if (sText.Trim().Length > 0)
                {
                    if (m_oGlobals.TriggerList.AcquireReaderLock())
                    {
                        try
                        {
                            foreach (Genie.Globals.Triggers.Trigger oTrigger in m_oGlobals.TriggerList.Values)
                            {
                                if (oTrigger.IsActive)
                                {
                                    if (oTrigger.bIsEvalTrigger == false)
                                    {
                                        if (!Information.IsNothing(oTrigger.oRegexTrigger))
                                        {
                                            m_oRegMatch = oTrigger.oRegexTrigger.Match(sText);
                                            if (m_oRegMatch.Success == true)
                                            {
                                                var RegExpArg = new Genie.Collections.ArrayList();
                                                if (m_oRegMatch.Groups.Count > 0)
                                                {
                                                    int J;
                                                    var loopTo = m_oRegMatch.Groups.Count - 1;
                                                    for (J = 1; J <= loopTo; J++)
                                                        RegExpArg.Add(m_oRegMatch.Groups[J].Value);
                                                }

                                                TriggerAction(oTrigger.sAction, RegExpArg);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            EventEchoText?.Invoke("Error in TriggerAction", "Debug");
                            EventEchoText?.Invoke("---------------------", "Debug");
                            EventEchoText?.Invoke(ex.Message, "Debug");
                            EventEchoText?.Invoke("---------------------", "Debug");
                            EventEchoText?.Invoke(ex.ToString(), "Debug");
                            EventEchoText?.Invoke("---------------------", "Debug");
                        }
                        finally
                        {
                            m_oGlobals.TriggerList.ReleaseReaderLock();
                        }
                    }
                    else
                    {
                        CoreError.Error("TriggerList", "Unable to acquire reader lock.");
                    }

                    // Scripts
                    if (m_oScriptManager.ScriptList.AcquireReaderLock())
                    {
                        try
                        {
                            foreach (Script oScript in m_oScriptManager.ScriptList)
                                oScript.TriggerParse(sText, bBufferWait);
                        }
                        catch (Exception ex)
                        {
                            EventEchoText?.Invoke("Error in TriggerParse", "Debug");
                            EventEchoText?.Invoke("---------------------", "Debug");
                            EventEchoText?.Invoke(ex.Message, "Debug");
                            EventEchoText?.Invoke("---------------------", "Debug");
                            EventEchoText?.Invoke(ex.ToString(), "Debug");
                            EventEchoText?.Invoke("---------------------", "Debug");
                        }
                        finally
                        {
                            m_oScriptManager.ScriptList.ReleaseReaderLock();
                        }
                    }
                    else
                    {
                        CoreError.Error("TriggerParse", "Unable to acquire reader lock.");
                    }
                }
            }
        }

        private void TriggerAction(string sAction, Genie.Collections.ArrayList oArgs)
        {
            if (TriggersEnabled == true)
            {
                if (sAction.Contains("$") == true)
                {
                    for (int i = 0, loopTo = m_oGlobals.Config.iArgumentCount - 1; i <= loopTo; i++)
                    {
                        if (i > oArgs.Count - 1)
                        {
                            sAction = sAction.Replace("$" + (i + 1).ToString(), "");
                        }
                        else
                        {
                            sAction = sAction.Replace("$" + (i + 1).ToString(), oArgs[i].ToString().Replace("\"", ""));
                        }
                    }

                    if (oArgs.Count > 0)
                    {
                        sAction = sAction.Replace("$0", oArgs[0].ToString().Replace("\"", ""));
                    }
                    else
                    {
                        sAction = sAction.Replace("$0", string.Empty);
                    }
                }

                try
                {
                    m_oCommand.ParseCommand(sAction, true, false, "Trigger");
                }
#pragma warning disable CS0168
                catch (Exception ex)
#pragma warning restore CS0168
                {
                    EventEchoText?.Invoke("Trigger action failed: " + sAction, "Debug");
                }
            }
        }

        /// <summary>
        /// Evaluate eval-triggers when a variable changes.
        /// Called from TriggerVariableChanged in the UI layer.
        /// </summary>
        public void EvalTriggers(string sVariableName)
        {
            if (TriggersEnabled == true)
            {
                if (m_oGlobals.TriggerList.AcquireReaderLock())
                {
                    try
                    {
                        foreach (Genie.Globals.Triggers.Trigger oTrigger in m_oGlobals.TriggerList.Values)
                        {
                            if (oTrigger.IsActive)
                            {
                                if (oTrigger.bIsEvalTrigger == true)
                                {
                                    if (oTrigger.sTrigger.Contains(sVariableName))
                                    {
                                        string s = "1";
                                        // If the command isn't an eval. Simply trigger it without checking.
                                        if ((oTrigger.sTrigger ?? "") != (sVariableName ?? ""))
                                        {
                                            string argsText = m_oGlobals.ParseGlobalVars(oTrigger.sTrigger);
                                            s = m_oCommand.Eval(argsText);
                                        }

                                        if (s.Length > 0 & (s ?? "") != "0")
                                        {
                                            TriggerAction(oTrigger.sAction, new Genie.Collections.ArrayList());
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        m_oGlobals.TriggerList.ReleaseReaderLock();
                    }
                }
                else
                {
                    CoreError.Error("TriggerList", "Unable to acquire reader lock.");
                }
            }
        }
    }
}

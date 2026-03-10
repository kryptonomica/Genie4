using System;
using GenieClient.Genie;

namespace GenieClient
{
    /// <summary>
    /// Timer tick coordination engine.
    /// Polls events, command queue, ticks scripts — all Core objects.
    /// Extracted from FormMain.TimerBgWorker_Tick.
    /// </summary>
    public class GameLoop
    {
        private readonly Genie.Globals m_oGlobals;
        private readonly Genie.Command m_oCommand;
        private readonly ScriptManager m_oScriptManager;

        public GameLoop(Genie.Globals globals, Genie.Command command, ScriptManager scriptManager)
        {
            m_oGlobals = globals;
            m_oCommand = command;
            m_oScriptManager = scriptManager;
        }

        public bool HasRoundTime => DateTime.Now < m_oGlobals.RoundTimeEnd;

        /// <summary>
        /// Raised after RunQueueCommand to flush pending text output.
        /// FormMain subscribes to call AddText("", ...) + EndUpdate().
        /// </summary>
        public event Action EventEndUpdate;

        /// <summary>
        /// Called every timer tick. Processes events, command queue, ticks scripts,
        /// and updates the script list variable.
        /// </summary>
        public void Tick()
        {
            // Process events
            string eventAction = m_oGlobals.Events.Poll();
            RunQueueCommand(eventAction);

            // Process command queue
            string sCommandQueue = m_oGlobals.CommandQueue.Poll(
                HasRoundTime,
                m_oGlobals.VariableList["webbed"].ToString() == "1",
                m_oGlobals.VariableList["stunned"].ToString() == "1");
            while (sCommandQueue.Length > 0)
            {
                RunQueueCommand(sCommandQueue);
                sCommandQueue = m_oGlobals.CommandQueue.Poll(
                    HasRoundTime,
                    m_oGlobals.VariableList["webbed"].ToString() == "1",
                    m_oGlobals.VariableList["stunned"].ToString() == "1");
            }

            // Tick scripts
            m_oScriptManager.TickScripts();
        }

        private void RunQueueCommand(string sAction)
        {
            if (sAction.Length > 0)
            {
                m_oCommand.ParseCommand(sAction, true, false, "");
                EventEndUpdate?.Invoke();
            }
        }
    }
}

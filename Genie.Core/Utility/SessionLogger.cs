using System;
using System.IO;
using System.Text;
using System.Threading;

namespace GenieClient.Genie
{
    /// <summary>
    /// Records bidirectional game session data (server→client and client→server)
    /// with timestamps for debugging, replay testing, and plugin development.
    ///
    /// Log format:
    ///   [HH:mm:ss.fff] RECV  raw server data (text with escaped control chars)
    ///   [HH:mm:ss.fff] SEND  outgoing client command
    ///   [HH:mm:ss.fff] EVENT description of lifecycle event
    ///
    /// Usage:
    ///   #recordsession start          — begin recording
    ///   #recordsession stop           — stop and close the log file
    ///   #recordsession status         — show whether recording is active
    ///
    /// Log files are written to {AppDir}/Logs/Sessions/session_{timestamp}.log
    /// </summary>
    public class SessionLogger
    {
        private static SessionLogger _instance;
        public static SessionLogger Instance => _instance ??= new SessionLogger();

        private readonly object _lock = new object();
        private StreamWriter _writer;
        private string _currentFilePath;
        private bool _isRecording;
        private DateTime _startTime;
        private long _recvBytes;
        private long _sendBytes;
        private long _lineCount;

        public bool IsRecording
        {
            get { lock (_lock) return _isRecording; }
        }

        public string CurrentFilePath
        {
            get { lock (_lock) return _currentFilePath; }
        }

        /// <summary>
        /// Start recording session data to a new log file.
        /// Returns the path to the log file.
        /// </summary>
        public string Start(string characterName = null, string gameName = null)
        {
            lock (_lock)
            {
                if (_isRecording)
                {
                    StopInternal();
                }

                string sessionDir = Path.Combine(LocalDirectory.Path, "Logs", "Sessions");
                Directory.CreateDirectory(sessionDir);

                string prefix = "session";
                if (!string.IsNullOrWhiteSpace(characterName))
                    prefix = characterName + (string.IsNullOrWhiteSpace(gameName) ? "" : "_" + gameName);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                _currentFilePath = Path.Combine(sessionDir, $"{prefix}_{timestamp}.log");

                _writer = new StreamWriter(_currentFilePath, false, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                _startTime = DateTime.Now;
                _recvBytes = 0;
                _sendBytes = 0;
                _lineCount = 0;
                _isRecording = true;

                WriteHeader();
                return _currentFilePath;
            }
        }

        /// <summary>
        /// Stop recording and close the log file.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                StopInternal();
            }
        }

        /// <summary>
        /// Log raw data received from the server.
        /// Called from Connection.ReceiveCallback with the decoded string.
        /// </summary>
        public void LogReceive(string data)
        {
            if (!_isRecording) return;
            lock (_lock)
            {
                if (!_isRecording || _writer == null) return;
                try
                {
                    _recvBytes += data.Length;
                    // Split into lines for readability, preserving empty lines
                    string escaped = EscapeControlChars(data);
                    _writer.WriteLine($"[{Timestamp()}] RECV  {escaped}");
                    _lineCount++;
                }
                catch { }
            }
        }

        /// <summary>
        /// Log raw bytes received from the server (hex dump for binary analysis).
        /// </summary>
        public void LogReceiveHex(byte[] buffer, int offset, int count)
        {
            if (!_isRecording) return;
            lock (_lock)
            {
                if (!_isRecording || _writer == null) return;
                try
                {
                    string hex = BitConverter.ToString(buffer, offset, Math.Min(count, 256));
                    _writer.WriteLine($"[{Timestamp()}] RXHEX [{count}] {hex}");
                    _lineCount++;
                }
                catch { }
            }
        }

        /// <summary>
        /// Log a command sent from the client to the server.
        /// </summary>
        public void LogSend(string data)
        {
            if (!_isRecording) return;
            lock (_lock)
            {
                if (!_isRecording || _writer == null) return;
                try
                {
                    _sendBytes += data.Length;
                    string escaped = EscapeControlChars(data);
                    _writer.WriteLine($"[{Timestamp()}] SEND  {escaped}");
                    _lineCount++;
                }
                catch { }
            }
        }

        /// <summary>
        /// Log a lifecycle event (connect, disconnect, state change, etc.)
        /// </summary>
        public void LogEvent(string description)
        {
            if (!_isRecording) return;
            lock (_lock)
            {
                if (!_isRecording || _writer == null) return;
                try
                {
                    _writer.WriteLine($"[{Timestamp()}] EVENT {description}");
                    _lineCount++;
                }
                catch { }
            }
        }

        /// <summary>
        /// Log a parsed row after the Connection layer has assembled it.
        /// This shows complete lines as the Game parser sees them.
        /// </summary>
        public void LogParsedRow(string row)
        {
            if (!_isRecording) return;
            lock (_lock)
            {
                if (!_isRecording || _writer == null) return;
                try
                {
                    string escaped = EscapeControlChars(row);
                    _writer.WriteLine($"[{Timestamp()}] PARSE {escaped}");
                    _lineCount++;
                }
                catch { }
            }
        }

        /// <summary>
        /// Returns a status string for display to the user.
        /// </summary>
        public string GetStatus()
        {
            lock (_lock)
            {
                if (!_isRecording)
                    return "Session recording is OFF.";

                var elapsed = DateTime.Now - _startTime;
                return $"Session recording is ON — {elapsed.Minutes}m {elapsed.Seconds}s, " +
                       $"{_lineCount} entries, recv={_recvBytes} bytes, sent={_sendBytes} bytes" +
                       Environment.NewLine + $"File: {_currentFilePath}";
            }
        }

        // --- Private helpers ---

        private void WriteHeader()
        {
            _writer.WriteLine($"# Genie Session Log");
            _writer.WriteLine($"# Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            _writer.WriteLine($"# App Version: {LocalDirectory.ApplicationVersion}");
            _writer.WriteLine($"#");
            _writer.WriteLine($"# Format: [timestamp] DIRECTION data");
            _writer.WriteLine($"#   RECV  = raw data from server (control chars escaped)");
            _writer.WriteLine($"#   RXHEX = hex dump of raw bytes from server");
            _writer.WriteLine($"#   SEND  = command sent to server");
            _writer.WriteLine($"#   PARSE = complete parsed line as seen by Game parser");
            _writer.WriteLine($"#   EVENT = lifecycle event (connect, disconnect, etc.)");
            _writer.WriteLine($"#");
            _writer.WriteLine($"# Escape notation: \\r=CR \\n=LF \\t=TAB \\xNN=hex byte");
            _writer.WriteLine($"# ---------------------------------------------------");
            _writer.WriteLine();
        }

        private void StopInternal()
        {
            if (!_isRecording) return;
            try
            {
                var elapsed = DateTime.Now - _startTime;
                _writer?.WriteLine();
                _writer?.WriteLine($"# ---------------------------------------------------");
                _writer?.WriteLine($"# Stopped: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                _writer?.WriteLine($"# Duration: {elapsed}");
                _writer?.WriteLine($"# Entries: {_lineCount}, Recv: {_recvBytes} bytes, Sent: {_sendBytes} bytes");
                _writer?.Close();
            }
            catch { }
            _writer = null;
            _isRecording = false;
        }

        private static string Timestamp()
        {
            return DateTime.Now.ToString("HH:mm:ss.fff");
        }

        /// <summary>
        /// Escape control characters for readable log output.
        /// Preserves printable ASCII and common unicode; escapes CR, LF, TAB, etc.
        /// </summary>
        private static string EscapeControlChars(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var sb = new StringBuilder(input.Length + 32);
            foreach (char c in input)
            {
                switch (c)
                {
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\a': sb.Append("\\a"); break;
                    case '\0': sb.Append("\\0"); break;
                    default:
                        if (c < 0x20 || c == 0x7F)
                            sb.Append($"\\x{(int)c:X2}");
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}

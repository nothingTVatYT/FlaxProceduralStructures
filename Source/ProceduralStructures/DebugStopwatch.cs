using System;
using System.Diagnostics;

namespace Game.ProceduralStructures {
    public class DebugStopwatch {
        private readonly Stopwatch _stopwatch = new();
        private string _purpose = "<unknown>";

        public DebugStopwatch Start(string purpose) {
            _purpose = purpose;
            _stopwatch.Reset();
            _stopwatch.Start();
            return this;
        }

        public DebugStopwatch Stop() {
            _stopwatch.Stop();
            return this;
        }

        public override string ToString() {
            var ts = _stopwatch.Elapsed;
            return $"{_purpose}: {ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
        }
    }
}
using System.Collections.Concurrent;
using System;

namespace Ava.Services
{
    public class DuplicateSuppressorService
    {
        private readonly ConcurrentDictionary<string, DateTime> _lastSeenTimes = new();
        private readonly int _windowSeconds;

        public DuplicateSuppressorService(int windowSeconds)
        {
            _windowSeconds = windowSeconds;
        }

        public bool IsDuplicate(string vrm, int laneId, int direction)
        {
            var key = $"{vrm}_{laneId}_{direction}";
            var now = DateTime.UtcNow;

            if (_lastSeenTimes.TryGetValue(key, out var lastSeen))
            {
                if ((now - lastSeen).TotalSeconds < _windowSeconds)
                {
                    return true; // Duplicate within window
                }
            }

            // Update last seen time
            _lastSeenTimes[key] = now;
            return false;
        }
    }
}

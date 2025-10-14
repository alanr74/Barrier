using System.Collections.Concurrent;
using System;
using System.Collections.Generic;

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

            // Evict entries older than 1 day to prevent cache from growing indefinitely
            var keysToRemove = new List<string>();
            foreach (var kvp in _lastSeenTimes)
            {
                if ((now - kvp.Value).TotalDays > 1)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var oldKey in keysToRemove)
            {
                _lastSeenTimes.TryRemove(oldKey, out _);
            }

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

        public void ClearCache()
        {
            _lastSeenTimes.Clear();
        }
    }
}

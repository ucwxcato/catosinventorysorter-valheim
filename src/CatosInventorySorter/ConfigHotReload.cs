using System;
using System.IO;
using BepInEx.Configuration;
using UnityEngine;

namespace CatosInventorySorter
{
    internal static class ConfigHotReload
    {
        private static ConfigFile _config;
        private static DateTime _loadedWriteTime;
        private static DateTime _candidateWriteTime;
        private static float _candidateSince;
        private static float _nextPoll;
        private static float _nextWarning;
        private static bool _pendingChange;

        internal static void Initialize(ConfigFile config)
        {
            _config = config;
            _loadedWriteTime = ReadWriteTime();
            _candidateWriteTime = _loadedWriteTime;
        }

        internal static void Update()
        {
            if (_config == null || Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 0.4f;

            try
            {
                DateTime currentWriteTime = ReadWriteTime();
                if (currentWriteTime == _loadedWriteTime)
                {
                    _candidateWriteTime = currentWriteTime;
                    _candidateSince = 0f;
                    _pendingChange = false;
                    return;
                }

                if (currentWriteTime != _candidateWriteTime)
                {
                    _candidateWriteTime = currentWriteTime;
                    _candidateSince = Time.unscaledTime;
                    _pendingChange = true;
                    return;
                }

                // Wait for editors that save by writing or replacing the file in stages.
                if (!_pendingChange || Time.unscaledTime - _candidateSince < 0.8f) return;

                _config.Reload();
                _loadedWriteTime = ReadWriteTime();
                _candidateWriteTime = _loadedWriteTime;
                _candidateSince = 0f;
                _pendingChange = false;
                Plugin.Log?.LogInfo("Configuration file changed; settings reloaded.");
            }
            catch (Exception ex)
            {
                if (Time.unscaledTime >= _nextWarning)
                {
                    _nextWarning = Time.unscaledTime + 5f;
                    Plugin.Log?.LogWarning($"Could not hot-reload configuration: {ex.Message}");
                }
            }
        }

        private static DateTime ReadWriteTime()
        {
            string path = _config.ConfigFilePath;
            return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        }
    }
}

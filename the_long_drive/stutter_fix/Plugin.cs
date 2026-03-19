using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StutterFix
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class StutterFixPlugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;

        private ConfigEntry<RigidbodyInterpolation> _interpolationMode;
        private ConfigEntry<float>                  _scanInterval;

        private void Awake()
        {
            Log = Logger;

            _interpolationMode = Config.Bind(
                "General",
                "InterpolationMode",
                RigidbodyInterpolation.Interpolate,
                "Interpolation mode applied to every Rigidbody.\n" +
                "  None        – disabled (vanilla)\n" +
                "  Interpolate – smooth between the last two physics frames (recommended)\n" +
                "  Extrapolate – predict the next position (can overshoot)"
            );

            _scanInterval = Config.Bind(
                "General",
                "ScanInterval",
                2f,
                new ConfigDescription(
                    "How often (seconds) to re-scan the scene for new Rigidbodies.",
                    new AcceptableValueRange<float>(0.1f, 60f)
                )
            );

            // Scan whenever a scene finishes loading
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Scan the current scene immediately
            ApplyToAll();

            // Periodic scan to catch runtime-spawned Rigidbodies
            StartCoroutine(PeriodicScan());

            Log.LogInfo($"[StutterFix] Loaded. Mode={_interpolationMode.Value}, ScanInterval={_scanInterval.Value}s");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.LogInfo($"[StutterFix] Scene loaded: {scene.name} — scanning Rigidbodies.");
            ApplyToAll();
        }

        private void ApplyToAll()
        {
            var rbs = FindObjectsOfType<Rigidbody>();
            foreach (var rb in rbs)
                rb.interpolation = _interpolationMode.Value;
            Log.LogInfo($"[StutterFix] Applied {_interpolationMode.Value} to {rbs.Length} Rigidbody(s).");
        }

        private IEnumerator PeriodicScan()
        {
            var wait = new WaitForSeconds(_scanInterval.Value);
            while (true)
            {
                yield return wait;
                ApplyToAll();
            }
        }
    }

    internal static class PluginInfo
    {
        public const string PLUGIN_GUID    = "com.BPplays.stutter_fix";
        public const string PLUGIN_NAME    = "stutter_fix";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}

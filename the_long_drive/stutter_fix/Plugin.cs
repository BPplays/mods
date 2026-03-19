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

            SceneManager.sceneLoaded += OnSceneLoaded;

            Log.LogInfo($"[StutterFix] Awake. Mode={_interpolationMode.Value}, ScanInterval={_scanInterval.Value}s");
        }

        // Start() is called after all Awake()s — scene is more likely populated here
        private void Start()
        {
            Log.LogInfo("[StutterFix] Start — launching coroutines.");
            StartCoroutine(DelayedInitialScan());
            StartCoroutine(PeriodicScan());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.LogInfo($"[StutterFix] Scene loaded: '{scene.name}' ({mode}) — scanning.");
            // Wait a frame so the scene's objects have a chance to initialise
            StartCoroutine(ScanNextFrame());
        }

        // Waits one frame then scans — gives newly loaded objects time to Awake/Start
        private IEnumerator DelayedInitialScan()
        {
            yield return null; // one frame
            Log.LogInfo("[StutterFix] Initial delayed scan.");
            ApplyToAll();
        }

        private IEnumerator ScanNextFrame()
        {
            yield return null;
            ApplyToAll();
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

        private void ApplyToAll()
        {
            // includeInactive: true catches Rigidbodies on disabled GameObjects too
            var rbs = FindObjectsOfType<Rigidbody>(true);
            foreach (var rb in rbs)
                rb.interpolation = _interpolationMode.Value;
            Log.LogInfo($"[StutterFix] Applied {_interpolationMode.Value} to {rbs.Length} Rigidbody(s).");
        }
    }

    internal static class PluginInfo
    {
        public const string PLUGIN_GUID    = "com.BPplays.stutter_fix";
        public const string PLUGIN_NAME    = "stutter_fix";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}

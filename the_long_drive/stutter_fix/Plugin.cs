using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace stutter_fix
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class RigidbodyInterpolationPlugin : BaseUnityPlugin {
        public static ManualLogSource Log;
        internal static RigidbodyInterpolation InstanceMode => Instance._interpolationMode.Value;

        // Config entries
        private ConfigEntry<RigidbodyInterpolation> _interpolationMode;
        private ConfigEntry<float>                  _scanInterval;

        private Harmony _harmony;

        private void Awake() {
            _instance = this;
            Log = Logger;

            // ── Configuration ──────────────────────────────────────────────
            _interpolationMode = Config.Bind(
                "General",
                "InterpolationMode",
                UnityEngine.RigidbodyInterpolation.Interpolate,
                "Interpolation mode applied to every Rigidbody.\n" +
                "  None        – disabled (vanilla)\n" +
                "  Interpolate – smooth between the last two physics frames (recommended)\n" +
                "  Extrapolate – predict the next position (can overshoot)"
            );

            _scanInterval = Config.Bind(
                "General",
                "ScanInterval",
                5f,
                new ConfigDescription(
                    "How often (seconds) to re-scan the scene for new Rigidbodies. " +
                    "Set to 0 to disable periodic scanning (rely on the Awake patch only).",
                    new AcceptableValueRange<float>(0f, 60f)
                )
            );

            // ── Harmony patch ───────────────────────────────────────────────
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll();
            SceneManager.sceneLoaded += OnSceneLoaded;

            // ── Initial scene scan ──────────────────────────────────────────
            ApplyToAll();

            // ── Periodic re-scan ────────────────────────────────────────────
            if (_scanInterval.Value > 0f)
                StartCoroutine(PeriodicScan());

            Log.LogInfo($"[RigidbodyInterpolation] Plugin loaded. " +
                        $"Mode={_interpolationMode.Value}, " +
                        $"ScanInterval={_scanInterval.Value}s, ");
        }

        private void OnDestroy() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _harmony?.UnpatchSelf();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            ApplyToAll();
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        /// <summary>Applies the configured interpolation mode to every Rigidbody currently in the scene.</summary>
        internal static void ApplyToAll() {
            var rbs = FindObjectsOfType<Rigidbody>();
            int count = 0;
            foreach (var rb in rbs)
            {
                Apply(rb);
                count++;
            }
            Log.LogInfo($"[RigidbodyInterpolation] Applied to {count} existing Rigidbody(s).");
        }

        /// <summary>Applies the configured interpolation mode to a single Rigidbody.</summary>
        internal static void Apply(Rigidbody rb)
        {
            if (rb == null) return;
            rb.interpolation = Instance._interpolationMode.Value;
        }

        // Singleton reference used by the static patch
        private static RigidbodyInterpolationPlugin _instance;
        private static RigidbodyInterpolationPlugin Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<RigidbodyInterpolationPlugin>();
                return _instance;
            }
        }

        private IEnumerator PeriodicScan()
        {
            var wait = new WaitForSeconds(_scanInterval.Value);
            while (true) {
                yield return wait;
                ApplyToAll();
            }
        }
    }

    // ── Harmony patch – runs after every Rigidbody.Awake() ──────────────────
    [HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.Instantiate))]
    internal static class InstantiatePatch {
        [HarmonyPostfix]
        private static void Postfix(object __result) {
            RigidbodyInterpolationPlugin.Log.LogInfo($"Instantiate hit: {__result?.GetType()}");
            if (__result == null)
                return;

            if (__result is GameObject go)
            {
                foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
                    RigidbodyInterpolationPlugin.Apply(rb);
            }
            else if (__result is Rigidbody rb)
            {
                RigidbodyInterpolationPlugin.Apply(rb);
            }
            else if (__result is Component c)
            {
                foreach (var childRb in c.GetComponentsInChildren<Rigidbody>(true))
                    RigidbodyInterpolationPlugin.Apply(childRb);
            }
        }
    }
    [HarmonyPatch(typeof(Rigidbody), "set_interpolation")]
    internal static class RigidbodySetterPatch {
        [HarmonyPostfix]
        private static void Postfix(Rigidbody __instance) {
            __instance.interpolation = RigidbodyInterpolationPlugin.InstanceMode;
            RigidbodyInterpolationPlugin.Log.LogInfo($"set interpolation mode to: {__instance.interpolation}");
        }
    }

    // ── Plugin metadata ──────────────────────────────────────────────────────
    internal static class PluginInfo
    {
        public const string PLUGIN_GUID    = "com.BPplays.stutter_fix";
        public const string PLUGIN_NAME    = "stutter_fix";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}

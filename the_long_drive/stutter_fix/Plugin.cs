using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace stutter_fix
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class RigidbodyInterpolationPlugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;

        public ConfigEntry<RigidbodyInterpolation> _interpolationMode;
        public ConfigEntry<float> _scanInterval;

        private Harmony _harmony;
        private static RigidbodyInterpolationPlugin _instance;
        private static RigidbodyInterpolationPlugin Instance => _instance;

        internal static RigidbodyInterpolation InstanceMode => Instance._interpolationMode.Value;
        // private float _nextScanTime;

        private void Awake()
        {
            _instance = this;
            Log = Logger;

            Log.LogInfo("[RigidbodyInterpolation] Awake entered.");

            var go = new GameObject("RigidbodyInterpolationUpdater");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<Updater>();

            _interpolationMode = Config.Bind(
                "General",
                "InterpolationMode",
                RigidbodyInterpolation.Interpolate,
                "Interpolation mode applied to every Rigidbody."
            );

            _scanInterval = Config.Bind(
                "General",
                "ScanInterval",
                5f,
                new ConfigDescription(
                    "How often (seconds) to re-scan the scene for new Rigidbodies.",
                    new AcceptableValueRange<float>(0f, 60f)
                )
            );

            // if (_scanInterval.Value > 0f)
            //     _nextScanTime = Time.unscaledTime + _scanInterval.Value;
                // Log.LogInfo($"[RigidbodyInterpolation] starting coroutine");
                // StartCoroutine(PeriodicScan());

            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll();
            SceneManager.sceneLoaded += OnSceneLoaded;

            ApplyToAll();


            Log.LogInfo($"[RigidbodyInterpolation] Plugin loaded. Mode={_interpolationMode.Value}, ScanInterval={_scanInterval.Value}s");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _harmony?.UnpatchSelf();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyToAll();
        }

        internal static void ApplyToAll() {
            Log.LogInfo("[RigidbodyInterpolation] Scan tick.");
            var rbs = FindObjectsOfType<Rigidbody>();
            int count = 0;

            foreach (var rb in rbs)
            {
                Apply(rb);
                count++;
            }

            Log.LogInfo($"[RigidbodyInterpolation] Applied to {count} existing Rigidbody(s).");
        }

        internal static void Apply(Rigidbody rb)
        {
            if (rb == null) return;
            rb.interpolation = InstanceMode;
        }

        private IEnumerator PeriodicScan()
        {
            while (true)
            {
                yield return new WaitForSeconds(_scanInterval.Value);
                ApplyToAll();
            }
        }
    }

    internal class Updater : MonoBehaviour {
        private float _nextScanTime;
        private bool doneFirstScanLog = false;
        private RigidbodyInterpolationPlugin _plugin;

        public ConfigEntry<RigidbodyInterpolation> _interpolationMode;
        public ConfigEntry<float> _scanInterval;

        private void Start()
        {
            RigidbodyInterpolationPlugin.Log.LogInfo("[Updater] Started");
            _nextScanTime = Time.unscaledTime + RigidbodyInterpolationPlugin.InstanceMode switch
            {
                _ => 0f
            };
        }

        private void Update() {
            var plugin = GetPlugin();
            if (plugin == null) {
                RigidbodyInterpolationPlugin.Log.LogInfo("[Updater] null plugin in update");
                return;
            }

            if (plugin._scanInterval.Value <= 0f) {
                if (!doneFirstScanLog) {
                    RigidbodyInterpolationPlugin.Log.LogInfo("[Updater] scan disabled");
                }
                return;
            }

            if (Time.unscaledTime < _nextScanTime) {
                return;
            }

            _nextScanTime = Time.unscaledTime + plugin._scanInterval.Value;

            RigidbodyInterpolationPlugin.Log.LogInfo("[Updater] Scan tick");
            RigidbodyInterpolationPlugin.ApplyToAll();
        }

        private RigidbodyInterpolationPlugin GetPlugin() {
            if (_plugin == null) {
                _plugin = FindObjectOfType<RigidbodyInterpolationPlugin>();
            }
            return _plugin;
        }
    }

    [HarmonyPatch]
    internal static class InstantiatePatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            return AccessTools.GetDeclaredMethods(typeof(UnityEngine.Object))
                .Where(m => m.Name == nameof(UnityEngine.Object.Instantiate));
        }

        [HarmonyPostfix]
        private static void Postfix(object __result)
        {
            RigidbodyInterpolationPlugin.Log?.LogInfo($"Instantiate hit: {__result?.GetType()}");

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

    [HarmonyPatch(typeof(Rigidbody), nameof(Rigidbody.interpolation), MethodType.Setter)]
    internal static class RigidbodySetterPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref RigidbodyInterpolation value)
        {
            value = RigidbodyInterpolationPlugin.InstanceMode;
        }
    }

    internal static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.BPplays.stutter_fix";
        public const string PLUGIN_NAME = "stutter_fix";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}

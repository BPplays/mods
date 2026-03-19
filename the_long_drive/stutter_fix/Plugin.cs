using System;
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
using UnityEngine.XR;

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

        // Resolved once, reused every scan
        internal static Type FpsControllerType { get; private set; }

        private void Awake()
        {
            _instance = this;
            Log = Logger;

            Log.LogInfo("[RigidbodyInterpolation] Awake entered.");

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

            // Resolve fpscontroller type now — all game assemblies are loaded at this point
            FpsControllerType = AccessTools.TypeByName("fpscontroller");
            if (FpsControllerType == null)
                Log.LogWarning("[RigidbodyInterpolation] Could not resolve type 'fpscontroller' — CameraLateSync will not attach. Check the exact class name.");
            else
                Log.LogInfo($"[RigidbodyInterpolation] Resolved fpscontroller type: {FpsControllerType.FullName}");

            var go = new GameObject("RigidbodyInterpolationUpdater");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<Updater>();

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
            TryAttachCameraLateSync();
        }

        internal static void ApplyToAll()
        {
            // var rbs = FindObjectsOfType<Rigidbody>(true);  // setting to true searches for inactive which finds gun and isn't tagged kinematics
            var rbs = FindObjectsOfType<Rigidbody>();
            foreach (var rb in rbs)
                Apply(rb);
        }

        internal static void Apply(Rigidbody rb)
        {
            if (rb == null) return;
            if (rb.isKinematic) {
                rb.interpolation = RigidbodyInterpolation.None;
                return;
            }
            rb.interpolation = InstanceMode;
        }

        /// <summary>
        /// Finds all live fpscontroller instances and attaches CameraLateSync
        /// if not already present. Safe to call repeatedly.
        /// </summary>
        internal static void TryAttachCameraLateSync()
        {
            if (FpsControllerType == null) return;

            // FindObjectsOfType with a runtime Type overload
            var instances = FindObjectsOfType(FpsControllerType);
            foreach (var obj in instances)
            {
                var mb = obj as MonoBehaviour;
                if (mb == null) continue;

                if (mb.GetComponent<CameraLateSync>() != null) continue; // already attached

                Log.LogInfo($"[TryAttachCameraLateSync] Attaching CameraLateSync to {mb.name}");
                var sync = mb.gameObject.AddComponent<CameraLateSync>();
                sync.Init(mb);
            }
        }
    }

    internal class Updater : MonoBehaviour
    {
        private float _nextScanTime;
        public float _scanInterval = 5.0f;

        private void Start()
        {
            RigidbodyInterpolationPlugin.Log.LogInfo("[Updater] Started");
            _nextScanTime = Time.unscaledTime + _scanInterval;

            // Try immediately on first Start in case scene already has an fpscontroller
            RigidbodyInterpolationPlugin.TryAttachCameraLateSync();
        }

        private void Update()
        {
            if (_scanInterval <= -0.5f) return;
            if (Time.unscaledTime < _nextScanTime) return;

            _nextScanTime = Time.unscaledTime + _scanInterval;
            RigidbodyInterpolationPlugin.ApplyToAll();
            RigidbodyInterpolationPlugin.TryAttachCameraLateSync();
        }
    }

    internal class CameraLateSync : MonoBehaviour
    {
        private MonoBehaviour _fps;

        private FieldInfo _fTh;
        private FieldInfo _fCamParent;
        private FieldInfo _fBsitting;

        private bool _ready;

        private Vector3 _prevThPos;
        private Vector3 _currThPos;
        private bool _haveSamples;

        public void Init(MonoBehaviour fps)
        {
            _fps = fps;
            var t = fps.GetType();

            _fTh        = AccessTools.Field(t, "Th");
            _fCamParent = AccessTools.Field(t, "CamParent");
            _fBsitting  = AccessTools.Field(t, "Bsitting");

            foreach (var (name, fi) in new[]
                    {
                    ("Th",        _fTh),
                    ("CamParent", _fCamParent),
                    ("Bsitting",  _fBsitting),
                    })
            {
                if (fi == null)
                    RigidbodyInterpolationPlugin.Log.LogWarning(
                            $"[CameraLateSync] Could not find field '{name}' on {t.Name}");
            }

            _ready = _fTh != null && _fCamParent != null;

            RigidbodyInterpolationPlugin.Log.LogInfo(
                    $"[CameraLateSync] Init complete on '{fps.name}', ready={_ready}");
        }

        private void FixedUpdate()
        {
            if (!_ready || _fps == null) return;

            var Th = _fTh.GetValue(_fps) as Transform;
            if (Th == null) return;

            if (!_haveSamples)
            {
                _prevThPos = Th.position;
                _currThPos = Th.position;
                _haveSamples = true;
                return;
            }

            _prevThPos = _currThPos;
            _currThPos = Th.position;
        }

        private void LateUpdate()
        {
            if (!_ready || _fps == null) return;
            if (XRSettings.enabled) return;
            if (!_haveSamples) return;

            var Th        = _fTh.GetValue(_fps) as Transform;
            var CamParent = _fCamParent.GetValue(_fps) as Transform;

            if (Th == null || CamParent == null)
            {
                RigidbodyInterpolationPlugin.Log.LogInfo("[CameraLateSync] skipping late update");
                return;
            }

            float alpha = 0f;
            if (Time.fixedDeltaTime > 0f)
                alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);

            // Interpolated physics position only.
            Vector3 interpolatedThPos = Vector3.Lerp(_prevThPos, _currThPos, alpha);

            // Apply the interpolated world position for rendering.
            Th.position = interpolatedThPos;

            // Keep the camera local offset clean if it is parented to Th.
            if (CamParent.parent == Th && CamParent.localPosition != Vector3.zero)
                CamParent.localPosition = Vector3.zero;
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
            if (__result == null) return;

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
        private static void Prefix(Rigidbody __instance, ref RigidbodyInterpolation value)
        {
            if (value != RigidbodyInterpolationPlugin.InstanceMode)
                RigidbodyInterpolationPlugin.Log.LogInfo(
                    $"[SetterPatch] Blocked {__instance.name} being set to {value}");
            value = RigidbodyInterpolationPlugin.InstanceMode;
        }
    }

    [HarmonyPatch(typeof(Rigidbody), "OnEnable")]
    internal static class RigidbodyOnEnablePatch
    {
        [HarmonyPostfix]
        private static void Postfix(Rigidbody __instance)
        {
            RigidbodyInterpolationPlugin.Apply(__instance);
        }
    }

    internal static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.BPplays.stutter_fix";
        public const string PLUGIN_NAME = "stutter_fix";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}

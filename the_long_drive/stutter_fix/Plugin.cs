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

            var updater = go.AddComponent<Updater>();
            // updater._scanInterval = _scanInterval.Value;
            // updater._interpolationMode = _interpolationMode.Value;

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
            // Log.LogInfo("[RigidbodyInterpolation] Scan tick.");
            // var rbs = FindObjectsOfType<Rigidbody>();
            var rbs = FindObjectsOfType<Rigidbody>(true);
            int count = 0;

            foreach (var rb in rbs)
            {
                Apply(rb);
                count++;
            }

            // Log.LogInfo($"[RigidbodyInterpolation] Applied to {count} existing Rigidbody(s).");
        }

        internal static void Apply(Rigidbody rb)
        {
            if (rb == null) return;

            // // Skip camera rigs
            // if (rb.CompareTag("MainCamera") || rb.GetComponentInChildren<Camera>() != null) {
            //     rb.interpolation = RigidbodyInterpolation.None;
            //     return;
            // }


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

        // private static Updater _instance;
        // private static Updater Instance => _instance;
        //
        // internal static RigidbodyInterpolation InstanceMode => Instance._interpolationMode.Value;

        public RigidbodyInterpolation _interpolationMode = RigidbodyInterpolation.Interpolate;
        public float _scanInterval = 5.0f;

        private void Start()
        {
            RigidbodyInterpolationPlugin.Log.LogInfo("[Updater] Started");
            // _instance = this;
            _nextScanTime = Time.unscaledTime + RigidbodyInterpolationPlugin.InstanceMode switch
            {
                _ => 0f
            };
        }

        private void Update() {
            if (_scanInterval <= -0.5f) {
                if (!doneFirstScanLog) {
                    RigidbodyInterpolationPlugin.Log.LogInfo("[Updater] scan disabled");
                }
                return;
            }

            if (Time.unscaledTime < _nextScanTime) {
                return;
            }

            _nextScanTime = Time.unscaledTime + _scanInterval;

            // RigidbodyInterpolationPlugin.Log.LogInfo("[Updater] Scan tick");
            RigidbodyInterpolationPlugin.ApplyToAll();
        }

        // private void LateUpdate() {
        //     var cam = Camera.main;
        //     if (cam == null) {
        //         RigidbodyInterpolationPlugin.Log.LogWarning("[Updater (LateUpdate)] camera itself null");
        //         return
        //     }
        //
        //     // example: force camera to follow final transform
        //     // (you'll customize this once you inspect the game)
        //
        //     var target = cam.transform.parent; // or wherever it follows
        //
        //     if (target != null) {
        //         // RigidbodyInterpolationPlugin.Log.LogInfo("[Updater (LateUpdate)] patched camera");
        //         cam.transform.position = target.position;
        //         cam.transform.rotation = target.rotation;
        //     } else {
        //         RigidbodyInterpolationPlugin.Log.LogWarning("[Updater (LateUpdate)] camera target null");
        //     }
        // }
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
        private static void Prefix(Rigidbody __instance, ref RigidbodyInterpolation value) {
            if (value != RigidbodyInterpolationPlugin.InstanceMode)
                RigidbodyInterpolationPlugin.Log.LogInfo(
                        $"[SetterPatch] Blocked {__instance.name} being set to {value}");
            value = RigidbodyInterpolationPlugin.InstanceMode;
        }
    }
    [HarmonyPatch(typeof(Rigidbody), "OnEnable")]
    internal static class RigidbodyOnEnablePatch {
        [HarmonyPostfix]
        private static void Postfix(Rigidbody __instance) {
            RigidbodyInterpolationPlugin.Log.LogInfo(
                    $"[enablePatch] {__instance.name} being set");
            RigidbodyInterpolationPlugin.Apply(__instance);
        }
    }

    // -------------------------------------------------------------------------
    // Camera LateUpdate sync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Attached to the fpscontroller GameObject at runtime.
    /// Re-syncs Th → CamParent in LateUpdate so the camera always reflects
    /// the final head position after all Update lerps AND after physics
    /// interpolation has resolved for this frame.
    /// </summary>
    internal class CameraLateSync : MonoBehaviour
    {
        private MonoBehaviour _fps;

        // Cached field accessors (reflection, done once in Init)
        private FieldInfo _fTh;
        private FieldInfo _fTHeadLean;
        private FieldInfo _fFHeadLerp;
        private FieldInfo _fCamParent;
        private FieldInfo _fBsitting;
        private FieldInfo _fTHeadBob;
        private FieldInfo _fTHeadDir;

        private bool _ready;

        public void Init(MonoBehaviour fps)
        {

            RigidbodyInterpolationPlugin.Log.LogInfo(
            $"[CameraLateSync] starting");
            _fps = fps;
            var t = fps.GetType();

            _fTh         = AccessTools.Field(t, "Th");
            _fTHeadLean  = AccessTools.Field(t, "THeadLean");
            _fFHeadLerp  = AccessTools.Field(t, "FHeadLerp");
            _fCamParent  = AccessTools.Field(t, "CamParent");
            _fBsitting   = AccessTools.Field(t, "Bsitting");
            _fTHeadBob   = AccessTools.Field(t, "THeadBob");
            _fTHeadDir   = AccessTools.Field(t, "THeadDir");

            // Warn loudly if any field wasn't found — field names may differ
            foreach (var (name, fi) in new[]
            {
                ("Th",        _fTh),
                ("THeadLean", _fTHeadLean),
                ("FHeadLerp", _fFHeadLerp),
                ("CamParent", _fCamParent),
                ("Bsitting",  _fBsitting),
                ("THeadBob",  _fTHeadBob),
                ("THeadDir",  _fTHeadDir),
            })
            {
                if (fi == null)
                    RigidbodyInterpolationPlugin.Log.LogWarning(
                        $"[CameraLateSync] Could not find field '{name}' on {t.Name}");
            }

            _ready = _fTh != null && _fTHeadLean != null &&
                     _fFHeadLerp != null && _fCamParent != null;

            RigidbodyInterpolationPlugin.Log.LogInfo(
                $"[CameraLateSync] Init complete on {fps.name}, ready={_ready}");
        }

        private void LateUpdate()
        {
            if (!_ready || _fps == null) return;

            // Skip if VR is active — VR has its own head tracking
            if (UnityEngine.XR.XRSettings.enabled) return;

            var Th        = _fTh.GetValue(_fps)        as Transform;
            var THeadLean = _fTHeadLean.GetValue(_fps) as Transform;
            var CamParent = _fCamParent.GetValue(_fps) as Transform;
            var Bsitting  = _fBsitting  != null && (bool)_fBsitting.GetValue(_fps);

            if (Th == null || THeadLean == null || CamParent == null) return;

            float FHeadLerp = (float)(_fFHeadLerp?.GetValue(_fps) ?? 8f);
            float dt = Time.deltaTime;

            // Re-apply the same lerp the game does in Update, but now in
            // LateUpdate so it runs after physics interpolation is done.
            // This corrects the one-frame lag between RB movement and camera.
            if (Bsitting && _fTHeadBob != null)
            {
                var THeadBob = _fTHeadBob.GetValue(_fps) as Transform;
                if (THeadBob != null)
                    Th.position = Vector3.Lerp(Th.position, THeadBob.position, FHeadLerp * dt);
            }
            else
            {
                Th.position = Vector3.Lerp(Th.position, THeadLean.position, FHeadLerp * dt);
            }

            // If CamParent is a direct child of Th its world position already
            // followed — but if the game detached it (dropCam, mapView, etc.)
            // we leave it alone and only fix the normal first-person case.
            if (CamParent.parent == Th)
            {
                // localPosition should already be zero in normal FP view;
                // just make sure it is so no offset accumulates.
                if (CamParent.localPosition != Vector3.zero)
                    CamParent.localPosition = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// Harmony patch: hook fpscontroller.Start so we can attach CameraLateSync.
    /// </summary>
    [HarmonyPatch]
    internal static class FpsControllerStartPatch
    {
        static MethodBase TargetMethod()
        {

            RigidbodyInterpolationPlugin.Log.LogInfo(
            $"[FpsControllerStartPatch] starting targetMethod");
            // Adjust the type name here if the game uses a namespace
            var type = AccessTools.TypeByName("fpscontroller");
            if (type == null)
            {
                RigidbodyInterpolationPlugin.Log?.LogError(
                    "[FpsControllerStartPatch] Could not find type 'fpscontroller'");
                return null;
            }

            // Try Start first, fall back to Awake
            var m = AccessTools.Method(type, "Start")
                 ?? AccessTools.Method(type, "Awake");

            if (m == null)
                RigidbodyInterpolationPlugin.Log?.LogError(
                    "[FpsControllerStartPatch] Could not find Start or Awake on fpscontroller");

            return m;
        }

        [HarmonyPostfix]
        static void Postfix(MonoBehaviour __instance)
        {
            RigidbodyInterpolationPlugin.Log.LogInfo(
            $"[FpsControllerStartPatch] starting postfix");
            if (__instance == null) return;

            // Avoid double-adding if the scene reloads
            if (__instance.GetComponent<CameraLateSync>() != null) return;

            RigidbodyInterpolationPlugin.Log?.LogInfo(
                $"[FpsControllerStartPatch] Attaching CameraLateSync to {__instance.name}");

            var sync = __instance.gameObject.AddComponent<CameraLateSync>();
            sync.Init(__instance);
        }
    }

    internal static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.BPplays.stutter_fix";
        public const string PLUGIN_NAME = "stutter_fix";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}



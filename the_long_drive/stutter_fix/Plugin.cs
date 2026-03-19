using BepInEx;
using BepInEx.Logging;

namespace stutter_fix;

using HarmonyLib;
using UnityEngine;

[BepInPlugin("net.BPplays.interpolationfix", "Interpolation Fix", "1.0.0")]
public class InterpolationFixPlugin : BaseUnityPlugin
{
    private Harmony _harmony;

    private void Awake()
    {
        _harmony = new Harmony("net.BPplays.interpolationfix");
        _harmony.PatchAll();
        Logger.LogInfo("Interpolation Fix loaded");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }

    public static void ApplyInterpolation(Object obj)
    {
        if (obj == null)
            return;

        if (obj is GameObject go)
        {
            ApplyToGameObject(go);
            return;
        }

        if (obj is Component component)
        {
            ApplyToGameObject(component.gameObject);
        }
    }

    private static void ApplyToGameObject(GameObject go)
    {
        var rigidbodies = go.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rigidbodies)
        {
            if (!rb.isKinematic)
                rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }
}

[HarmonyPatch(typeof(Object), nameof(Object.Instantiate), new[] { typeof(Object) })]
internal static class InstantiatePatch
{
    private static void Postfix(Object __result)
    {
        InterpolationFixPlugin.ApplyInterpolation(__result);
    }
}

using HarmonyLib;
using LightEater.Managers;

namespace LightEater.Patches;

internal class LandminePatch
{
    [HarmonyPatch(typeof(Landmine), nameof(Landmine.Start))]
    [HarmonyPostfix]
    private static void StartLandmine(ref Landmine __instance)
        => LightEnergyManager.AddLandmine(__instance);
}

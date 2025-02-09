using HarmonyLib;

namespace LightEater.Patches
{
    internal class LandminePatch
    {
        [HarmonyPatch(typeof(Landmine), nameof(Landmine.Start))]
        [HarmonyPostfix]
        private static void StartLandmine(ref Landmine __instance)
            => RoundManagerPatch.AddLandmine(__instance);
    }
}

using HarmonyLib;

namespace LightEater.Patches
{
    internal class RadMechAIPatch
    {
        [HarmonyPatch(typeof(RadMechAI), nameof(RadMechAI.Start))]
        [HarmonyPostfix]
        private static void StartGame(ref RadMechAI __instance)
            => RoundManagerPatch.AddRadMech(__instance);
    }
}

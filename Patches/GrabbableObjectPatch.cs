using HarmonyLib;

namespace LightEater.Patches
{
    internal class GrabbableObjectPatch
    {
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        [HarmonyPostfix]
        private static void StartGame(ref GrabbableObject __instance)
            => RoundManagerPatch.AddGrabbableObject(__instance);
    }
}

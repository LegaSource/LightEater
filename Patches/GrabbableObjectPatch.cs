using HarmonyLib;

namespace LightEater.Patches;

internal class GrabbableObjectPatch
{
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
    [HarmonyPostfix]
    private static void StartGrabbableObject(ref GrabbableObject __instance)
    {
        if (__instance is BeltBagItem beltBagItem)
        {
            RoundManagerPatch.AddBeltBagItem(beltBagItem);
            return;
        }
        RoundManagerPatch.AddGrabbableObject(__instance);
    }
}

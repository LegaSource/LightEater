using HarmonyLib;
using LightEater.Managers;

namespace LightEater.Patches;

internal class GrabbableObjectPatch
{
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
    [HarmonyPostfix]
    private static void StartGrabbableObject(ref GrabbableObject __instance)
    {
        if (__instance is BeltBagItem beltBagItem)
        {
            LightEnergyManager.AddBeltBag(beltBagItem);
            return;
        }
        LightEnergyManager.AddObject(__instance);
    }
}

using HarmonyLib;

namespace LightEater.Patches;

internal class HangarShipDoorPatch
{
    [HarmonyPatch(typeof(HangarShipDoor), nameof(HangarShipDoor.Update))]
    [HarmonyPostfix]
    private static void UpdateShipDoor(HangarShipDoor __instance)
    {
        if (!ShipLightsPatch.hasBeenAbsorbed) return;

        __instance.doorPower = 0;
        __instance.doorPowerDisplay.text = "0%";
    }
}

﻿using HarmonyLib;

namespace LightEater.Patches;

internal class ItemChargerPatch
{
    [HarmonyPatch(typeof(ItemCharger), nameof(ItemCharger.Update))]
    [HarmonyPostfix]
    private static void UpdateItemCharger(ref ItemCharger __instance)
    {
        if (!ShipLightsPatch.hasBeenAbsorbed) return;

        __instance.triggerScript.disabledHoverTip = Constants.MESSAGE_NO_SHIP_ENERGY;
        __instance.triggerScript.interactable = false;
    }
}

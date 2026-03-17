using HarmonyLib;
using LightEater.Behaviours;
using UnityEngine;

namespace LightEater.Patches;

internal class StartOfRoundPatch
{
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipLeave))]
    [HarmonyPostfix]
    private static void ShipLeave()
    {
        foreach (Deluminator deluminator in Object.FindObjectsOfType<Deluminator>())
            deluminator.energyNetwork.StopHandleLightCoroutine(showMsg: false);
        foreach (LightEaterAI lightEater in Object.FindObjectsOfType<LightEaterAI>())
            lightEater.energyNetwork.StopHandleLightCoroutine(showMsg: false);
    }
}

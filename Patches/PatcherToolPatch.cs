using HarmonyLib;
using LightEater.Behaviours;
using UnityEngine;

namespace LightEater.Patches;

internal class PatcherToolPatch
{
    private static float shockingTimer;

    [HarmonyPatch(typeof(PatcherTool), nameof(PatcherTool.StopShockingAnomalyOnClient))]
    [HarmonyPrefix]
    private static void StopShocking(ref PatcherTool __instance)
    {
        if (__instance.shockedTargetScript == null) return;
        if (__instance.shockedTargetScript is not EnemyAICollisionDetect enemyCollision) return;
        if (enemyCollision.mainScript == null || enemyCollision.mainScript is not LightEaterAI lightEater) return;

        lightEater.StopShockingServerRpc();
        shockingTimer = 0f;
    }

    [HarmonyPatch(typeof(PatcherTool), nameof(PatcherTool.LateUpdate))]
    [HarmonyPostfix]
    private static void UpdateShocking(ref PatcherTool __instance)
    {
        if (!__instance.isShocking) return;
        if (__instance.playerHeldBy == null || __instance.playerHeldBy != GameNetworkManager.Instance.localPlayerController) return;
        if (__instance.shockedTargetScript == null) return;
        if (__instance.shockedTargetScript is not EnemyAICollisionDetect enemyCollision) return;
        if (enemyCollision.mainScript == null || enemyCollision.mainScript is not LightEaterAI lightEater) return;

        shockingTimer += Time.deltaTime;
        int charges = (int)(shockingTimer * 40f);
        if (charges > 0)
        {
            shockingTimer -= charges / 40f;
            lightEater.energyNetwork.UpdateCharges(lightEater.energyNetwork.currentCharge + charges);
        }
    }
}

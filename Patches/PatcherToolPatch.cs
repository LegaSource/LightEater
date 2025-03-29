using HarmonyLib;
using LightEater.Behaviours;

namespace LightEater.Patches;

internal class PatcherToolPatch
{
    [HarmonyPatch(typeof(PatcherTool), nameof(PatcherTool.StopShockingAnomalyOnClient))]
    [HarmonyPrefix]
    private static void StopShocking(ref PatcherTool __instance)
    {
        if (__instance.shockedTargetScript == null) return;
        if (__instance.shockedTargetScript is not EnemyAICollisionDetect enemyCollision) return;
        if (enemyCollision.mainScript == null || enemyCollision.mainScript is not LightEaterAI lightEater) return;

        lightEater.ShockEnemyServerRpc((int)(__instance.timeSpentShocking / 0.1f * 25));
    }
}

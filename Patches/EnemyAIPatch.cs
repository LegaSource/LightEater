using HarmonyLib;
using LightEater.Behaviours;
using LightEater.Managers;

namespace LightEater.Patches;

internal class EnemyAIPatch
{
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Start))]
    [HarmonyPostfix]
    private static void StartEnemy(ref EnemyAI __instance)
        => LightEnergyManager.AddEnemy(__instance);

    [HarmonyPatch(typeof(EnemyAICollisionDetect), "IShockableWithGun.CanBeShocked")]
    [HarmonyPrefix]
    private static bool CanBeShocked(ref EnemyAICollisionDetect __instance, ref bool __result)
    {
        if (__instance.mainScript == null || __instance.mainScript is not LightEaterAI) return true;

        __result = true;
        return false;
    }

    [HarmonyPatch(typeof(EnemyAICollisionDetect), "IShockableWithGun.ShockWithGun")]
    [HarmonyPostfix]
    private static void ShockWithGun(ref EnemyAICollisionDetect __instance)
    {
        if (__instance.mainScript == null || __instance.mainScript is not LightEaterAI lightEater) return;
        lightEater.isShocked = true;
    }
}

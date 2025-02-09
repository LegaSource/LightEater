using HarmonyLib;

namespace LightEater.Patches
{
    internal class EnemyAIPatch
    {
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Start))]
        [HarmonyPostfix]
        private static void StartEnemy(ref EnemyAI __instance)
            => RoundManagerPatch.AddEnemy(__instance);
    }
}

using HarmonyLib;
using LightEater.Managers;

namespace LightEater.Patches;

internal class RoundManagerPatch
{
    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FinishGeneratingNewLevelClientRpc))]
    [HarmonyPostfix]
    private static void StartGame()
    {
        StormyWeatherPatch.lightEaters.Clear();

        LightEnergyManager.ResetEnemies();
        LightEnergyManager.ResetObjects();
        LightEnergyManager.ResetTurrets();
        LightEnergyManager.ResetLandmines();
        LightEnergyManager.ResetPoweredLights();
        LightEnergyManager.ResetBeltBags();
    }
}

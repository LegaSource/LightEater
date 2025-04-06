using HarmonyLib;
using LightEater.Managers;

namespace LightEater.Patches;

internal class TurretPatch
{
    [HarmonyPatch(typeof(Turret), nameof(Turret.Start))]
    [HarmonyPostfix]
    private static void StartTurret(ref Turret __instance)
        => LightEnergyManager.AddTurret(__instance);
}

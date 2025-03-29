using HarmonyLib;

namespace LightEater.Patches;

internal class TurretPatch
{
    [HarmonyPatch(typeof(Turret), nameof(Turret.Start))]
    [HarmonyPostfix]
    private static void StartTurret(ref Turret __instance)
        => RoundManagerPatch.AddTurret(__instance);
}

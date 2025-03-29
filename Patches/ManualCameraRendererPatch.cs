using HarmonyLib;

namespace LightEater.Patches;

internal class ManualCameraRendererPatch
{
    [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.SwitchScreenButton))]
    [HarmonyPrefix]
    private static bool SwitchScreenButton()
        => !ShipLightsPatch.hasBeenAbsorbed;

    [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.SwitchScreenOn))]
    [HarmonyPrefix]
    private static bool SwitchScreenOn()
        => !ShipLightsPatch.hasBeenAbsorbed;

    [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.SwitchScreenOnClientRpc))]
    [HarmonyPrefix]
    private static bool SwitchScreenOnClientRpc()
        => !ShipLightsPatch.hasBeenAbsorbed;
}

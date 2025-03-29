using HarmonyLib;

namespace LightEater.Patches;

internal class ShipLightsPatch
{
    public static bool hasBeenAbsorbed = false;

    [HarmonyPatch(typeof(ShipLights), nameof(ShipLights.SetShipLightsBoolean))]
    [HarmonyPrefix]
    private static bool SetShipLightsBoolean()
        => !hasBeenAbsorbed;

    [HarmonyPatch(typeof(ShipLights), nameof(ShipLights.SetShipLightsClientRpc))]
    [HarmonyPrefix]
    private static bool SetShipLightsClientRpc()
        => !hasBeenAbsorbed;

    [HarmonyPatch(typeof(ShipLights), nameof(ShipLights.SetShipLightsOnLocalClientOnly))]
    [HarmonyPrefix]
    private static bool SetShipLightsOnLocalClientOnly()
        => !hasBeenAbsorbed;

    [HarmonyPatch(typeof(ShipLights), nameof(ShipLights.ToggleShipLights))]
    [HarmonyPrefix]
    private static bool ToggleShipLights()
        => !hasBeenAbsorbed;

    [HarmonyPatch(typeof(ShipLights), nameof(ShipLights.ToggleShipLightsOnLocalClientOnly))]
    [HarmonyPrefix]
    private static bool ToggleShipLightsOnLocalClientOnly()
        => !hasBeenAbsorbed;
}

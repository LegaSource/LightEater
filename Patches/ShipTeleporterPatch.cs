using HarmonyLib;

namespace LightEater.Patches
{
    internal class ShipTeleporterPatch
    {
        [HarmonyPatch(typeof(ShipTeleporter), nameof(ShipTeleporter.Update))]
        [HarmonyPostfix]
        private static void UpdateShipTeleporter(ref ShipTeleporter __instance)
        {
            if (ShipLightsPatch.hasBeenAbsorbed)
            {
                __instance.buttonTrigger.disabledHoverTip = Constants.MESSAGE_SHIP_ENERGY;
                __instance.buttonTrigger.interactable = false;
            }
        }
    }
}

using HarmonyLib;
using LightEater.Managers;
using System.Linq;
using UnityEngine;

namespace LightEater.Patches;

internal class StartOfRoundPatch
{
    public static HangarShipDoor shipDoor;
    public static StartMatchLever shipLever;
    public static ItemCharger itemCharger;
    public static Terminal terminal;
    public static ShipTeleporter shipTeleporter;
    public static ShipTeleporter inverseShipTeleporter;

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    [HarmonyPostfix]
    private static void StartRound()
    {
        if (ConfigManager.disableShipDoor.Value) shipDoor = Object.FindObjectOfType<HangarShipDoor>();
        if (ConfigManager.disableShipLever.Value) shipLever = Object.FindObjectOfType<StartMatchLever>();
        if (ConfigManager.disableItemCharger.Value) itemCharger = Object.FindObjectOfType<ItemCharger>();
        if (ConfigManager.disableTerminal.Value) terminal = Object.FindObjectOfType<Terminal>();
        if (ConfigManager.disableShipTeleporters.Value)
        {
            shipTeleporter = Object.FindObjectsOfType<ShipTeleporter>().FirstOrDefault(t => !t.isInverseTeleporter);
            inverseShipTeleporter = Object.FindObjectsOfType<ShipTeleporter>().FirstOrDefault(t => t.isInverseTeleporter);
        }
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipLeave))]
    [HarmonyPostfix]
    private static void ShipLeave()
        => ChargeShip();

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipLeaveAutomatically))]
    [HarmonyPostfix]
    private static void ShipLeaveAutomatically()
        => ChargeShip();

    public static void ChargeShip()
    {
        ShipLightsPatch.hasBeenAbsorbed = false;
        EnablesShipFunctionalities(true);
    }

    public static void EnablesShipFunctionalities(bool enable)
    {
        EnablesShipDoor(enable);
        EnablesShipLever(enable);
        EnablesItemCharger(enable);
        EnablesTerminal(enable);
        EnablesShipTeleporter(enable);
        EnablesInverseShipTeleporter(enable);
    }

    public static void EnablesShipDoor(bool enable)
    {
        if (!ConfigManager.disableShipDoor.Value) return;

        shipDoor ??= Object.FindObjectOfType<HangarShipDoor>();
        if (shipDoor == null) return;

        shipDoor.hydraulicsScreenDisplayed = enable;
        shipDoor.hydraulicsDisplay.SetActive(enable);
        shipDoor.SetDoorButtonsEnabled(enable);
        if (!enable) StartOfRound.Instance.shipDoorsAnimator.SetBool("Closed", value: enable);
        if (!enable) shipDoor.SetDoorOpen();
    }

    public static void EnablesShipLever(bool enable)
    {
        if (!ConfigManager.disableShipLever.Value) return;

        shipLever ??= Object.FindObjectOfType<StartMatchLever>();
        if (shipLever == null) return;

        shipLever.triggerScript.disabledHoverTip = enable ? Constants.MESSAGE_DEFAULT_SHIP_LEVER : Constants.MESSAGE_NO_SHIP_ENERGY;
        shipLever.triggerScript.interactable = enable;
    }

    public static void EnablesItemCharger(bool enable)
    {
        if (!ConfigManager.disableItemCharger.Value) return;

        itemCharger ??= Object.FindObjectOfType<ItemCharger>();
        if (itemCharger == null) return;

        itemCharger.triggerScript.disabledHoverTip = enable ? Constants.MESSAGE_DEFAULT_ITEM_CHARGER : Constants.MESSAGE_NO_SHIP_ENERGY;
        itemCharger.triggerScript.interactable = enable;
    }

    public static void EnablesTerminal(bool enable)
    {
        if (!ConfigManager.disableTerminal.Value) return;

        terminal ??= Object.FindObjectOfType<Terminal>();
        if (terminal == null) return;

        if (!enable) terminal.terminalTrigger.disabledHoverTip = Constants.MESSAGE_NO_SHIP_ENERGY;
        terminal.terminalTrigger.interactable = enable;
    }

    public static void EnablesShipTeleporter(bool enable)
    {
        if (!ConfigManager.disableShipTeleporters.Value) return;

        shipTeleporter ??= Object.FindObjectsOfType<ShipTeleporter>().FirstOrDefault(t => !t.isInverseTeleporter);
        if (shipTeleporter == null) return;

        if (!enable) shipTeleporter.buttonTrigger.disabledHoverTip = Constants.MESSAGE_NO_SHIP_ENERGY;
        shipTeleporter.buttonTrigger.interactable = enable;
    }

    public static void EnablesInverseShipTeleporter(bool enable)
    {
        if (!ConfigManager.disableShipTeleporters.Value) return;

        inverseShipTeleporter ??= Object.FindObjectsOfType<ShipTeleporter>().FirstOrDefault(t => t.isInverseTeleporter);
        if (inverseShipTeleporter == null) return;

        if (!enable) inverseShipTeleporter.buttonTrigger.disabledHoverTip = Constants.MESSAGE_NO_SHIP_ENERGY;
        inverseShipTeleporter.buttonTrigger.interactable = enable;
    }
}

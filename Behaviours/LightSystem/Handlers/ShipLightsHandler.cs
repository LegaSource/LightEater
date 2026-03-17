using LightEater.Behaviours.LightSystem.Interfaces;
using LightEater.Managers;
using UnityEngine;
using static LegaFusionCore.Registries.LFCShipFeatureRegistry;

namespace LightEater.Behaviours.LightSystem.Handlers;

public class ShipLightsHandler : ILightSource
{
    protected Animator shipAnimator;
    protected Animator shipDoorsAnimator;
    protected AudioSource shipDoorsAudio;
    protected Vector3 shipPosition;

    protected ShipLightsHandler()
    {
        shipAnimator = StartOfRound.Instance.shipAnimatorObject.gameObject.GetComponent<Animator>();
        shipDoorsAnimator = StartOfRound.Instance.shipDoorsAnimator;
        shipDoorsAudio = StartOfRound.Instance.shipDoorAudioSource;
        shipPosition = StartOfRound.Instance.shipLandingPosition.position;
    }
    public virtual void HandleLightInitialization(ref float remainingDuration)
    {
        StartOfRound.Instance.PowerSurgeShip();
        shipAnimator.SetBool("AlarmRinging", value: true);
        shipDoorsAudio.PlayOneShot(StartOfRound.Instance.alarmSFX);
    }
    public virtual bool HandleLightConsumption(float absorbDuration, float remainingDuration, float timePassed)
    {
        shipDoorsAnimator.SetBool("Closed", value: !shipDoorsAnimator.GetBool("Closed"));
        return true;
    }
    public virtual void HandleLightDepletion()
    {
        if (ConfigManager.disableShipLights.Value) AddLock(ShipFeatureType.SHIP_LIGHTS, LightEater.modName);
        if (ConfigManager.disableShipScreen.Value) AddLock(ShipFeatureType.MAP_SCREEN, LightEater.modName);
        if (ConfigManager.disableShipDoors.Value) AddLock(ShipFeatureType.SHIP_DOORS, LightEater.modName);
        if (ConfigManager.disableShipLever.Value) AddLock(ShipFeatureType.SHIP_LEVER, LightEater.modName);
        if (ConfigManager.disableTerminal.Value) AddLock(ShipFeatureType.SHIP_TERMINAL, LightEater.modName);
        if (ConfigManager.disableItemCharger.Value) AddLock(ShipFeatureType.ITEM_CHARGER, LightEater.modName);
        if (ConfigManager.disableShipTV.Value) AddLock(ShipFeatureType.SHIP_TV, LightEater.modName);
        if (ConfigManager.disableShipTeleporters.Value) AddLock(ShipFeatureType.SHIP_TELEPORTERS, LightEater.modName);
        if (ConfigManager.disableSmartCupboard.Value) AddLock(ShipFeatureType.SMART_CUPBOARD, LightEater.modName);
        HandleInterruptAction();
    }
    public virtual void HandleInterruptAction()
    {
        shipAnimator.SetBool("Closed", value: false);
        shipDoorsAudio.Stop();
        shipAnimator.SetBool("AlarmRinging", value: false);
    }
    public virtual Vector3 GetClosestNodePosition() => shipPosition;
    public virtual Vector3 GetClosestLightPosition() => shipPosition;
}

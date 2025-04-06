using LightEater.Behaviours.LightSystem.Interfaces;
using LightEater.Patches;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers;

public class ShipLightsHandler : ILightSource
{
    protected Animator shipAnimator;
    protected Animator shipDoorsAnimator;
    protected Vector3 shipPosition;

    protected ShipLightsHandler()
    {
        shipAnimator = StartOfRound.Instance.shipAnimatorObject.gameObject.GetComponent<Animator>();
        shipPosition = StartOfRound.Instance.shipLandingPosition.position;
        shipDoorsAnimator = StartOfRound.Instance.shipDoorsAnimator;
    }

    public virtual void HandleLightInitialization(ref float remainingDuration, bool enable)
    {
        StartOfRound.Instance.PowerSurgeShip();
        shipAnimator.SetBool("AlarmRinging", value: true);
        StartOfRound.Instance.shipDoorAudioSource.PlayOneShot(StartOfRound.Instance.alarmSFX);
    }

    public virtual bool HandleLightConsumption(float absorbDuration, float remainingDuration, float timePassed)
    {
        shipDoorsAnimator.SetBool("Closed", value: !shipDoorsAnimator.GetBool("Closed"));
        return true;
    }

    public virtual void HandleLightDepletion()
    {
        ShipLightsPatch.hasBeenAbsorbed = true;
        StartOfRoundPatch.EnablesShipFunctionalities(false);
        shipAnimator.SetBool("AlarmRinging", value: false);
    }

    public virtual bool HandleLightInjection(float releaseDuration, float remainingDuration, float timePassed) => true;

    public virtual void HandleLightRestoration()
    {
        ShipLightsPatch.hasBeenAbsorbed = false;
        StartOfRoundPatch.EnablesShipFunctionalities(true);
        shipAnimator.SetBool("AlarmRinging", value: false);
    }

    public virtual Vector3 GetClosestNodePosition()
        => shipPosition;

    public virtual Vector3 GetClosestLightPosition()
        => shipPosition;
}

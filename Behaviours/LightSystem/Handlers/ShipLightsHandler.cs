using LightEater.Behaviours.LightSystem.Interfaces;
using LightEater.Patches;
using UnityEngine;

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

    public virtual void HandleLightInitialization(ref float remainingDuration, bool enable)
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
        ShipLightsPatch.hasBeenAbsorbed = true;
        StartOfRoundPatch.EnablesShipFunctionalities(false);
        HandleInterruptAction();
    }

    public virtual bool HandleLightInjection(float releaseDuration, float remainingDuration, float timePassed) => true;

    public virtual void HandleLightRestoration()
    {
        ShipLightsPatch.hasBeenAbsorbed = false;
        StartOfRoundPatch.EnablesShipFunctionalities(true);
        HandleInterruptAction();
    }

    public virtual void HandleInterruptAction()
    {
        shipAnimator.SetBool("Closed", value: false);
        shipDoorsAudio.Stop();
        shipAnimator.SetBool("AlarmRinging", value: false);
    }

    public virtual Vector3 GetClosestNodePosition()
        => shipPosition;

    public virtual Vector3 GetClosestLightPosition()
        => shipPosition;
}

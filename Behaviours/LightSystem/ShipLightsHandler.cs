using LightEater.Patches;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem;

public class ShipLightsHandler(LightEaterAI lightEater) : ILightSource
{
    private readonly LightEaterAI lightEater = lightEater;

    public void HandleLightInitialization(ref float absorbDuration)
    {
        StartOfRound.Instance.PowerSurgeShip();
        StartOfRound.Instance.shipAnimatorObject.gameObject.GetComponent<Animator>().SetBool("AlarmRinging", value: true);
        StartOfRound.Instance.shipDoorAudioSource.PlayOneShot(StartOfRound.Instance.alarmSFX);
    }

    public bool HandleLightConsumption(float absorbDuration, float timePassed)
    {
        StartOfRound.Instance.shipDoorsAnimator.SetBool("Closed", value: !StartOfRound.Instance.shipDoorsAnimator.GetBool("Closed"));
        return true;
    }

    public void HandleLightDepletion()
    {
        lightEater.currentCharge += 200;
        ShipLightsPatch.hasBeenAbsorbed = true;
        StartOfRoundPatch.EnablesShipFunctionalities(false);
        StartOfRound.Instance.shipAnimatorObject.gameObject.GetComponent<Animator>().SetBool("AlarmRinging", value: false);
    }

    public Vector3 GetClosestNodePosition()
        => StartOfRound.Instance.shipLandingPosition.position;

    public Vector3 GetClosestLightPosition()
        => StartOfRound.Instance.shipLandingPosition.position;
}

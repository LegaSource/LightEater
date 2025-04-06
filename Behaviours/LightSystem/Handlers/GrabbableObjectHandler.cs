using LightEater.Behaviours.LightSystem.Interfaces;
using LightEater.Managers;
using System.Linq;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers;

public class GrabbableObjectHandler : ILightSource
{
    protected readonly GrabbableObject grabbableObject;

    protected GrabbableObjectHandler(GrabbableObject grabbableObject)
        => this.grabbableObject = grabbableObject;

    public virtual void HandleLightInitialization(ref float remainingDuration, bool enable)
    {
        float totalDuration = remainingDuration;
        remainingDuration *= grabbableObject.insertedBattery.charge;
        remainingDuration = enable ? remainingDuration : totalDuration - remainingDuration;

        if (grabbableObject is FlashlightItem flashlight)
        {
            flashlight.flashlightAudio.PlayOneShot(flashlight.flashlightFlicker);
            WalkieTalkie.TransmitOneShotAudio(flashlight.flashlightAudio, flashlight.flashlightFlicker, 0.8f);
            flashlight.flashlightInterferenceLevel = 1;
        }
    }

    public virtual bool HandleLightConsumption(float absorbDuration, float remainingDuration, float timePassed)
    {
        grabbableObject.insertedBattery.charge = Mathf.Max(0f, 1f - ((timePassed + (absorbDuration - remainingDuration)) / absorbDuration));
        return true;
    }

    public virtual void HandleLightDepletion()
    {
        grabbableObject.insertedBattery = new Battery(true, 0f);
        grabbableObject.ChargeBatteries();
        grabbableObject.isBeingUsed = false;
    }

    public virtual bool HandleLightInjection(float releaseDuration, float remainingDuration, float timePassed)
    {
        grabbableObject.insertedBattery.charge = Mathf.Min(1f, (timePassed + (releaseDuration - remainingDuration)) / releaseDuration);
        return true;
    }

    public virtual void HandleLightRestoration()
    {
        grabbableObject.insertedBattery = new Battery(false, 1f);
        grabbableObject.ChargeBatteries();
        grabbableObject.isBeingUsed = false;
    }

    public virtual void HandleInterruptAction()
    {
        if (grabbableObject is FlashlightItem flashlight)
        {
            flashlight.flashlightInterferenceLevel = 0;
            flashlight.SwitchFlashlight(on: false);
        }
    }

    public virtual Vector3 GetClosestNodePosition()
        => GetObjectPosition();

    public virtual Vector3 GetClosestLightPosition()
        => GetObjectPosition();

    protected Vector3 GetObjectPosition()
    {
        Vector3 objectPosition = grabbableObject.transform.position;
        foreach (BeltBagItem beltBag in LightEnergyManager.beltBags)
        {
            if (beltBag == null) continue;
            if (beltBag.objectsInBag.FirstOrDefault(o => o == grabbableObject) == null) continue;

            objectPosition = beltBag.transform.position;
        }
        return objectPosition;
    }
}

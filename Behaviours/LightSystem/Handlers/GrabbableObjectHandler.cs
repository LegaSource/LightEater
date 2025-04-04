using LightEater.Behaviours.LightSystem.Interfaces;
using System.Linq;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers;

public class GrabbableObjectHandler : ILightSource
{
    protected readonly GrabbableObject grabbableObject;

    protected GrabbableObjectHandler(GrabbableObject grabbableObject)
        => this.grabbableObject = grabbableObject;

    public virtual void HandleLightInitialization(ref float absorbDuration)
    {
        absorbDuration *= grabbableObject.insertedBattery.charge;
        if (grabbableObject is FlashlightItem flashlight)
        {
            flashlight.flashlightAudio.PlayOneShot(flashlight.flashlightFlicker);
            WalkieTalkie.TransmitOneShotAudio(flashlight.flashlightAudio, flashlight.flashlightFlicker, 0.8f);
            flashlight.flashlightInterferenceLevel = 1;
        }
    }

    public virtual bool HandleLightConsumption(float absorbDuration, float timePassed)
    {
        grabbableObject.insertedBattery.charge = Mathf.Max(0f, 1f - ((timePassed + (5f - absorbDuration)) / 5f));
        return true;
    }

    public virtual void HandleLightDepletion() { }

    public virtual Vector3 GetClosestNodePosition()
        => GetObjectPosition();

    public virtual Vector3 GetClosestLightPosition()
        => GetObjectPosition();

    protected Vector3 GetObjectPosition()
    {
        Vector3 objectPosition = grabbableObject.transform.position;
        foreach (BeltBagItem beltBag in LightEater.beltBags)
        {
            if (beltBag == null) continue;
            if (beltBag.objectsInBag.FirstOrDefault(o => o == grabbableObject) == null) continue;

            objectPosition = beltBag.transform.position;
        }
        return objectPosition;
    }
}

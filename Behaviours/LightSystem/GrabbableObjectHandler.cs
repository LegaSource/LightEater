using LightEater.Managers;
using System.Linq;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem;

public class GrabbableObjectHandler(LightEaterAI lightEater, GrabbableObject grabbableObject) : ILightSource
{
    private readonly LightEaterAI lightEater = lightEater;
    private readonly GrabbableObject grabbableObject = grabbableObject;

    public void HandleLightInitialization(ref float absorbDuration)
    {
        absorbDuration *= grabbableObject.insertedBattery.charge;
        if (grabbableObject is FlashlightItem flashlight)
        {
            flashlight.flashlightAudio.PlayOneShot(flashlight.flashlightFlicker);
            WalkieTalkie.TransmitOneShotAudio(flashlight.flashlightAudio, flashlight.flashlightFlicker, 0.8f);
            flashlight.flashlightInterferenceLevel = 1;
        }
    }

    public bool HandleLightConsumption(float absorbDuration, float timePassed)
    {
        grabbableObject.insertedBattery.charge = Mathf.Max(0f, 1f - ((timePassed + (5f - absorbDuration)) / 5f));
        return !(grabbableObject.insertedBattery.charge > 0 && !lightEater.CanBeAbsorbed(grabbableObject, 15f));
    }

    public void HandleLightDepletion()
    {
        lightEater.currentCharge += ConfigManager.itemCharge.Value;
        _ = LightEater.grabbableObjects.Remove(grabbableObject);
    }

    public Vector3 GetClosestNodePosition()
        => lightEater.ChooseClosestNodeToPosition(GetObjectPosition()).position;

    public Vector3 GetClosestLightPosition()
        => GetObjectPosition();

    private Vector3 GetObjectPosition()
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

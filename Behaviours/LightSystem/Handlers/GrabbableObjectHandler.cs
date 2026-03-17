using LegaFusionCore.Registries;
using LightEater.Behaviours.LightSystem.Interfaces;
using System.Linq;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers;

public class GrabbableObjectHandler : ILightSource
{
    protected readonly GrabbableObject grabbableObject;

    protected GrabbableObjectHandler(GrabbableObject grabbableObject) => this.grabbableObject = grabbableObject;
    public virtual void HandleLightInitialization(ref float remainingDuration)
    {
        remainingDuration *= grabbableObject.insertedBattery.charge;
        if (grabbableObject is FlashlightItem flashlight)
            LFCObjectStateRegistry.AddFlickeringFlashlight(flashlight, LightEater.modName);
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
    public virtual void HandleInterruptAction()
    {
        if (grabbableObject is FlashlightItem flashlight)
        {
            LFCObjectStateRegistry.RemoveFlickeringFlashlight(flashlight, LightEater.modName);
            flashlight.SwitchFlashlight(on: false);
        }
    }
    public virtual Vector3 GetClosestNodePosition() => GetObjectPosition();
    public virtual Vector3 GetClosestLightPosition() => GetObjectPosition();
    protected Vector3 GetObjectPosition()
    {
        Vector3 objectPosition = grabbableObject.transform.position;
        foreach (BeltBagItem beltBag in LFCSpawnRegistry.GetAllAs<BeltBagItem>())
        {
            if (beltBag != null && beltBag.objectsInBag.FirstOrDefault(o => o == grabbableObject) != null)
                objectPosition = beltBag.transform.position;
        }
        return objectPosition;
    }
}

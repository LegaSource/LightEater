using LightEater.Managers;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers.DeluminatorHandlers;

public class GrabbableObjectHandler(Deluminator deluminator, GrabbableObject grabbableObject) : Handlers.GrabbableObjectHandler(grabbableObject)
{
    private readonly Deluminator deluminator = deluminator;

    public override bool HandleLightConsumption(float absorbDuration, float remainingDuration, float timePassed)
        => base.HandleLightConsumption(absorbDuration, remainingDuration, timePassed)
            && !(grabbableObject.insertedBattery.charge > 0f && !LightEnergyManager.CanBeAbsorbed(grabbableObject, deluminator.transform.position, 7.5f));

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();
        int charges = Mathf.Min(200, deluminator.energyNetwork.currentCharge + ConfigManager.itemCharge.Value);
        deluminator.energyNetwork.UpdateCharges(charges);
    }

    public override bool HandleLightInjection(float releaseDuration, float remainingDuration, float timePassed)
        => base.HandleLightInjection(releaseDuration, remainingDuration, timePassed)
            && !(grabbableObject.insertedBattery.charge < 1f && !LightEnergyManager.CanBeAbsorbed(grabbableObject, deluminator.transform.position, 7.5f));

    public override void HandleLightRestoration()
    {
        base.HandleLightRestoration();
        deluminator.energyNetwork.UpdateCharges(deluminator.energyNetwork.currentCharge - ConfigManager.itemCharge.Value);
    }
}

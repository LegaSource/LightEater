using LightEater.Managers;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers.LightEaterHandlers;

public class GrabbableObjectHandler(LightEaterAI lightEater, GrabbableObject grabbableObject) : Handlers.GrabbableObjectHandler(grabbableObject)
{
    private readonly LightEaterAI lightEater = lightEater;

    public override bool HandleLightConsumption(float absorbDuration, float remainingDuration, float timePassed)
        => base.HandleLightConsumption(absorbDuration, remainingDuration, timePassed)
            && !(grabbableObject.insertedBattery.charge > 0f && !LightEnergyManager.CanBeAbsorbed(grabbableObject, lightEater.transform.position, 15f));

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();
        lightEater.energyNetwork.UpdateCharges(lightEater.energyNetwork.currentCharge + ConfigManager.itemCharge.Value);
    }

    public override Vector3 GetClosestNodePosition()
        => lightEater.ChooseClosestNodeToPosition(GetObjectPosition()).position;
}

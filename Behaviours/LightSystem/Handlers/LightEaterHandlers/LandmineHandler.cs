using LightEater.Managers;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers.LightEaterHandlers;

public class LandmineHandler(LightEaterAI lightEater, Landmine landmine) : Handlers.LandmineHandler(landmine)
{
    private readonly LightEaterAI lightEater = lightEater;

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();
        lightEater.energyNetwork.UpdateCharges(lightEater.energyNetwork.currentCharge + ConfigManager.landmineCharge.Value);
    }

    public override Vector3 GetClosestNodePosition()
        => lightEater.ChooseClosestNodeToPosition(lightEater.energyNetwork.closestLightSource.transform.position).position;
}

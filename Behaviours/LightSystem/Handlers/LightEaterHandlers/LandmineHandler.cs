using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers.LightEaterHandlers;

public class LandmineHandler(LightEaterAI lightEater, Landmine landmine) : Handlers.LandmineHandler(landmine)
{
    private readonly LightEaterAI lightEater = lightEater;

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();
        lightEater.energyNetwork.currentCharge += 20;
    }
    public override Vector3 GetClosestNodePosition() => lightEater.ChooseClosestNodeToPosition(lightEater.energyNetwork.closestLightSource.transform.position).position;
}

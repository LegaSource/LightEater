using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers.LightEaterHandlers;

public class TurretHandler(LightEaterAI lightEater, Turret turret) : Handlers.TurretHandler(turret)
{
    private readonly LightEaterAI lightEater = lightEater;

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();
        lightEater.energyNetwork.currentCharge += 20;
    }
    public override Vector3 GetClosestNodePosition() => lightEater.ChooseClosestNodeToPosition(lightEater.energyNetwork.closestLightSource.transform.position).position;
}

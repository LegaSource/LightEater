using LightEater.Managers;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem;

public class LandmineHandler(LightEaterAI lightEater, Landmine landmine) : ILightSource
{
    private readonly LightEaterAI lightEater = lightEater;
    private readonly Landmine landmine = landmine;

    public void HandleLightInitialization(ref float absorbDuration) { }

    public bool HandleLightConsumption(float absorbDuration, float timePassed) => true;

    public void HandleLightDepletion()
    {
        lightEater.currentCharge += ConfigManager.landmineCharge.Value;
        landmine.ToggleMineEnabledLocalClient(false);
        _ = LightEater.landmines.Remove(landmine);
    }

    public Vector3 GetClosestNodePosition()
        => lightEater.ChooseClosestNodeToPosition(lightEater.closestLightSource.transform.position).position;

    public Vector3 GetClosestLightPosition()
        => landmine.transform.position;
}

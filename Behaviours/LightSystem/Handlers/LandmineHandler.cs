using LightEater.Behaviours.LightSystem.Interfaces;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers;

public class LandmineHandler : ILightSource
{
    protected readonly Landmine landmine;

    protected LandmineHandler(Landmine landmine)
        => this.landmine = landmine;

    public virtual void HandleLightInitialization(ref float absorbDuration) { }

    public virtual bool HandleLightConsumption(float absorbDuration, float timePassed) => true;

    public virtual void HandleLightDepletion()
    {
        landmine.ToggleMineEnabledLocalClient(false);
        _ = LightEater.landmines.Remove(landmine);
    }

    public virtual Vector3 GetClosestNodePosition()
        => landmine.transform.position;

    public virtual Vector3 GetClosestLightPosition()
        => landmine.transform.position;
}

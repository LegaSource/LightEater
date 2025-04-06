using LightEater.Behaviours.LightSystem.Interfaces;
using LightEater.Managers;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers;

public class LandmineHandler : ILightSource
{
    protected readonly Landmine landmine;

    protected LandmineHandler(Landmine landmine)
        => this.landmine = landmine;

    public virtual void HandleLightInitialization(ref float remainingDuration, bool enable) { }

    public virtual bool HandleLightConsumption(float absorbDuration, float remainingDuration, float timePassed) => true;

    public virtual void HandleLightDepletion()
    {
        landmine.ToggleMineEnabledLocalClient(false);
        LightEnergyManager.SetLandmineValue(landmine, false);
    }

    public virtual bool HandleLightInjection(float releaseDuration, float remainingDuration, float timePassed) => true;

    public virtual void HandleLightRestoration()
    {
        landmine.ToggleMineEnabledLocalClient(true);
        LightEnergyManager.SetLandmineValue(landmine, true);
    }

    public virtual void HandleInterruptAction() { }

    public virtual Vector3 GetClosestNodePosition()
        => landmine.transform.position;

    public virtual Vector3 GetClosestLightPosition()
        => landmine.transform.position;
}

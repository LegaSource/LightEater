using LightEater.Managers;

namespace LightEater.Behaviours.LightSystem.Handlers.DeluminatorHandlers;

public class GrabbableObjectHandler(Deluminator deluminator, GrabbableObject grabbableObject) : Handlers.GrabbableObjectHandler(grabbableObject)
{
    private readonly Deluminator deluminator = deluminator;

    public override bool HandleLightConsumption(float absorbDuration, float timePassed)
        => base.HandleLightConsumption(absorbDuration, timePassed)
            && !(grabbableObject.insertedBattery.charge > 0 && !LightEnergyManager.CanBeAbsorbed(grabbableObject, deluminator.transform.position, 15f));

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();
        deluminator.energyNetwork.currentCharge += ConfigManager.itemCharge.Value;
    }
}

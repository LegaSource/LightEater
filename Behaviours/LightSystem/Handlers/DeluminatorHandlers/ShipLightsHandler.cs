namespace LightEater.Behaviours.LightSystem.Handlers.DeluminatorHandlers;

public class ShipLightsHandler(Deluminator deluminator) : Handlers.ShipLightsHandler
{
    private readonly Deluminator deluminator = deluminator;

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();
        deluminator.energyNetwork.currentCharge += 200;
    }

    public override void HandleLightRestoration()
    {
        base.HandleLightRestoration();
        deluminator.energyNetwork.currentCharge -= 200;
    }
}

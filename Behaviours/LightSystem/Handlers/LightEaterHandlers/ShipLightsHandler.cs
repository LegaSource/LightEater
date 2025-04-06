namespace LightEater.Behaviours.LightSystem.Handlers.LightEaterHandlers;

public class ShipLightsHandler(LightEaterAI lightEater) : Handlers.ShipLightsHandler
{
    private readonly LightEaterAI lightEater = lightEater;

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();
        lightEater.energyNetwork.currentCharge += 200;
    }

    public override void HandleLightRestoration()
    {
        base.HandleLightRestoration();
        lightEater.energyNetwork.currentCharge -= 200;
    }
}

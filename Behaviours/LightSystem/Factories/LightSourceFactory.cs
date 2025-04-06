using LightEater.Behaviours.LightSystem.Interfaces;
using LightEater.Managers;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Factories;

public class LightSourceFactory
{
    public static ILightSource GetLightHandler(object lightSource, LightEnergyNetworkManager absorptionNetwork)
    {
        object handler = absorptionNetwork.GetComponentInParent<LightEaterAI>() as object
            ?? absorptionNetwork.GetComponentInParent<Deluminator>();

        return handler switch
        {
            LightEaterAI lightEater => GetLightHandler(lightSource, lightEater),
            Deluminator deluminator => GetLightHandler(lightSource, deluminator),
            _ => null
        };
    }

    public static ILightSource GetLightHandler(object lightSource, LightEaterAI lightEater) => lightSource switch
    {
        ShipLights => new Handlers.LightEaterHandlers.ShipLightsHandler(lightEater),
        EnemyAI enemy => new Handlers.LightEaterHandlers.EnemyHandler(lightEater, enemy),
        GrabbableObject grabbableObject => new Handlers.LightEaterHandlers.GrabbableObjectHandler(lightEater, grabbableObject),
        Turret turret => new Handlers.LightEaterHandlers.TurretHandler(lightEater, turret),
        Landmine landmine => new Handlers.LightEaterHandlers.LandmineHandler(lightEater, landmine),
        Animator animator => new Handlers.LightEaterHandlers.AnimatorHandler(lightEater, animator),
        _ => null
    };

    public static ILightSource GetLightHandler(object lightSource, Deluminator deluminator) => lightSource switch
    {
        ShipLights => new Handlers.DeluminatorHandlers.ShipLightsHandler(deluminator),
        EnemyAI enemy => new Handlers.DeluminatorHandlers.EnemyHandler(deluminator, enemy),
        GrabbableObject grabbableObject => new Handlers.DeluminatorHandlers.GrabbableObjectHandler(deluminator, grabbableObject),
        Turret turret => new Handlers.DeluminatorHandlers.TurretHandler(deluminator, turret),
        Landmine landmine => new Handlers.DeluminatorHandlers.LandmineHandler(deluminator, landmine),
        Animator animator => new Handlers.DeluminatorHandlers.AnimatorHandler(deluminator, animator),
        _ => null
    };
}

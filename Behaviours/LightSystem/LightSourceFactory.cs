using UnityEngine;

namespace LightEater.Behaviours.LightSystem;

public class LightSourceFactory
{
    public static ILightSource GetLightHandler(object lightSource, LightEaterAI lightEater) => lightSource switch
    {
        ShipLights => new ShipLightsHandler(lightEater),
        EnemyAI enemy => new EnemyAIHandler(lightEater, enemy),
        GrabbableObject grabbableObject => new GrabbableObjectHandler(lightEater, grabbableObject),
        Turret turret => new TurretHandler(lightEater, turret),
        Landmine landmine => new LandmineHandler(lightEater, landmine),
        Animator animator => new AnimatorHandler(lightEater, animator),
        _ => null
    };
}

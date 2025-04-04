using GameNetcodeStuff;
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
        EnemyAI enemy => new Handlers.DeluminatorHandlers.EnemyHandler(deluminator, enemy),
        GrabbableObject grabbableObject => new Handlers.DeluminatorHandlers.GrabbableObjectHandler(deluminator, grabbableObject),
        Turret turret => new Handlers.DeluminatorHandlers.TurretHandler(deluminator, turret),
        Landmine landmine => new Handlers.DeluminatorHandlers.LandmineHandler(deluminator, landmine),
        Animator animator => new Handlers.DeluminatorHandlers.AnimatorHandler(deluminator, animator),
        _ => null
    };

    public static GameObject GetClosestLightSourceInView(Deluminator deluminator)
    {
        PlayerControllerB player = deluminator?.playerHeldBy;
        if (player == null) return null;

        Ray ray = new Ray(player.gameplayCamera.transform.position, player.gameplayCamera.transform.forward);
        if (Physics.SphereCast(ray, 2f, out RaycastHit hit, 15f))
        {
            GameObject lightSource = hit.collider.GetComponent<EnemyAI>()?.gameObject
                ?? hit.collider.GetComponent<GrabbableObject>()?.gameObject
                ?? hit.collider.GetComponent<Turret>()?.gameObject
                ?? hit.collider.GetComponent<Landmine>()?.gameObject
                ?? hit.collider.GetComponent<Animator>()?.gameObject;
            return lightSource;
        }
        return null;
    }
}

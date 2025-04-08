using GameNetcodeStuff;
using LightEater.Managers;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers.DeluminatorHandlers;

public class TurretHandler(Deluminator deluminator, Turret turret) : Handlers.TurretHandler(turret)
{
    private readonly Deluminator deluminator = deluminator;

    public override bool HandleLightConsumption(float absorbDuration, float remainingDuration, float timePassed)
    {
        if (!base.HandleLightConsumption(absorbDuration, remainingDuration, timePassed)) return false;

        PlayerControllerB player = deluminator.playerHeldBy;
        return player != null && Vector3.Distance(player.transform.position, turret.transform.position) <= 7.5f;
    }

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();
        int charges = Mathf.Min(200, deluminator.energyNetwork.currentCharge + ConfigManager.turretCharge.Value);
        deluminator.energyNetwork.UpdateCharges(charges);
    }

    public override bool HandleLightInjection(float releaseDuration, float remainingDuration, float timePassed)
    {
        if (!base.HandleLightInjection(releaseDuration, remainingDuration, timePassed)) return false;

        PlayerControllerB player = deluminator.playerHeldBy;
        return player != null && Vector3.Distance(player.transform.position, turret.transform.position) <= 7.5f;
    }

    public override void HandleLightRestoration()
    {
        base.HandleLightRestoration();
        deluminator.energyNetwork.UpdateCharges(deluminator.energyNetwork.currentCharge - ConfigManager.turretCharge.Value);
    }
}

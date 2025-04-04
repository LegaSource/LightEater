using GameNetcodeStuff;
using LightEater.Managers;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers.DeluminatorHandlers;

public class AnimatorHandler(Deluminator deluminator, Animator animator) : Handlers.AnimatorHandler(animator)
{
    private readonly Deluminator deluminator = deluminator;

    public override bool HandleLightConsumption(float absorbDuration, float timePassed)
    {
        if (!base.HandleLightConsumption(absorbDuration, timePassed)) return false;

        PlayerControllerB player = deluminator.playerHeldBy;
        return player != null && Vector3.Distance(player.transform.position, animator.transform.position) <= 15f;
    }

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();
        deluminator.energyNetwork.currentCharge += ConfigManager.lightCharge.Value;
    }
}

using GameNetcodeStuff;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers.DeluminatorHandlers;

public class AnimatorHandler(Deluminator deluminator, Animator animator) : Handlers.AnimatorHandler(animator)
{
    private readonly Deluminator deluminator = deluminator;

    public override bool HandleLightConsumption(float absorbDuration, float remainingDuration, float timePassed)
    {
        if (base.HandleLightConsumption(absorbDuration, remainingDuration, timePassed))
        {
            PlayerControllerB player = deluminator.playerHeldBy;
            return player != null && Vector3.Distance(player.transform.position, animator.transform.position) <= 7.5f;
        }
        return false;
    }

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();
        deluminator.energyNetwork.currentCharge = Mathf.Min(200, deluminator.energyNetwork.currentCharge + 20);
    }
}

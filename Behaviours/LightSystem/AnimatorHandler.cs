using LightEater.Managers;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem;

public class AnimatorHandler(LightEaterAI lightEater, Animator animator) : ILightSource
{
    private readonly LightEaterAI lightEater = lightEater;
    private readonly Animator animator = animator;

    public void HandleLightInitialization(ref float absorbDuration) { }

    public bool HandleLightConsumption(float absorbDuration, float timePassed)
    {
        animator?.SetTrigger("Flicker");
        return true;
    }

    public void HandleLightDepletion()
    {
        lightEater.currentCharge += ConfigManager.lightCharge.Value;
        animator.SetBool("on", false);
        _ = RoundManager.Instance.allPoweredLightsAnimators.RemoveAll(l => l.gameObject == lightEater.closestLightSource);
    }

    public Vector3 GetClosestNodePosition()
        => lightEater.ChooseClosestNodeToPosition(lightEater.closestLightSource.transform.position).position;

    public Vector3 GetClosestLightPosition()
        => lightEater.closestLightSource.transform.position;
}

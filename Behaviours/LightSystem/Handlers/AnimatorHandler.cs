using LightEater.Behaviours.LightSystem.Interfaces;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers;

public class AnimatorHandler : ILightSource
{
    protected readonly Animator animator;

    protected AnimatorHandler(Animator animator)
        => this.animator = animator;

    public virtual void HandleLightInitialization(ref float remainingDuration, bool enable) { }

    public virtual bool HandleLightConsumption(float absorbDuration, float remainingDuration, float timePassed)
    {
        animator?.SetTrigger("Flicker");
        return true;
    }

    public virtual void HandleLightDepletion()
        => animator.SetBool("on", false);

    public virtual bool HandleLightInjection(float releaseDuration, float remainingDuration, float timePassed)
    {
        animator?.SetTrigger("Flicker");
        return true;
    }

    public virtual void HandleLightRestoration()
        => animator.SetBool("on", true);

    public virtual Vector3 GetClosestNodePosition()
        => animator.transform.position;

    public virtual Vector3 GetClosestLightPosition()
        => animator.transform.position;
}

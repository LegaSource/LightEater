using LightEater.Behaviours.LightSystem.Interfaces;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers;

public class AnimatorHandler : ILightSource
{
    protected readonly Animator animator;

    protected AnimatorHandler(Animator animator)
        => this.animator = animator;

    public virtual void HandleLightInitialization(ref float absorbDuration) { }

    public virtual bool HandleLightConsumption(float absorbDuration, float timePassed)
    {
        animator?.SetTrigger("Flicker");
        return true;
    }

    public virtual void HandleLightDepletion()
        => animator.SetBool("on", false);

    public virtual Vector3 GetClosestNodePosition()
        => animator.transform.position;

    public virtual Vector3 GetClosestLightPosition()
        => animator.transform.position;
}

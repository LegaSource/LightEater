using LegaFusionCore.Registries;
using LightEater.Behaviours.LightSystem.Interfaces;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers;

public class AnimatorHandler : ILightSource
{
    protected readonly Animator animator;

    protected AnimatorHandler(Animator animator) => this.animator = animator;
    public virtual void HandleLightInitialization(ref float remainingDuration) { }
    public virtual bool HandleLightConsumption(float absorbDuration, float remainingDuration, float timePassed)
    {
        animator?.SetTrigger("Flicker");
        return true;
    }
    public virtual void HandleLightDepletion() => LFCPoweredLightsRegistry.AddLock(animator, LightEater.modName);
    public virtual void HandleInterruptAction() { }
    public virtual Vector3 GetClosestNodePosition() => animator.transform.position;
    public virtual Vector3 GetClosestLightPosition() => animator.transform.position;
}

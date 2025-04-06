using LightEater.Behaviours.LightSystem.Interfaces;
using LightEater.Managers;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers;

public class TurretHandler : ILightSource
{
    protected readonly Turret turret;

    protected TurretHandler(Turret turret)
        => this.turret = turret;

    public virtual void HandleLightInitialization(ref float remainingDuration, bool enable) { }

    public virtual bool HandleLightConsumption(float absorbDuration, float remainingDuration, float timePassed)
    {
        turret.SwitchTurretMode((int)TurretMode.Berserk);
        return true;
    }

    public virtual void HandleLightDepletion()
    {
        turret.ToggleTurretEnabledLocalClient(false);
        turret.mainAudio.Stop();
        turret.farAudio.Stop();
        turret.berserkAudio.Stop();
        if (turret.fadeBulletAudioCoroutine != null) turret.StopCoroutine(turret.fadeBulletAudioCoroutine);
        turret.fadeBulletAudioCoroutine = turret.StartCoroutine(turret.FadeBulletAudio());
        turret.bulletParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
        turret.turretAnimator.SetInteger("TurretMode", 0);

        LightEnergyManager.SetTurretValue(turret, false);
    }

    public virtual bool HandleLightInjection(float releaseDuration, float remainingDuration, float timePassed) => true;

    public virtual void HandleLightRestoration()
    {
        turret.ToggleTurretEnabledLocalClient(true);
        LightEnergyManager.SetTurretValue(turret, true);
    }

    public virtual void HandleInterruptAction() { }

    public virtual Vector3 GetClosestNodePosition()
        => turret.transform.position;

    public virtual Vector3 GetClosestLightPosition()
        => turret.transform.position;
}

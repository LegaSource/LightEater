using LightEater.Behaviours.LightSystem.Interfaces;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers;

public class TurretHandler : ILightSource
{
    protected readonly Turret turret;

    protected TurretHandler(Turret turret)
        => this.turret = turret;

    public virtual void HandleLightInitialization(ref float absorbDuration) { }

    public virtual bool HandleLightConsumption(float absorbDuration, float timePassed)
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

        _ = LightEater.turrets.Remove(turret);
    }

    public virtual Vector3 GetClosestNodePosition()
        => turret.transform.position;

    public virtual Vector3 GetClosestLightPosition()
        => turret.transform.position;
}

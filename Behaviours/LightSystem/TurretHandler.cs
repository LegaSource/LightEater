using LightEater.Managers;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem;

public class TurretHandler(LightEaterAI lightEater, Turret turret) : ILightSource
{
    private readonly LightEaterAI lightEater = lightEater;
    private readonly Turret turret = turret;

    public void HandleLightInitialization(ref float absorbDuration) { }

    public bool HandleLightConsumption(float absorbDuration, float timePassed)
    {
        turret.SwitchTurretMode((int)TurretMode.Berserk);
        return true;
    }

    public void HandleLightDepletion()
    {
        lightEater.currentCharge += ConfigManager.turretCharge.Value;

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

    public Vector3 GetClosestNodePosition()
        => lightEater.ChooseClosestNodeToPosition(lightEater.closestLightSource.transform.position).position;

    public Vector3 GetClosestLightPosition()
        => turret.transform.position;
}

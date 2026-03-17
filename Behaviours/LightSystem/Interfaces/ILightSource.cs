using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Interfaces;

public interface ILightSource
{
    public void HandleLightInitialization(ref float remainingDuration);
    public bool HandleLightConsumption(float absorbDuration, float remainingDuration, float timePassed);
    public void HandleLightDepletion();
    public void HandleInterruptAction();
    public Vector3 GetClosestNodePosition();
    public Vector3 GetClosestLightPosition();
}

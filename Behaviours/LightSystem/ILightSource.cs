using UnityEngine;

namespace LightEater.Behaviours.LightSystem;

public interface ILightSource
{
    public void HandleLightInitialization(ref float absorbDuration);
    public bool HandleLightConsumption(float absorbDuration, float timePassed);
    public void HandleLightDepletion();
    public Vector3 GetClosestNodePosition();
    public Vector3 GetClosestLightPosition();
}

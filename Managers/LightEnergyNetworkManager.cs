using LightEater.Behaviours.LightSystem.Factories;
using LightEater.Behaviours.LightSystem.Interfaces;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace LightEater.Managers;
public class LightEnergyNetworkManager : NetworkBehaviour
{
    public int currentCharge = 0; // Server side
    public GameObject closestLightSource;

    public float absorbDuration;

    public Coroutine absorbCoroutine;

    public Action PlayAbsorptionSound;
    public Action ResetAbsorption;

    public void AbsorbLight(GameObject lightSource, float absorbDuration)
    {
        if (absorbCoroutine != null) return;

        closestLightSource = lightSource;
        this.absorbDuration = absorbDuration;

        NetworkObject networkObject = closestLightSource.GetComponent<NetworkObject>();
        if (NetworkManager.Singleton.IsHost)
        {
            if (networkObject != null) AbsorbLightClientRpc(networkObject);
            else AbsorbLightClientRpc(closestLightSource.name, closestLightSource.transform.position);
            return;
        }
        if (networkObject != null) AbsorbLightServerRpc(networkObject);
        else AbsorbLightServerRpc(closestLightSource.name, closestLightSource.transform.position);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AbsorbLightServerRpc(NetworkObjectReference obj)
        => AbsorbLightClientRpc(obj);

    [ClientRpc]
    public void AbsorbLightClientRpc(NetworkObjectReference obj)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        closestLightSource = networkObject.gameObject;
        absorbCoroutine ??= StartCoroutine(AbsorbLightCoroutine());
    }

    // Si aucun NetworkObject
    [ServerRpc(RequireOwnership = false)]
    public void AbsorbLightServerRpc(string objectName, Vector3 position)
        => AbsorbLightClientRpc(objectName, position);

    // Si aucun NetworkObject
    [ClientRpc]
    public void AbsorbLightClientRpc(string objectName, Vector3 position)
    {
        closestLightSource = LightEnergyManager.GetLightSourceByName(objectName, position);
        absorbCoroutine ??= StartCoroutine(AbsorbLightCoroutine());
    }

    public IEnumerator AbsorbLightCoroutine()
    {
        object lightSource = LightEnergyManager.DetermineLightSource(closestLightSource);
        ILightSource lightHandler = LightSourceFactory.GetLightHandler(lightSource, this);
        if (lightHandler == null) yield break;

        bool isAbsorbed = true;
        float timePassed = 0f;

        PlayAbsorptionSound?.Invoke();
        lightHandler.HandleLightInitialization(ref absorbDuration);

        while (timePassed < absorbDuration)
        {
            yield return new WaitForSeconds(0.5f);
            timePassed += 0.5f;

            if (!lightHandler.HandleLightConsumption(absorbDuration, timePassed))
            {
                isAbsorbed = false;
                break;
            }
        }

        if (lightSource is FlashlightItem flashlight) flashlight.flashlightInterferenceLevel = 0;
        if (isAbsorbed) lightHandler.HandleLightDepletion();

        ResetAbsorption?.Invoke();
        closestLightSource = null;
        absorbCoroutine = null;
    }
}
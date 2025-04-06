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

    public Coroutine absorbCoroutine;
    public Coroutine releaseCoroutine;

    public Action PlayAbsorptionSound;
    public Action PlayReleaseSound;
    public Action ResetAction;

    public void AbsorbLight(GameObject lightSource, float absorbDuration)
    {
        if (absorbCoroutine != null) return;

        NetworkObject networkObject = lightSource.GetComponent<NetworkObject>();
        if (NetworkManager.Singleton.IsHost)
        {
            if (networkObject != null) AbsorbLightClientRpc(networkObject, absorbDuration);
            else AbsorbLightClientRpc(lightSource.name, lightSource.transform.position, absorbDuration);
            return;
        }
        if (networkObject != null) AbsorbLightServerRpc(networkObject, absorbDuration);
        else AbsorbLightServerRpc(lightSource.name, lightSource.transform.position, absorbDuration);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AbsorbLightServerRpc(NetworkObjectReference obj, float absorbDuration)
        => AbsorbLightClientRpc(obj, absorbDuration);

    [ClientRpc]
    public void AbsorbLightClientRpc(NetworkObjectReference obj, float absorbDuration)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        closestLightSource = networkObject.gameObject;
        absorbCoroutine ??= StartCoroutine(AbsorbLightCoroutine(absorbDuration));
    }

    // Si aucun NetworkObject
    [ServerRpc(RequireOwnership = false)]
    public void AbsorbLightServerRpc(string objectName, Vector3 position, float absorbDuration)
        => AbsorbLightClientRpc(objectName, position, absorbDuration);

    // Si aucun NetworkObject
    [ClientRpc]
    public void AbsorbLightClientRpc(string objectName, Vector3 position, float absorbDuration)
    {
        closestLightSource = LightEnergyManager.GetLightSourceByName(objectName, position, true);
        absorbCoroutine ??= StartCoroutine(AbsorbLightCoroutine(absorbDuration));
    }

    public IEnumerator AbsorbLightCoroutine(float absorbDuration)
    {
        object lightSource = LightEnergyManager.DetermineLightSource(closestLightSource, true);

        ILightSource lightHandler = LightSourceFactory.GetLightHandler(lightSource, this);
        if (lightHandler == null)
        {
            closestLightSource = null;
            releaseCoroutine = null;
            yield break;
        }

        bool isAbsorbed = true;
        float remainingDuration = absorbDuration;
        float timePassed = 0f;

        PlayAbsorptionSound?.Invoke();
        lightHandler.HandleLightInitialization(ref remainingDuration, true);

        while (timePassed < remainingDuration)
        {
            yield return new WaitForSeconds(0.5f);
            timePassed += 0.5f;

            if (!lightHandler.HandleLightConsumption(absorbDuration, remainingDuration, timePassed))
            {
                isAbsorbed = false;
                break;
            }
        }

        if (lightSource is FlashlightItem flashlight) flashlight.flashlightInterferenceLevel = 0;
        if (isAbsorbed) lightHandler.HandleLightDepletion();

        ResetAction?.Invoke();
        closestLightSource = null;
        absorbCoroutine = null;
    }

    public void ReleaseLight(GameObject lightSource, float releaseDuration)
    {
        if (releaseCoroutine != null) return;

        NetworkObject networkObject = lightSource.GetComponent<NetworkObject>();
        if (NetworkManager.Singleton.IsHost)
        {
            if (networkObject != null) ReleaseLightClientRpc(networkObject, releaseDuration);
            else ReleaseLightClientRpc(lightSource.name, lightSource.transform.position, releaseDuration);
            return;
        }
        if (networkObject != null) ReleaseLightServerRpc(networkObject, releaseDuration);
        else ReleaseLightServerRpc(lightSource.name, lightSource.transform.position, releaseDuration);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReleaseLightServerRpc(NetworkObjectReference obj, float releaseDuration)
        => ReleaseLightClientRpc(obj, releaseDuration);

    [ClientRpc]
    public void ReleaseLightClientRpc(NetworkObjectReference obj, float releaseDuration)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        closestLightSource = networkObject.gameObject;
        releaseCoroutine ??= StartCoroutine(ReleaseLightCoroutine(releaseDuration));
    }

    // Si aucun NetworkObject
    [ServerRpc(RequireOwnership = false)]
    public void ReleaseLightServerRpc(string objectName, Vector3 position, float releaseDuration)
        => ReleaseLightClientRpc(objectName, position, releaseDuration);

    // Si aucun NetworkObject
    [ClientRpc]
    public void ReleaseLightClientRpc(string objectName, Vector3 position, float releaseDuration)
    {
        closestLightSource = LightEnergyManager.GetLightSourceByName(objectName, position, false);
        releaseCoroutine ??= StartCoroutine(ReleaseLightCoroutine(releaseDuration));
    }

    public IEnumerator ReleaseLightCoroutine(float releaseDuration)
    {
        object lightSource = LightEnergyManager.DetermineLightSource(closestLightSource, false);

        ILightSource lightHandler = LightSourceFactory.GetLightHandler(lightSource, this);
        if (lightHandler == null)
        {
            closestLightSource = null;
            releaseCoroutine = null;
            yield break;
        }

        bool isReleased = true;
        float remainingDuration = releaseDuration;
        float timePassed = 0f;

        PlayReleaseSound?.Invoke();
        lightHandler.HandleLightInitialization(ref remainingDuration, false);

        while (timePassed < remainingDuration)
        {
            yield return new WaitForSeconds(0.5f);
            timePassed += 0.5f;

            if (!lightHandler.HandleLightInjection(releaseDuration, remainingDuration, timePassed))
            {
                isReleased = false;
                break;
            }
        }

        if (lightSource is FlashlightItem flashlight) flashlight.flashlightInterferenceLevel = 0;
        if (isReleased) lightHandler.HandleLightRestoration();

        ResetAction?.Invoke();
        closestLightSource = null;
        releaseCoroutine = null;
    }
}
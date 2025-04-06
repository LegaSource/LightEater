using LightEater.Behaviours.LightSystem.Factories;
using LightEater.Behaviours.LightSystem.Interfaces;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace LightEater.Managers;
public class LightEnergyNetworkManager : NetworkBehaviour
{
    public int currentCharge = 0;
    public GameObject closestLightSource;
    public float totalDuration;

    public Coroutine handleLightCoroutine;

    public Action PlayActionSound;
    public Action StopActionSound;
    public Action ResetAction;

    public int currentActionType = (int)LightActionType.Absorb;
    public enum LightActionType { Absorb, Release }

    public void HandleLight(GameObject lightSource, LightActionType actionType)
    {
        if (handleLightCoroutine != null) return;

        NetworkObject networkObject = lightSource.GetComponent<NetworkObject>();
        if (NetworkManager.Singleton.IsHost)
        {
            if (networkObject != null) HandleLightClientRpc(networkObject, (int)actionType);
            else HandleLightClientRpc(lightSource.name, lightSource.transform.position, (int)actionType);
            return;
        }
        if (networkObject != null) HandleLightServerRpc(networkObject, (int)actionType);
        else HandleLightServerRpc(lightSource.name, lightSource.transform.position, (int)actionType);
    }

    [ServerRpc(RequireOwnership = false)]
    public void HandleLightServerRpc(NetworkObjectReference obj, int actionType)
        => HandleLightClientRpc(obj, actionType);

    [ClientRpc]
    public void HandleLightClientRpc(NetworkObjectReference obj, int actionType)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        currentActionType = actionType;
        closestLightSource = networkObject.gameObject;
        handleLightCoroutine ??= StartCoroutine(HandleLightCoroutine());
    }

    // Si aucun NetworkObject
    [ServerRpc(RequireOwnership = false)]
    public void HandleLightServerRpc(string objectName, Vector3 position, int actionType)
        => HandleLightClientRpc(objectName, position, actionType);

    // Si aucun NetworkObject
    [ClientRpc]
    public void HandleLightClientRpc(string objectName, Vector3 position, int actionType)
    {
        currentActionType = actionType;
        closestLightSource = LightEnergyManager.GetLightSourceByName(objectName, position, currentActionType == (int)LightActionType.Absorb);
        handleLightCoroutine ??= StartCoroutine(HandleLightCoroutine());
    }

    public IEnumerator HandleLightCoroutine()
    {
        bool isAbsorbing = currentActionType == (int)LightActionType.Absorb;
        object lightSource = LightEnergyManager.DetermineLightSource(closestLightSource, isAbsorbing);

        ILightSource lightHandler = LightSourceFactory.GetLightHandler(lightSource, this);
        if (lightHandler == null)
        {
            StopHandleLightCoroutine(false);
            yield break;
        }

        bool completed = true;
        totalDuration = lightSource is EnemyAI ? 10f : 5f;
        float remainingDuration = totalDuration;
        float timePassed = 0f;

        PlayActionSound?.Invoke();
        lightHandler.HandleLightInitialization(ref remainingDuration, isAbsorbing);

        while (timePassed < remainingDuration)
        {
            yield return new WaitForSeconds(0.5f);
            timePassed += 0.5f;

            bool valid = isAbsorbing
                ? lightHandler.HandleLightConsumption(totalDuration, remainingDuration, timePassed)
                : lightHandler.HandleLightInjection(totalDuration, remainingDuration, timePassed);

            if (!valid)
            {
                completed = false;
                break;
            }
        }

        lightHandler.HandleInterruptAction();
        if (completed)
        {
            if (isAbsorbing) lightHandler.HandleLightDepletion();
            else lightHandler.HandleLightRestoration();
        }

        ResetAction?.Invoke();
        StopHandleLightCoroutine(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopCoroutineServerRpc(bool showMsg)
        => StopCoroutineClientRpc(showMsg);

    [ClientRpc]
    public void StopCoroutineClientRpc(bool showMsg)
        => StopHandleLightCoroutine(showMsg);

    public void StopHandleLightCoroutine(bool showMsg)
    {
        if (handleLightCoroutine == null) return;

        StopCoroutine(handleLightCoroutine);
        handleLightCoroutine = null;

        StopActionSound?.Invoke();

        if (closestLightSource != null)
        {
            object lightSource = LightEnergyManager.DetermineLightSource(closestLightSource, currentActionType == (int)LightActionType.Absorb);
            ILightSource lightHandler = LightSourceFactory.GetLightHandler(lightSource, this);
            lightHandler?.HandleInterruptAction();

            closestLightSource = null;
        }

        if (showMsg)
        {
            if (currentActionType == (int)LightActionType.Absorb)
                HUDManager.Instance.DisplayTip(Constants.INFORMATION, Constants.MESSAGE_INFO_ABSORPTION_CANCELED);
            else
                HUDManager.Instance.DisplayTip(Constants.INFORMATION, Constants.MESSAGE_INFO_RELEASE_CANCELED);
        }
    }
}
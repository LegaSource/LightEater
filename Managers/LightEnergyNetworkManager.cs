using LegaFusionCore.Utilities;
using LightEater.Behaviours;
using LightEater.Behaviours.LightSystem.Factories;
using LightEater.Behaviours.LightSystem.Interfaces;
using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LightEater.Managers;
public class LightEnergyNetworkManager : NetworkBehaviour
{
    public int currentCharge = 0;
    public GameObject closestLightSource;

    public Coroutine handleLightCoroutine;

    public Action PlayActionSound;
    public Action StopActionSound;
    public Action ResetAction;

    public void HandleLight(GameObject lightSource)
    {
        if (handleLightCoroutine == null)
        {
            if (lightSource.TryGetComponentInParent(out NetworkObject networkObject))
                HandleLightEveryoneRpc(networkObject);
            else
                HandleLightEveryoneRpc(lightSource.transform.position);
        }
    }

    // Avec NetworkObject
    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void HandleLightEveryoneRpc(NetworkObjectReference obj)
    {
        if (obj.TryGet(out NetworkObject networkObject))
        {
            closestLightSource = networkObject.gameObject;
            handleLightCoroutine = StartCoroutine(HandleLightCoroutine());
        }
    }

    // Si aucun NetworkObject
    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void HandleLightEveryoneRpc(Vector3 position)
    {
        closestLightSource = GetPoweredLightByPosition(position);
        handleLightCoroutine = StartCoroutine(HandleLightCoroutine());
    }

    public GameObject GetPoweredLightByPosition(Vector3 position)
        => RoundManager.Instance.allPoweredLightsAnimators?
            .OrderBy(p => p != null ? Vector3.Distance(position, p.transform.position) : float.MaxValue)
            .FirstOrDefault()
            .gameObject;

    public IEnumerator HandleLightCoroutine()
    {
        object lightSource = LightEnergyManager.DetermineLightSource(closestLightSource);

        ILightSource lightHandler = LightSourceFactory.GetLightHandler(lightSource, this);
        if (lightHandler == null)
        {
            StopHandleLightCoroutine(showMsg: false);
            yield break;
        }

        bool completed = true;
        float duration = lightSource is EnemyAI ? 10f : 5f;
        float remainingDuration = duration;
        float timePassed = 0f;

        PlayActionSound?.Invoke();
        lightHandler.HandleLightInitialization(ref remainingDuration);

        while (timePassed < remainingDuration)
        {
            yield return new WaitForSeconds(0.5f);
            timePassed += 0.5f;

            if (!lightHandler.HandleLightConsumption(duration, remainingDuration, timePassed))
            {
                completed = false;
                break;
            }
        }

        lightHandler.HandleInterruptAction();
        if (completed)
            lightHandler.HandleLightDepletion();

        ResetAction?.Invoke();
        StopHandleLightCoroutine(showMsg: false);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void StopHandleLightEveryoneRpc() => StopHandleLightCoroutine();

    public void StopHandleLightCoroutine(bool showMsg = true)
    {
        if (handleLightCoroutine != null)
        {
            StopActionSound?.Invoke();

            if (closestLightSource != null)
            {
                ILightSource lightHandler = LightSourceFactory.GetLightHandler(LightEnergyManager.DetermineLightSource(closestLightSource), this);
                lightHandler?.HandleInterruptAction();

                closestLightSource = null;
            }

            if (showMsg && LFCUtilities.ShouldBeLocalPlayer(GetComponentInParent<Deluminator>()?.playerHeldBy))
                HUDManager.Instance.DisplayTip(Constants.INFORMATION, Constants.MESSAGE_INFO_ABSORPTION_CANCELED);

            StopCoroutine(handleLightCoroutine);
            handleLightCoroutine = null;
        }
    }
}
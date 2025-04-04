using GameNetcodeStuff;
using HarmonyLib;
using LightEater.Behaviours;
using LightEater.Managers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LightEater.Patches;

internal class StormyWeatherPatch
{
    public static List<LightEaterAI> lightEaters = [];

    public static int randomStrikeTimer = 10;
    public static float strikeLightEaterTimer = 0f;

    public static GameObject setStaticToEnemy;

    public static LightEaterAI targetedLightEater;

    [HarmonyPatch(typeof(StormyWeather), nameof(StormyWeather.Update))]
    [HarmonyPostfix]
    private static void UpdateStormyWeather(ref StormyWeather __instance)
    {
        if (setStaticToEnemy != null)
        {
            EnemyAI enemy = setStaticToEnemy.GetComponentInChildren<EnemyAI>();
            if (enemy != null && !enemy.isOutside) __instance.staticElectricityParticle.Stop();
            __instance.staticElectricityParticle.transform.position = setStaticToEnemy.transform.position;
        }

        if (!RoundManager.Instance.IsOwner) return;

        _ = lightEaters.RemoveAll(l => l == null);
        if (!lightEaters.Any()) return;

        strikeLightEaterTimer += Time.deltaTime;
        if (strikeLightEaterTimer < randomStrikeTimer) return;

        LightEaterAI lightEater = null;
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (!player.isPlayerControlled || player.isPlayerDead) continue;

            lightEater = lightEaters.FirstOrDefault(l => l.isOutside && Vector3.Distance(player.transform.position, l.transform.position) < 20f);
            if (lightEater != null) break;
        }
        if (lightEater == null) return;

        randomStrikeTimer = new System.Random().Next(ConfigManager.minStrikeDuration.Value, ConfigManager.maxStrikeDuration.Value);
        strikeLightEaterTimer = 0f;
        _ = __instance.StartCoroutine(StrikeLightEaterCoroutine());
    }

    public static IEnumerator StrikeLightEaterCoroutine()
    {
        LightEaterAI lightEater = lightEaters[new System.Random().Next(lightEaters.Count)];
        RoundManager.Instance.ShowStaticElectricityWarningServerRpc(lightEater.thisNetworkObject, 1.5f);

        yield return new WaitForSeconds(2f);

        if (!lightEater.isOutside) yield break;

        targetedLightEater = lightEater;
        RoundManager.Instance.LightningStrikeServerRpc(lightEater.transform.position);
    }

    [HarmonyPatch(typeof(StormyWeather), nameof(StormyWeather.SetStaticElectricityWarning))]
    [HarmonyPrefix]
    private static bool SetStaticElectricityWarning(ref StormyWeather __instance, ref NetworkObject warningObject, ref float particleTime)
    {
        if (warningObject.GetComponentInChildren<EnemyAI>() == null) return true;

        setStaticToEnemy = warningObject.gameObject;

        ParticleSystem.ShapeModule shape = __instance.staticElectricityParticle.shape;
        shape.meshRenderer = setStaticToEnemy.GetComponentInChildren<MeshRenderer>();

        __instance.staticElectricityParticle.time = particleTime;
        __instance.staticElectricityParticle.Play();

        AudioSource audioSource = __instance.staticElectricityParticle.gameObject.GetComponent<AudioSource>();
        audioSource.clip = __instance.staticElectricityAudio;
        audioSource.Play();
        audioSource.time = particleTime;

        return false;
    }

    [HarmonyPatch(typeof(StormyWeather), nameof(StormyWeather.LightningStrike))]
    [HarmonyPostfix]
    private static void LightningStrike()
    {
        if (targetedLightEater == null) return;
        targetedLightEater.energyNetwork.currentCharge += ConfigManager.stormCharge.Value;
    }
}

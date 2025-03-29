﻿using GameNetcodeStuff;
using LightEater.Behaviours.LightSystem;
using LightEater.Managers;
using LightEater.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace LightEater.Behaviours;

public class LightEaterAI : EnemyAI
{
    public int currentCharge = 0; // Server side
    public bool isShocked = false; // Server side

    public Transform TurnCompass;
    public AudioClip[] MoveSounds = Array.Empty<AudioClip>();
    public AudioClip AbsorptionSound;
    public AudioClip SwingSound;
    public AudioClip ChargeSound;
    public AudioClip StunSound;
    public AudioClip ExplosionSound;
    public float moveTimer = 0f;

    public List<EntranceTeleport> entrances;

    public int absorbDistance = 5;
    public GameObject closestLightSource;
    private float explodeTimer = 0f;
    public bool absorbPlayerObject = false;

    public Coroutine absorbCoroutine;
    public Coroutine stunCoroutine;
    public Coroutine attackCoroutine;
    public Coroutine killCoroutine;

    public enum State
    {
        WANDERING,
        HUNTING,
        ABSORBING,
        CHASING
    }

    public override void Start()
    {
        base.Start();

        enemyType.canDie = false;

        currentBehaviourStateIndex = (int)State.WANDERING;
        creatureAnimator.SetTrigger("startWalk");
        StartSearch(transform.position);

        entrances = FindObjectsOfType<EntranceTeleport>().ToList();
        StormyWeatherPatch.lightEaters.Add(this);
    }

    public override void Update()
    {
        base.Update();

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        explodeTimer += Time.deltaTime;

        PlayMoveSound();
        int state = currentBehaviourStateIndex;
        if (targetPlayer != null && (state == (int)State.CHASING))
        {
            TurnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, TurnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
        }
    }

    public void PlayMoveSound()
    {
        if (currentBehaviourStateIndex == (int)State.ABSORBING) return;

        moveTimer -= Time.deltaTime;
        if (MoveSounds.Length > 0 && moveTimer <= 0)
        {
            creatureSFX.PlayOneShot(MoveSounds[UnityEngine.Random.Range(0, MoveSounds.Length)]);
            moveTimer = 3f;
        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.WANDERING:
                DoWandering();
                break;
            case (int)State.HUNTING:
                DoHunting();
                break;
            case (int)State.ABSORBING:
                DoAbsorbing();
                break;
            case (int)State.CHASING:
                DoChasing();
                break;
        }
    }

    public void DoWandering()
    {
        agent.speed = 2f;
        if (FoundClosestPlayerInRange(25, 10))
        {
            closestLightSource = null;
            StopSearch(currentSearch);
            DoAnimationClientRpc("startRun");
            SwitchToBehaviourClientRpc((int)State.CHASING);
            return;
        }
        if (currentCharge > 200 || FoundLightSource())
        {
            StopSearch(currentSearch);
            DoAnimationClientRpc("startRun");
            SwitchToBehaviourClientRpc((int)State.HUNTING);
            return;
        }
    }

    private bool FoundClosestPlayerInRange(int range, int senseRange)
    {
        PlayerControllerB player = CheckLineOfSightForPlayer(60f, range, senseRange);
        return player != null && PlayerIsTargetable(player) && (bool)(targetPlayer = player);
    }

    public bool FoundLightSource()
    {
        // Si on croise une lumière sur le chemin
        if (CrossingLight()) return true;

        // Se diriger vers la lumière la plus proche
        if (TimeOfDay.Instance.hour >= ConfigManager.shipMinHour.Value && !ShipLightsPatch.hasBeenAbsorbed)
        {
            absorbDistance = 20;
            closestLightSource = StartOfRound.Instance.shipRoomLights.gameObject;
        }
        else if (LightEater.enemies.Any(r => !r.isEnemyDead))
        {
            EnemyAI closestEnemy = LightEater.enemies.Where(r => !r.isEnemyDead)
                .OrderBy(r => Vector3.Distance(transform.position, r.transform.position))
                .FirstOrDefault();
            absorbDistance = ConfigManager.enemiesValues
                .FirstOrDefault(e => e.EnemyName.Equals(closestEnemy.enemyType.enemyName))?
                .AbsorbDistance
                    ?? 5;
            closestLightSource = closestEnemy.gameObject;
        }
        else
        {
            absorbDistance = 5;
            if (!isOutside)
            {
                path1 = new NavMeshPath();
                closestLightSource = RoundManager.Instance.allPoweredLightsAnimators
                    .Where(l => l != null && agent.CalculatePath(ChooseClosestNodeToPosition(l.transform.position).position, path1))
                    .OrderBy(l => Vector3.Distance(transform.position, l.transform.position))
                    .FirstOrDefault()?
                    .gameObject;
            }
            else
            {
                closestLightSource = RoundManager.Instance.allPoweredLightsAnimators
                    .OrderBy(l => l ? Vector3.Distance(GetEntranceExitPosition(GetClosestEntrance()), l.transform.position) : float.MaxValue)
                    .FirstOrDefault()?
                    .gameObject;
            }
        }
        return closestLightSource != null;
    }

    public void DoHunting()
    {
        agent.speed = ConfigManager.huntingSpeed.Value;
        if (FoundClosestPlayerInRange(25, 10))
        {
            closestLightSource = null;
            SwitchToBehaviourClientRpc((int)State.CHASING);
            return;
        }
        if (HuntPlayers()) return;
        if (CloseToLightSource())
        {
            DoAnimationClientRpc("startAbsorb");
            SwitchToBehaviourClientRpc((int)State.ABSORBING);
            return;
        }
    }

    public bool HuntPlayers()
    {
        if (currentCharge <= 200 && closestLightSource != null) return false;

        PlayerControllerB closestPlayer = StartOfRound.Instance.allPlayerScripts
            .Where(p => p.isPlayerControlled && !p.isPlayerDead)
            .OrderBy(p => Vector3.Distance(transform.position, p.transform.position))
            .FirstOrDefault();

        if (closestPlayer.isInsideFactory == isOutside)
        {
            GoTowardsEntrance();
            return true;
        }

        _ = SetDestinationToPosition(closestPlayer.transform.position);
        return true;
    }

    public bool CloseToLightSource()
    {
        _ = CrossingLight();
        object lightSource = DetermineLightSource();

        if ((lightSource is ShipLights && !isOutside)
            || (lightSource is EnemyAI enemy && enemy.isOutside != isOutside)
            || (lightSource is Animator && isOutside))
        {
            GoTowardsEntrance();
            return false;
        }

        ILightSource lightHandler = LightSourceFactory.GetLightHandler(lightSource, this);
        if (lightHandler == null) return false;

        _ = SetDestinationToPosition(lightHandler.GetClosestNodePosition());
        return Vector3.Distance(eye.transform.position, lightHandler.GetClosestLightPosition()) < absorbDistance || Vector3.Distance(transform.position, lightHandler.GetClosestNodePosition()) < 1f;
    }

    public void DoAbsorbing()
    {
        agent.speed = 0f;
        if (closestLightSource == null)
        {
            StartSearch(transform.position);
            DoAnimationClientRpc("startWalk");
            SwitchToBehaviourClientRpc((int)State.WANDERING);
            return;
        }
        AbsorbLight();
    }

    public void AbsorbLight()
    {
        if (absorbCoroutine != null) return;

        NetworkObject networkObject = closestLightSource.GetComponent<NetworkObject>();
        if (networkObject != null) AbsorbLightClientRpc(networkObject);
        else AbsorbLightClientRpc(closestLightSource.name, closestLightSource.transform.position);
    }

    [ClientRpc]
    public void AbsorbLightClientRpc(NetworkObjectReference obj)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        closestLightSource = networkObject.gameObject;
        absorbCoroutine ??= StartCoroutine(AbsorbLightCoroutine());
    }

    // Si aucun NetworkObject
    [ClientRpc]
    public void AbsorbLightClientRpc(string objectName, Vector3 position)
    {
        closestLightSource = objectName switch
        {
            Constants.SHIP_LIGHTS => StartOfRound.Instance.shipRoomLights.gameObject,
            Constants.TURRET => LightEater.turrets
                .OrderBy(t => t != null ? Vector3.Distance(position, t.transform.position) : float.MaxValue)
                .FirstOrDefault()
                .gameObject,
            Constants.LANDMINE => LightEater.landmines
                .OrderBy(l => l != null ? Vector3.Distance(position, l.transform.position) : float.MaxValue)
                .FirstOrDefault()
                .gameObject,
            _ => RoundManager.Instance.allPoweredLightsAnimators
                .OrderBy(l => l != null ? Vector3.Distance(position, l.transform.position) : float.MaxValue)
                .FirstOrDefault()
                .gameObject,
        };
        absorbCoroutine ??= StartCoroutine(AbsorbLightCoroutine());
    }

    public IEnumerator AbsorbLightCoroutine()
    {
        object lightSource = DetermineLightSource();
        ILightSource lightHandler = LightSourceFactory.GetLightHandler(lightSource, this);
        if (lightHandler == null) yield break;

        bool isAbsorbed = true;
        float absorbDuration = 5f;
        float timePassed = 0f;

        creatureSFX.PlayOneShot(AbsorptionSound);
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

        closestLightSource = null;
        absorbCoroutine = null;
    }

    public void DoChasing()
    {
        if (stunCoroutine != null) return;

        agent.speed = isShocked ? ConfigManager.chasingSpeed.Value / 2f : ConfigManager.chasingSpeed.Value;
        if (TargetOutsideChasedPlayer()) return;

        if (explodeTimer < 10f)
        {
            SetMovingTowardsTargetPlayer(targetPlayer);
            return;
        }
        if (!TargetClosestPlayerInAnyCase() || (Vector3.Distance(transform.position, targetPlayer.transform.position) > 20f && !CheckLineOfSightForPosition(targetPlayer.transform.position)))
        {
            StartSearch(transform.position);
            DoAnimationClientRpc("startWalk");
            SwitchToBehaviourClientRpc((int)State.WANDERING);
            return;
        }
        if (StunExplosion()) return;
        if (CrossingLight())
        {
            DoAnimationClientRpc("startAbsorb");
            SwitchToBehaviourClientRpc((int)State.ABSORBING);
            return;
        }
        SetMovingTowardsTargetPlayer(targetPlayer);
    }

    public bool TargetOutsideChasedPlayer()
    {
        if (targetPlayer.isInsideFactory == isOutside)
        {
            GoTowardsEntrance();
            return true;
        }
        return false;
    }

    public bool TargetClosestPlayerInAnyCase()
    {
        mostOptimalDistance = 2000f;
        targetPlayer = null;
        for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
        {
            tempDist = Vector3.Distance(transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
            if (tempDist < mostOptimalDistance)
            {
                mostOptimalDistance = tempDist;
                targetPlayer = StartOfRound.Instance.allPlayerScripts[i];
            }
        }
        return targetPlayer != null;
    }

    public bool StunExplosion()
    {
        if (currentCharge < 100 || explodeTimer < 10f) return false;
        if (!targetPlayer.HasLineOfSightToPosition(eye.position, 60f, 15)) return false;

        agent.speed = 0f;

        if (currentCharge > 200)
        {
            StunEnemyClientRpc();
            return true;
        }

        currentCharge -= 100;
        explodeTimer = 0f;

        StunExplosionClientRpc();
        return true;
    }

    [ClientRpc]
    public void StunEnemyClientRpc()
        => stunCoroutine = StartCoroutine(StunEnemyCoroutine());

    public IEnumerator StunEnemyCoroutine()
    {
        creatureAnimator.SetTrigger("startStun");
        creatureSFX.PlayOneShot(StunSound);
        enemyType.canDie = true;

        yield return new WaitForSeconds(5f);

        if (killCoroutine == null)
        {
            currentCharge = 200;
            creatureAnimator.SetTrigger("startRun");
            enemyType.canDie = false;
            stunCoroutine = null;
        }
    }

    [ClientRpc]
    public void StunExplosionClientRpc()
        => StartCoroutine(StunExplosionCoroutine());

    public IEnumerator StunExplosionCoroutine()
    {
        creatureAnimator.SetTrigger("startExplode");
        creatureSFX.PlayOneShot(ChargeSound);

        yield return new WaitForSeconds(0.75f);

        creatureSFX.PlayOneShot(ExplosionSound);
        StunGrenadeItem.StunExplosion(eye.position, affectAudio: true, flashSeverityMultiplier: 2f, enemyStunTime: 2f);
        creatureAnimator.SetTrigger("startRun");
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);

        if (currentBehaviourStateIndex != (int)State.CHASING) return;
        if (stunCoroutine != null || attackCoroutine != null || killCoroutine != null) return;

        PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other);
        if (player == null || player != GameNetworkManager.Instance.localPlayerController) return;

        AttackServerRpc((int)player.playerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AttackServerRpc(int playerId)
        => AttackClientRpc(playerId);

    [ClientRpc]
    public void AttackClientRpc(int playerId)
        => attackCoroutine ??= StartCoroutine(AttackCoroutine(StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>()));

    public IEnumerator AttackCoroutine(PlayerControllerB player)
    {
        creatureAnimator.SetTrigger("startAttack");
        creatureSFX.PlayOneShot(SwingSound);

        yield return new WaitForSeconds(0.8f);

        player.DamagePlayer(ConfigManager.damage.Value, hasDamageSFX: true, callRPC: true, CauseOfDeath.Crushing);
        creatureAnimator.SetTrigger("startRun");
        attackCoroutine = null;
    }

    public bool CrossingLight()
    {
        GameObject crossedLight = RoundManager.Instance.allPoweredLightsAnimators
            .FirstOrDefault(l => l != null && Vector3.Distance(transform.position, l.transform.position) <= 10f)?
            .gameObject
            ?? LightEater.grabbableObjects
                .FirstOrDefault(g => CanBeAbsorbed(g, 10f))?
                .gameObject
            ?? LightEater.turrets
                .FirstOrDefault(t => t != null && t.turretActive && Vector3.Distance(transform.position, t.transform.position) <= 10f)?
                .gameObject
            ?? LightEater.landmines
                .FirstOrDefault(l => l != null && l.mineActivated && Vector3.Distance(transform.position, l.transform.position) <= 10f)?
                .gameObject;

        if (crossedLight != null)
        {
            absorbDistance = 5;
            closestLightSource = crossedLight;
        }

        return closestLightSource != null;
    }

    public bool CanBeAbsorbed(GrabbableObject grabbableObject, float distance)
        => grabbableObject != null
            && grabbableObject.itemProperties.requiresBattery
            && grabbableObject.insertedBattery != null && grabbableObject.insertedBattery.charge > 0f
            && (grabbableObject is not PatcherTool patcherTool || !patcherTool.isShocking)
            && IsObjectClose(grabbableObject, distance);

    public bool IsObjectClose(GrabbableObject grabbableObject, float distance)
    {
        if (Vector3.Distance(transform.position, grabbableObject.transform.position) <= distance) return true;

        foreach (BeltBagItem beltBag in LightEater.beltBags)
        {
            if (beltBag == null || Vector3.Distance(transform.position, beltBag.transform.position) > distance) continue;
            if (beltBag.objectsInBag.FirstOrDefault(o => o == grabbableObject) != null) return true;
        }
        return false;
    }

    public object DetermineLightSource()
        => closestLightSource.GetComponentInParent<ShipLights>() is ShipLights shipLights
            ? shipLights : closestLightSource.TryGetComponent(out GrabbableObject grabbableObject)
            ? grabbableObject : closestLightSource.GetComponentInParent<Turret>() is Turret turret
            ? turret : closestLightSource.GetComponentInParent<Landmine>() is Landmine landmine
            ? landmine : closestLightSource.GetComponentInParent<EnemyAI>() is EnemyAI enemy
            ? enemy : RoundManager.Instance.allPoweredLightsAnimators.FirstOrDefault(l => l?.gameObject == closestLightSource);

    public void GoTowardsEntrance()
    {
        EntranceTeleport entranceTeleport = GetClosestEntrance();

        if (Vector3.Distance(transform.position, entranceTeleport.entrancePoint.position) < 1f)
        {
            Vector3 exitPosition = GetEntranceExitPosition(entranceTeleport);
            _ = StartCoroutine(TeleportEnemyCoroutine(exitPosition));
            return;
        }

        _ = SetDestinationToPosition(entranceTeleport.entrancePoint.position);
    }

    public EntranceTeleport GetClosestEntrance()
        => entrances.Where(e => e.isEntranceToBuilding == isOutside)
            .OrderBy(e => Vector3.Distance(transform.position, e.entrancePoint.position))
            .FirstOrDefault();

    public Vector3 GetEntranceExitPosition(EntranceTeleport entranceTeleport)
        => entrances.FirstOrDefault(e => e.isEntranceToBuilding != entranceTeleport.isEntranceToBuilding && e.entranceId == entranceTeleport.entranceId)
            .entrancePoint
            .position;

    public IEnumerator TeleportEnemyCoroutine(Vector3 position)
    {
        yield return new WaitForSeconds(1f);
        TeleportEnemyClientRpc(position, !isOutside);
    }

    [ClientRpc]
    public void TeleportEnemyClientRpc(Vector3 teleportPosition, bool isOutside)
    {
        SetEnemyOutside(isOutside);
        serverPosition = teleportPosition;
        transform.position = teleportPosition;
        _ = agent.Warp(teleportPosition);
        SyncPositionToClients();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ShockEnemyServerRpc(int charge)
    {
        currentCharge += charge;
        isShocked = false;
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (isEnemyDead || !enemyType.canDie) return;

        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        enemyHP -= force;
        if (enemyHP <= 0 && IsOwner) KillEnemyOnOwnerClient(!LightEater.isSellBodies);
    }

    public override void KillEnemy(bool destroy = false)
    {
        _ = StormyWeatherPatch.lightEaters.Remove(this);
        killCoroutine = StartCoroutine(KillEnemyCoroutine(destroy));
    }

    public IEnumerator KillEnemyCoroutine(bool destroy)
    {
        creatureAnimator.SetTrigger("startExplode");
        creatureSFX.PlayOneShot(ChargeSound);

        yield return new WaitForSeconds(4f);

        base.KillEnemy(destroy);
        Landmine.SpawnExplosion(transform.position + Vector3.up, spawnExplosionEffect: true, 6f, 6.3f);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DoAnimationServerRpc(string animationState)
        => DoAnimationClientRpc(animationState);

    [ClientRpc]
    public void DoAnimationClientRpc(string animationState)
        => creatureAnimator.SetTrigger(animationState);
}

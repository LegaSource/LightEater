using GameNetcodeStuff;
using LegaFusionCore.Managers;
using LegaFusionCore.Managers.NetworkManagers;
using LegaFusionCore.Registries;
using LegaFusionCore.Utilities;
using LethalStatus.StatusEffects;
using LightEater.Behaviours.LightSystem.Factories;
using LightEater.Behaviours.LightSystem.Interfaces;
using LightEater.Managers;
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
    public LightEnergyNetworkManager energyNetwork;

    public Transform TurnCompass;
    public AudioClip[] MoveSounds = Array.Empty<AudioClip>();
    public AudioClip[] LightEaterSounds = Array.Empty<AudioClip>();

    public int absorbDistance = 5;
    public float moveTimer = 0f;

    public Coroutine stunCoroutine;
    public Coroutine swingCoroutine;

    public enum State { WANDERING, HUNTING, ABSORBING, CHASING }
    public enum Sound { ABSORPTION, SWING, CHARGE, EXPLOSION }

    public override void Start()
    {
        base.Start();

        energyNetwork.PlayActionSound = () =>
        {
            //if (energyNetwork.totalDuration > 0f) creatureSFX.pitch = AbsorptionSound.length / energyNetwork.totalDuration;
            creatureSFX.PlayOneShot(LightEaterSounds[(int)Sound.ABSORPTION]);
            creatureSFX.pitch = 1f;
        };
        energyNetwork.StopActionSound = creatureSFX.Stop;

        currentBehaviourStateIndex = (int)State.WANDERING;
        StartSearch(transform.position);
    }

    public override void Update()
    {
        base.Update();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        PlayMoveSound();
        if (targetPlayer != null && (currentBehaviourStateIndex == (int)State.CHASING))
        {
            TurnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, TurnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
        }
    }

    public void PlayMoveSound()
    {
        if (currentBehaviourStateIndex != (int)State.ABSORBING)
        {
            moveTimer -= Time.deltaTime;
            if (MoveSounds.Length > 0 && moveTimer <= 0)
            {
                creatureSFX.PlayOneShot(MoveSounds[UnityEngine.Random.Range(0, MoveSounds.Length)]);
                moveTimer = 3f;
            }
        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.WANDERING: DoWandering(); break;
            case (int)State.HUNTING: DoHunting(); break;
            case (int)State.ABSORBING: DoAbsorbing(); break;
            case (int)State.CHASING: DoChasing(); break;
        }
    }

    public void DoWandering()
    {
        agent.speed = 2f;
        if (this.FoundClosestPlayerInRange(25, 10))
        {
            energyNetwork.closestLightSource = null;
            StopSearch(currentSearch);
            SwitchToBehaviourClientRpc((int)State.CHASING);
            return;
        }
        if (FoundLightSource())
        {
            StopSearch(currentSearch);
            SwitchToBehaviourClientRpc((int)State.HUNTING);
            return;
        }
    }

    public bool FoundLightSource()
    {
        // Si on croise une lumière sur le chemin
        if (CrossingLight()) return true;

        // Se diriger vers la lumière la plus proche
        if (CanAbsorbShip())
        {
            absorbDistance = 20;
            energyNetwork.closestLightSource = StartOfRound.Instance.shipRoomLights.gameObject;
            return energyNetwork.closestLightSource != null;
        }

        IEnumerable<EnemyAI> enemies = LFCSpawnRegistry.GetAllAs<EnemyAI>()?
            .Where(e => !e.isEnemyDead && ConfigManager.enemiesValues.Select(v => v.EnemyName).Contains(e.enemyType.enemyName));
        if (enemies.Any())
        {
            EnemyAI closestEnemy = enemies.OrderBy(r => Vector3.Distance(transform.position, r.transform.position)).FirstOrDefault();
            absorbDistance = ConfigManager.enemiesValues
                .FirstOrDefault(e => e.EnemyName.Equals(closestEnemy.enemyType.enemyName))?
                .AbsorbDistance
                    ?? 5;
            energyNetwork.closestLightSource = closestEnemy.gameObject;
            return energyNetwork.closestLightSource != null;
        }

        absorbDistance = 5;
        if (!isOutside)
        {
            path1 = new NavMeshPath();
            energyNetwork.closestLightSource = RoundManager.Instance.allPoweredLightsAnimators?
                .Where(p => p != null && !LFCPoweredLightsRegistry.IsLocked(p) && agent.CalculatePath(ChooseClosestNodeToPosition(p.transform.position).position, path1))
                .OrderBy(p => Vector3.Distance(transform.position, p.transform.position))
                .FirstOrDefault()?
                .gameObject;
        }
        else
        {
            energyNetwork.closestLightSource = RoundManager.Instance.allPoweredLightsAnimators?
                .Where(p => p != null && !LFCPoweredLightsRegistry.IsLocked(p))
                .OrderBy(p => p ? Vector3.Distance(LFCEnemyManager.GetEntranceExitPosition(this.GetClosestEntrance()), p.transform.position) : float.MaxValue)
                .FirstOrDefault()?
                .gameObject;
        }
        return energyNetwork.closestLightSource != null;
    }

    public bool CanAbsorbShip()
        => TimeOfDay.Instance.hour >= ConfigManager.shipMinHour.Value
        && ((ConfigManager.disableShipLights.Value && !LFCShipFeatureRegistry.IsLocked(LFCShipFeatureRegistry.ShipFeatureType.SHIP_LIGHTS))
            || (ConfigManager.disableShipScreen.Value && !LFCShipFeatureRegistry.IsLocked(LFCShipFeatureRegistry.ShipFeatureType.MAP_SCREEN))
            || (ConfigManager.disableShipDoors.Value && !LFCShipFeatureRegistry.IsLocked(LFCShipFeatureRegistry.ShipFeatureType.SHIP_DOORS))
            || (ConfigManager.disableShipLever.Value && !LFCShipFeatureRegistry.IsLocked(LFCShipFeatureRegistry.ShipFeatureType.SHIP_LEVER))
            || (ConfigManager.disableTerminal.Value && !LFCShipFeatureRegistry.IsLocked(LFCShipFeatureRegistry.ShipFeatureType.SHIP_TERMINAL))
            || (ConfigManager.disableItemCharger.Value && !LFCShipFeatureRegistry.IsLocked(LFCShipFeatureRegistry.ShipFeatureType.ITEM_CHARGER))
            || (ConfigManager.disableShipTV.Value && !LFCShipFeatureRegistry.IsLocked(LFCShipFeatureRegistry.ShipFeatureType.SHIP_TV))
            || (ConfigManager.disableShipTeleporters.Value && !LFCShipFeatureRegistry.IsLocked(LFCShipFeatureRegistry.ShipFeatureType.SHIP_TELEPORTERS))
            || (ConfigManager.disableSmartCupboard.Value && !LFCShipFeatureRegistry.IsLocked(LFCShipFeatureRegistry.ShipFeatureType.SMART_CUPBOARD)));

    public void DoHunting()
    {
        agent.speed = 4f;
        if (this.FoundClosestPlayerInRange(25, 10))
        {
            energyNetwork.closestLightSource = null;
            SwitchToBehaviourClientRpc((int)State.CHASING);
            return;
        }
        if (CloseToLightSource())
        {
            DoAnimationEveryoneRpc("startAbsorb");
            SwitchToBehaviourClientRpc((int)State.ABSORBING);
            return;
        }
    }

    public bool CloseToLightSource()
    {
        _ = CrossingLight();
        object lightSource = LightEnergyManager.DetermineLightSource(energyNetwork.closestLightSource);

        if ((lightSource is ShipLights && !isOutside)
            || (lightSource is EnemyAI enemy && enemy.isOutside != isOutside)
            || (lightSource is Animator && isOutside))
        {
            this.GoTowardsEntrance();
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
        if (energyNetwork.closestLightSource == null || !StartOfRound.Instance.shipHasLanded)
        {
            StartSearch(transform.position);
            DoAnimationEveryoneRpc("startMove");
            SwitchToBehaviourClientRpc((int)State.WANDERING);
            return;
        }
        energyNetwork.HandleLight(energyNetwork.closestLightSource);
    }

    public void DoChasing()
    {
        if (stunCoroutine != null || swingCoroutine != null) return;

        agent.speed = 5f;
        if (this.TargetOutsideChasedPlayer()) return;
        if (!this.TargetClosestPlayerInAnyCase(out float distanceWithPlayer) || (distanceWithPlayer > 20f && !CheckLineOfSightForPosition(targetPlayer.transform.position)))
        {
            StartSearch(transform.position);
            SwitchToBehaviourClientRpc((int)State.WANDERING);
            return;
        }
        if (!LSStatusEffectRegistry.HasStatus(targetPlayer.gameObject, LSStatusEffectRegistry.StatusEffectType.LIGHTNING))
        {
            if (StunExplosion()) return;
            if (CrossingLight())
            {
                DoAnimationEveryoneRpc("startAbsorb");
                SwitchToBehaviourClientRpc((int)State.ABSORBING);
                return;
            }
        }
        SetMovingTowardsTargetPlayer(targetPlayer);
    }

    public bool StunExplosion()
    {
        if (energyNetwork.currentCharge >= 100 && targetPlayer.HasLineOfSightToPosition(eye.position, width: 60f, range: 12, proximityAwareness: 2f))
        {
            agent.speed = 0f;
            energyNetwork.currentCharge -= 100;
            stunCoroutine = StartCoroutine(StunExplosionCoroutine());
            return true;
        }
        return false;
    }

    public IEnumerator StunExplosionCoroutine()
    {
        DoAnimationEveryoneRpc("startExplode");
        PlayAudioEveryoneRpc((int)Sound.CHARGE);

        yield return this.WaitForFullAnimation("explode");

        PlayAudioEveryoneRpc((int)Sound.EXPLOSION);
        DoAnimationEveryoneRpc("startMove");
        StunExplosionEveryoneRpc(eye.position, applyStatus: true);

        stunCoroutine = null;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void StunExplosionEveryoneRpc(Vector3 position, bool applyStatus) => LightEnergyManager.StunExplosion(position, applyStatus, enemyWhoExplode: this);

    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);

        if (isEnemyDead || currentBehaviourStateIndex != (int)State.CHASING || stunCoroutine != null || swingCoroutine != null) return;
        PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other);
        if (!LFCUtilities.ShouldBeLocalPlayer(player)) return;

        SwingServerRpc((int)player.playerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SwingServerRpc(int playerId) => swingCoroutine ??= StartCoroutine(SwingCoroutine(playerId));

    public IEnumerator SwingCoroutine(int playerId)
    {
        DoAnimationEveryoneRpc("startSwing");
        PlayAudioEveryoneRpc((int)Sound.SWING);

        yield return this.WaitForFullAnimation("swing");
        LFCNetworkManager.Instance.DamagePlayerEveryoneRpc(playerId, ConfigManager.damage.Value, hasDamageSFX: true, callRPC: true, (int)CauseOfDeath.Crushing);

        DoAnimationEveryoneRpc("startMove");
        if (StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>().isPlayerDead)
        {
            StartSearch(transform.position);
            SwitchToBehaviourClientRpc((int)State.WANDERING);
        }

        swingCoroutine = null;
    }

    public bool CrossingLight()
    {
        GameObject crossedLight = RoundManager.Instance.allPoweredLightsAnimators?
            .FirstOrDefault(p => p != null && !LFCPoweredLightsRegistry.IsLocked(p) && Vector3.Distance(transform.position, p.transform.position) <= 10f)?
            .gameObject
            ?? LFCSpawnRegistry.GetAllAs<GrabbableObject>()?
                .FirstOrDefault(g => LightEnergyManager.CanBeAbsorbed(g, transform.position, 10f))?
                .gameObject
            ?? LFCSpawnRegistry.GetSetExact<Turret>()?
                .Cast<Turret>()?
                .FirstOrDefault(t => t != null && t.turretActive && Vector3.Distance(transform.position, t.transform.position) <= 10f)?
                .gameObject
            ?? LFCSpawnRegistry.GetSetExact<Landmine>()?
                .Cast<Landmine>()?
                .FirstOrDefault(l => l != null && l.mineActivated && Vector3.Distance(transform.position, l.transform.position) <= 10f)?
                .gameObject;

        if (crossedLight != null)
        {
            absorbDistance = 5;
            energyNetwork.closestLightSource = crossedLight;
        }

        return energyNetwork.closestLightSource != null;
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (!isEnemyDead)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            enemyHP -= force;

            if (IsOwner)
            {
                if (energyNetwork.currentCharge > 0)
                    StunExplosionEveryoneRpc(eye.position, applyStatus: energyNetwork.currentCharge >= 100);
                if (enemyHP <= 0)
                    KillEnemyOnOwnerClient();
                energyNetwork.currentCharge = 0;
            }
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy();

        if (LFCUtilities.IsServer && ConfigManager.isDeluminator.Value)
            _ = LFCObjectsManager.SpawnObjectForServer(LightEater.deluminatorObj, transform.position + (Vector3.up * 0.5f));
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void PlayAudioEveryoneRpc(int enemySound)
    {
        if (LightEaterSounds.Length > 0)
            creatureSFX.PlayOneShot(LightEaterSounds[enemySound]);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void DoAnimationEveryoneRpc(string animationState) => creatureAnimator.SetTrigger(animationState);
}

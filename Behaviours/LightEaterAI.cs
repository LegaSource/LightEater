using GameNetcodeStuff;
using LightEater.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace LightEater.Behaviours
{
    public class LightEaterAI : EnemyAI
    {
        public float currentCharge = 0f;

        public Transform TurnCompass;
        public AudioClip[] MoveSounds = Array.Empty<AudioClip>();
        public AudioClip AbsorptionSound;
        public AudioClip SwingSound;
        public AudioClip ChargeSound;
        public AudioClip ExplosionSound;
        public float moveTimer = 0f;

        public List<EntranceTeleport> entrances;

        public GameObject closestLightSource;
        private float explodeTimer = 0f;
        public bool absorbPlayerObject = false;

        public Coroutine absorbLightCoroutine;
        public Coroutine attackCoroutine;

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

            currentBehaviourStateIndex = (int)State.WANDERING;
            creatureAnimator.SetTrigger("startWalk");
            StartSearch(transform.position);

            entrances = FindObjectsOfType<EntranceTeleport>().ToList();
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
                    agent.speed = 2f;
                    if (FoundClosestPlayerInRange(25f, 10f))
                    {
                        StopSearch(currentSearch);
                        DoAnimationClientRpc("startRun");
                        SwitchToBehaviourClientRpc((int)State.CHASING);
                        return;
                    }
                    if (FoundLightSource())
                    {
                        StopSearch(currentSearch);
                        DoAnimationClientRpc("startRun");
                        SwitchToBehaviourClientRpc((int)State.HUNTING);
                        return;
                    }
                    break;
                case (int)State.HUNTING:
                    agent.speed = ConfigManager.huntingSpeed.Value;
                    if (FoundClosestPlayerInRange(25f, 10f))
                    {
                        SwitchToBehaviourClientRpc((int)State.CHASING);
                        return;
                    }
                    if (CloseToLightSource())
                    {
                        DoAnimationClientRpc("startAbsorb");
                        SwitchToBehaviourClientRpc((int)State.ABSORBING);
                        return;
                    }
                    break;
                case (int)State.ABSORBING:
                    agent.speed = 0f;
                    if (closestLightSource == null)
                    {
                        StartSearch(transform.position);
                        DoAnimationClientRpc("startWalk");
                        SwitchToBehaviourClientRpc((int)State.WANDERING);
                        return;
                    }
                    AbsorbLight();
                    break;
                case (int)State.CHASING:
                    agent.speed = ConfigManager.chasingSpeed.Value;
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
                    if (AbsorbPlayerObject() || CrossingLight())
                    {
                        absorbPlayerObject = false;
                        DoAnimationClientRpc("startAbsorb");
                        SwitchToBehaviourClientRpc((int)State.ABSORBING);
                        return;
                    }
                    SetMovingTowardsTargetPlayer(targetPlayer);
                    break;

                default:
                    break;
            }
        }

        public bool FoundClosestPlayerInRange(float range, float senseRange)
        {
            TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: true);
            if (targetPlayer == null)
            {
                TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: false);
                range = senseRange;
            }
            return targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.transform.position) < range;
        }

        public bool FoundLightSource()
        {
            // Si on croise une lumière sur le chemin
            if (CrossingLight()) return true;

            // Sinon se diriger vers la lumière la plus proche en priorisant les Old Birds
            if (LightEater.radMechAIs.Any(r => !r.isEnemyDead))
            {
                closestLightSource = LightEater.radMechAIs.Where(r => !r.isEnemyDead)
                    .OrderBy(r => Vector3.Distance(transform.position, r.transform.position))
                    .FirstOrDefault()
                    .gameObject;
            }
            else
            {
                if (!isOutside)
                {
                    path1 = new NavMeshPath();
                    closestLightSource = RoundManager.Instance.allPoweredLightsAnimators
                        .Where(l => agent.CalculatePath(ChooseClosestNodeToPosition(l.transform.position).position, path1))
                        .OrderBy(l => Vector3.Distance(transform.position, l.transform.position))
                        .FirstOrDefault()
                        .gameObject;
                }
                else
                {
                    closestLightSource = RoundManager.Instance.allPoweredLightsAnimators
                        .OrderBy(l => Vector3.Distance(GetEntranceExitPosition(GetClosestEntrance()), l.transform.position))
                        .FirstOrDefault()
                        .gameObject;
                }
            }
            return closestLightSource != null;
        }

        public bool CrossingLight()
        {
            closestLightSource = RoundManager.Instance.allPoweredLightsAnimators
                .FirstOrDefault(l => Vector3.Distance(transform.position, l.transform.position) <= 10f)?
                .gameObject
                ?? LightEater.grabbableObjects
                    .FirstOrDefault(g => g.insertedBattery.charge > 0f && Vector3.Distance(transform.position, g.transform.position) <= 10f)?
                    .gameObject;
            return closestLightSource != null;
        }

        public bool CloseToLightSource()
        {
            object lightSource = DetermineLightSource();

            if ((lightSource is RadMechAI && !isOutside)
                || (lightSource is Animator && isOutside))
            {
                GoTowardsEntrance();
                return false;
            }

            Vector3 closestPosition = lightSource is RadMechAI ? closestLightSource.transform.position : ChooseClosestNodeToPosition(closestLightSource.transform.position).position;
            SetDestinationToPosition(closestPosition);
            return Vector3.Distance(eye.transform.position, closestLightSource.transform.position) < 5f || Vector3.Distance(transform.position, closestPosition) < 1f;
        }

        public void AbsorbLight()
        {
            if (absorbLightCoroutine != null) return;

            NetworkObject networkObject = closestLightSource.GetComponent<NetworkObject>();
            if (networkObject != null)
                AbsorbLightClientRpc(networkObject);
            else
                AbsorbLightClientRpc(closestLightSource.transform.position);
        }

        [ClientRpc]
        public void AbsorbLightClientRpc(NetworkObjectReference obj)
        {
            if (obj.TryGet(out NetworkObject networkObject))
            {
                closestLightSource = networkObject.gameObject;
                absorbLightCoroutine ??= StartCoroutine(AbsorbLightCoroutine());
            }
        }

        // Si aucun NetworkObject, il s'agit d'une lumière
        [ClientRpc]
        public void AbsorbLightClientRpc(Vector3 position)
        {
            closestLightSource = RoundManager.Instance.allPoweredLightsAnimators
                .OrderBy(l => Vector3.Distance(position, l.transform.position))
                .FirstOrDefault()
                .gameObject;
            absorbLightCoroutine ??= StartCoroutine(AbsorbLightCoroutine());
        }

        public IEnumerator AbsorbLightCoroutine()
        {
            float absorbDuration = 5f;
            float timePassed = 0f;
            object lightSource = DetermineLightSource();

            creatureSFX.PlayOneShot(AbsorptionSound);

            if (lightSource is GrabbableObject grabbableObject)
            {
                absorbDuration *= grabbableObject.insertedBattery.charge;
                if (grabbableObject is FlashlightItem flashlight)
                {
                    flashlight.flashlightAudio.PlayOneShot(flashlight.flashlightFlicker);
                    WalkieTalkie.TransmitOneShotAudio(flashlight.flashlightAudio, flashlight.flashlightFlicker, 0.8f);
                    flashlight.flashlightInterferenceLevel = 1;
                }
            }

            while (timePassed < absorbDuration)
            {
                yield return new WaitForSeconds(0.5f);
                timePassed += 0.5f;

                if (!HandleLightConsumption(lightSource, absorbDuration, timePassed)) break;
            }

            HandleLightDepletion(lightSource);
            closestLightSource = null;
            absorbLightCoroutine = null;
        }

        public object DetermineLightSource()
        {
            if (closestLightSource.TryGetComponent(out GrabbableObject grabbableObject))
                return grabbableObject;

            if (closestLightSource.GetComponentInParent<RadMechAI>() is RadMechAI radMech)
                return radMech;

            return RoundManager.Instance.allPoweredLightsAnimators
                .FirstOrDefault(l => l.gameObject == closestLightSource);
        }

        public bool HandleLightConsumption(object lightSource, float absorbDuration, float timePassed)
        {
            switch (lightSource)
            {
                case RadMechAI radMech:
                    radMech.FlickerFace();
                    return !radMech.isEnemyDead;
                case GrabbableObject grabbableObject:
                    grabbableObject.insertedBattery.charge = Mathf.Max(0f, 1f - (timePassed + (5f - absorbDuration)) / 5f);
                    return Vector3.Distance(transform.position, grabbableObject.transform.position) <= 15f;
                case Animator poweredLightAnimator:
                    poweredLightAnimator?.SetTrigger("Flicker");
                    break;
            }
            return true;
        }

        public void HandleLightDepletion(object lightSource)
        {
            switch (lightSource)
            {
                case RadMechAI radMech:
                    currentCharge += 1f;
                    if (!radMech.isEnemyDead)
                    {
                        GameObject gameObject = Instantiate(radMech.enemyType.nestSpawnPrefab, radMech.transform.position, radMech.transform.rotation);
                        LightEater.radMechAIs.Remove(radMech);
                        radMech.KillEnemyOnOwnerClient(true);
                    }
                    break;
                case GrabbableObject grabbableObject:
                    currentCharge += 0.2f;
                    if (grabbableObject is FlashlightItem flashlight)
                        flashlight.flashlightInterferenceLevel = 0;
                    break;
                case Animator:
                    currentCharge += 0.2f;
                    if (lightSource is Animator poweredLightAnimator)
                    {
                        poweredLightAnimator.SetBool("on", false);
                        RoundManager.Instance.allPoweredLightsAnimators.RemoveAll(l => l.gameObject == closestLightSource);
                    }
                    break;
            }
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
            if (targetPlayer == null) return false;
            return true;
        }

        public bool AbsorbPlayerObject()
        {
            if (Vector3.Distance(transform.position, targetPlayer.transform.position) > 15f) return false;
            if (absorbPlayerObject) return true;

            AbsorbPlayerObjectClientRpc((int)targetPlayer.playerClientId);
            return absorbPlayerObject;
        }

        [ClientRpc]
        public void AbsorbPlayerObjectClientRpc(int playerId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
            if (player != GameNetworkManager.Instance.localPlayerController) return;

            for (int i = 0; i < player.ItemSlots.Length; i++)
            {
                GrabbableObject grabbableObject = player.ItemSlots[i];
                if (grabbableObject != null && grabbableObject.itemProperties.requiresBattery && grabbableObject.insertedBattery.charge > 0f)
                {
                    closestLightSource = grabbableObject.gameObject;
                    return;
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void AbsorbPlayerObjectServerRpc(NetworkObjectReference obj)
        {
            if (obj.TryGet(out NetworkObject networkObject))
            {
                absorbPlayerObject = true;
                closestLightSource = networkObject.gameObject;
            }
        }

        public bool StunExplosion()
        {
            if (currentCharge < 1f || explodeTimer < 10f) return false;
            if (!targetPlayer.HasLineOfSightToPosition(eye.position, 60f, 15)) return false;

            agent.speed = 0f;
            currentCharge -= 1f;
            explodeTimer = 0f;

            StunExplosionClientRpc();
            return true;
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
            StunGrenadeItem.StunExplosion(eye.position, affectAudio: true, flashSeverityMultiplier: 9999f, enemyStunTime: 2f);
            creatureAnimator.SetTrigger("startRun");
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);

            if (currentBehaviourStateIndex != (int)State.CHASING) return;
            if (attackCoroutine != null) return;

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

        public void GoTowardsEntrance()
        {
            EntranceTeleport entranceTeleport = GetClosestEntrance();

            if (Vector3.Distance(transform.position, entranceTeleport.entrancePoint.position) < 1f)
            {
                Vector3 exitPosition = GetEntranceExitPosition(entranceTeleport);
                TeleportEnemyClientRpc(exitPosition, !isOutside);
                return;
            }

            SetDestinationToPosition(entranceTeleport.entrancePoint.position);
        }

        public EntranceTeleport GetClosestEntrance()
        {
            return entrances.Where(e => e.isEntranceToBuilding == isOutside)
                .OrderBy(e => Vector3.Distance(transform.position, e.entrancePoint.position))
                .FirstOrDefault();
        }

        public Vector3 GetEntranceExitPosition(EntranceTeleport entranceTeleport)
        {
            return entrances.FirstOrDefault(e => e.isEntranceToBuilding != entranceTeleport.isEntranceToBuilding && e.entranceId == entranceTeleport.entranceId)
                    .entrancePoint
                    .position;
        }

        [ClientRpc]
        public void TeleportEnemyClientRpc(Vector3 teleportPosition, bool isOutside)
        {
            SetEnemyOutside(isOutside);
            serverPosition = teleportPosition;
            transform.position = teleportPosition;
            agent.Warp(teleportPosition);
            SyncPositionToClients();
        }

        [ServerRpc(RequireOwnership = false)]
        public void DoAnimationServerRpc(string animationState)
            => DoAnimationClientRpc(animationState);

        [ClientRpc]
        public void DoAnimationClientRpc(string animationState)
            => creatureAnimator.SetTrigger(animationState);
    }
}

using GameNetcodeStuff;
using LightEater.Managers;
using LightEater.Patches;
using LightEater.Values;
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

        bool FoundClosestPlayerInRange(int range, int senseRange)
        {
            PlayerControllerB player = CheckLineOfSightForPlayer(60f, range, senseRange);
            if (player == null || !PlayerIsTargetable(player)) return false;

            return targetPlayer = player;
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
            SetDestinationToPosition(closestPlayer.transform.position);
            return true;
        }

        public bool CloseToLightSource()
        {
            CrossingLight();
            object lightSource = DetermineLightSource();

            if ((lightSource is ShipLights && !isOutside)
                || (lightSource is EnemyAI enemy && enemy.isOutside != isOutside)
                || (lightSource is Animator && isOutside))
            {
                GoTowardsEntrance();
                return false;
            }

            Vector3 closestPosition = lightSource switch
            {
                ShipLights => StartOfRound.Instance.shipLandingPosition.position,
                EnemyAI => closestLightSource.transform.position,
                _ => ChooseClosestNodeToPosition(closestLightSource.transform.position).position,
            };
            SetDestinationToPosition(closestPosition);
            return Vector3.Distance(eye.transform.position, closestLightSource.transform.position) < absorbDistance || Vector3.Distance(transform.position, closestPosition) < 1f;
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
            bool isAbsorbed = true;
            float absorbDuration = 5f;
            float timePassed = 0f;

            creatureSFX.PlayOneShot(AbsorptionSound);
            HandleLightInitialization(lightSource, ref absorbDuration);

            while (timePassed < absorbDuration)
            {
                yield return new WaitForSeconds(0.5f);
                timePassed += 0.5f;

                if (!HandleLightConsumption(lightSource, absorbDuration, timePassed))
                {
                    isAbsorbed = false;
                    break;
                }
            }

            if (lightSource is FlashlightItem flashlight) flashlight.flashlightInterferenceLevel = 0;
            if (isAbsorbed) HandleLightDepletion(lightSource);

            closestLightSource = null;
            absorbCoroutine = null;
        }

        public void HandleLightInitialization(object lightSource, ref float absorbDuration)
        {
            switch (lightSource)
            {
                case GrabbableObject grabbableObject:
                    absorbDuration *= grabbableObject.insertedBattery.charge;
                    if (lightSource is FlashlightItem flashlight)
                    {
                        flashlight.flashlightAudio.PlayOneShot(flashlight.flashlightFlicker);
                        WalkieTalkie.TransmitOneShotAudio(flashlight.flashlightAudio, flashlight.flashlightFlicker, 0.8f);
                        flashlight.flashlightInterferenceLevel = 1;
                    }
                    break;

                case ShipLights:
                    StartOfRound.Instance.PowerSurgeShip();
                    StartOfRound.Instance.shipAnimatorObject.gameObject.GetComponent<Animator>().SetBool("AlarmRinging", value: true);
                    StartOfRound.Instance.shipDoorAudioSource.PlayOneShot(StartOfRound.Instance.alarmSFX);
                    break;
            }
        }

        public bool HandleLightConsumption(object lightSource, float absorbDuration, float timePassed)
        {
            switch (lightSource)
            {
                case ShipLights:
                    StartOfRound.Instance.shipDoorsAnimator.SetBool("Closed", value: !StartOfRound.Instance.shipDoorsAnimator.GetBool("Closed"));
                    break;

                case EnemyAI enemy:
                    if (enemy is RadMechAI radMech)
                    {
                        radMech.FlickerFace();
                        if (radMech.inFlyingMode) return false;
                    }
                    return !enemy.isEnemyDead;

                case GrabbableObject grabbableObject:
                    grabbableObject.insertedBattery.charge = Mathf.Max(0f, 1f - (timePassed + (5f - absorbDuration)) / 5f);
                    return !(grabbableObject.insertedBattery.charge > 0 && !CanBeAbsorbed(grabbableObject, 15f));

                case Turret turret:
                    turret.SwitchTurretMode((int)TurretMode.Berserk);
                    break;

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
                case ShipLights:
                    currentCharge += 200;
                    ShipLightsPatch.hasBeenAbsorbed = true;
                    StartOfRoundPatch.EnablesShipFunctionalities(false);
                    StartOfRound.Instance.shipAnimatorObject.gameObject.GetComponent<Animator>().SetBool("AlarmRinging", value: false);
                    break;

                case EnemyAI enemy:
                    EnemyValue enemyValue = ConfigManager.enemiesValues.FirstOrDefault(e => e.EnemyName.Equals(enemy.enemyType.enemyName));
                    currentCharge += enemyValue?.AbsorbCharge ?? 20;

                    if (!IsOwner) break;
                    LightEater.enemies.Remove(enemy);

                    if (enemy.isEnemyDead) break;

                    switch (enemy.enemyType.enemyName)
                    {
                        case Constants.OLD_BIRD_NAME:
                            GameObject gameObject = Instantiate(enemy.enemyType.nestSpawnPrefab, enemy.transform.position, enemy.transform.rotation);
                            gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
                            break;
                    }
                    enemy.KillEnemyOnOwnerClient(enemyValue?.Destroy ?? true);
                    break;

                case GrabbableObject grabbableObject:
                    currentCharge += ConfigManager.itemCharge.Value;
                    LightEater.grabbableObjects.Remove(grabbableObject);
                    break;

                case Turret turret:
                    currentCharge += ConfigManager.turretCharge.Value;
                    
                    turret.ToggleTurretEnabledLocalClient(false);
                    turret.mainAudio.Stop();
                    turret.farAudio.Stop();
                    turret.berserkAudio.Stop();
                    if (turret.fadeBulletAudioCoroutine != null) StopCoroutine(turret.fadeBulletAudioCoroutine);
                    turret.fadeBulletAudioCoroutine = StartCoroutine(turret.FadeBulletAudio());
                    turret.bulletParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
                    turret.turretAnimator.SetInteger("TurretMode", 0);

                    LightEater.turrets.Remove(turret);
                    break;

                case Landmine landmine:
                    currentCharge += ConfigManager.landmineCharge.Value;
                    landmine.ToggleMineEnabledLocalClient(false);
                    LightEater.landmines.Remove(landmine);
                    break;

                case Animator:
                    currentCharge += ConfigManager.lightCharge.Value;
                    if (lightSource is Animator poweredLightAnimator)
                    {
                        poweredLightAnimator.SetBool("on", false);
                        RoundManager.Instance.allPoweredLightsAnimators.RemoveAll(l => l.gameObject == closestLightSource);
                    }
                    break;
            }
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
            if (AbsorbPlayerObject() || CrossingLight())
            {
                absorbPlayerObject = false;
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
                if (!CanBeAbsorbed(grabbableObject, 15f)) continue;

                absorbDistance = 5;
                closestLightSource = grabbableObject.gameObject;
                return;
            }
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
        {
            if (grabbableObject == null) return false;
            if (!grabbableObject.itemProperties.requiresBattery) return false;
            if (grabbableObject.insertedBattery == null || grabbableObject.insertedBattery.charge <= 0f) return false;
            if (Vector3.Distance(transform.position, grabbableObject.transform.position) > distance) return false;
            if (grabbableObject is PatcherTool patcherTool && patcherTool.isShocking) return false;
            return true;
        }

        public object DetermineLightSource()
        {
            if (closestLightSource.GetComponentInParent<ShipLights>() is ShipLights shipLights) return shipLights;
            if (closestLightSource.TryGetComponent(out GrabbableObject grabbableObject)) return grabbableObject;
            if (closestLightSource.GetComponentInParent<Turret>() is Turret turret) return turret;
            if (closestLightSource.GetComponentInParent<Landmine>() is Landmine landmine) return landmine;
            if (closestLightSource.GetComponentInParent<EnemyAI>() is EnemyAI enemy) return enemy;
            return RoundManager.Instance.allPoweredLightsAnimators.FirstOrDefault(l => l?.gameObject == closestLightSource);
        }

        public void GoTowardsEntrance()
        {
            EntranceTeleport entranceTeleport = GetClosestEntrance();

            if (Vector3.Distance(transform.position, entranceTeleport.entrancePoint.position) < 1f)
            {
                Vector3 exitPosition = GetEntranceExitPosition(entranceTeleport);
                StartCoroutine(TeleportEnemyCoroutine(exitPosition));
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
            agent.Warp(teleportPosition);
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
            => killCoroutine = StartCoroutine(KillEnemyCoroutine(destroy));

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
}

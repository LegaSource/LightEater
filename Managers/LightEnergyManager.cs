using GameNetcodeStuff;
using LegaFusionCore.Registries;
using LegaFusionCore.Utilities;
using LethalStatus.Managers;
using System.Linq;
using UnityEngine;
using static LethalStatus.StatusEffects.LSStatusEffectRegistry;

namespace LightEater.Managers;
public class LightEnergyManager
{
    public static object DetermineLightSource(GameObject lightSource)
        => lightSource.GetComponentInChildren<ShipLights>() is ShipLights shipLights ? shipLights
            : lightSource.TryGetComponent(out GrabbableObject grabbableObject) ? grabbableObject
            : lightSource.GetComponentInChildren<Turret>() is Turret turret ? turret
            : lightSource.GetComponentInChildren<Landmine>() is Landmine landmine ? landmine
            : lightSource.GetComponentInChildren<EnemyAI>() is EnemyAI enemy ? enemy
            : RoundManager.Instance.allPoweredLightsAnimators?.FirstOrDefault(l => l?.gameObject == lightSource);

    public static bool CanBeAbsorbed(GrabbableObject grabbableObject, Vector3 position, float distance)
        => grabbableObject != null
            && grabbableObject.itemProperties.requiresBattery
            && grabbableObject.insertedBattery?.charge > 0f
            && IsObjectClose(grabbableObject, position, distance);

    public static bool IsObjectClose(GrabbableObject grabbableObject, Vector3 position, float distance)
    {
        if (Vector3.Distance(position, grabbableObject.transform.position) <= distance) return true;

        foreach (BeltBagItem beltBag in LFCSpawnRegistry.GetAllAs<BeltBagItem>())
        {
            if (beltBag == null || Vector3.Distance(position, beltBag.transform.position) > distance) continue;
            if (beltBag.objectsInBag.FirstOrDefault(o => o == grabbableObject) != null) return true;
        }
        return false;
    }

    public static void StunExplosion(Vector3 position, bool applyStatus, PlayerControllerB playerHeldBy = null, EnemyAI enemyWhoExplode = null)
    {
        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
        if (player.isPlayerDead && player.spectatedPlayerScript != null)
            player = GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript;

        if (player != playerHeldBy && player.HasLineOfSightToPosition(position, width: 60f, range: 12, proximityAwareness: 4f))
        {
            if (applyStatus)
            {
                LSNetworkManager.Instance.ApplyStatusEveryoneRpc(playerId: playerHeldBy != null ? (int)playerHeldBy.playerClientId : -1,
                    targetId: (int)player.playerClientId,
                    type: (int)StatusEffectType.LIGHTNING,
                    duration: 10,
                    totalDamage: 10,
                    enemyWhoHitObj: enemyWhoExplode != null ? enemyWhoExplode.NetworkObject : default);
            }
            HUDManager.Instance.flashFilter = 1f;
            SoundManager.Instance.earsRingingTimer = 1f;
        }

        Collider[] overlapBuffer = new Collider[64];
        int count = Physics.OverlapSphereNonAlloc(position, 12f, overlapBuffer, 524288, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider collider = overlapBuffer[i];
            if (collider != null && collider.gameObject.TryGetComponentInParent(out EnemyAICollisionDetect collisionDetect))
            {
                EnemyAI enemy = collisionDetect.mainScript;
                if (enemy != null && enemy != enemyWhoExplode && !enemy.isEnemyDead && enemy.NetworkObject != null && enemy.CheckLineOfSightForPosition(position, 120f, 15, 4f))
                {
                    if (applyStatus)
                    {
                        LSNetworkManager.Instance.ApplyStatusEveryoneRpc(playerId: playerHeldBy != null ? (int)playerHeldBy.playerClientId : -1,
                            enemyObj: enemy.NetworkObject,
                            type: (int)StatusEffectType.LIGHTNING,
                            duration: 10,
                            totalDamage: 100,
                            enemyWhoHitObj: enemyWhoExplode != null ? enemyWhoExplode.NetworkObject : default);
                    }
                    enemy.SetEnemyStunned(setToStunned: true, setStunnedByPlayer: playerHeldBy);
                }
            }
        }
    }
}

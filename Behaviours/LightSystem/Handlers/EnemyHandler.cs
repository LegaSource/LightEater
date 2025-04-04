using LightEater.Behaviours.LightSystem.Interfaces;
using LightEater.Managers;
using LightEater.Values;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers;

public class EnemyHandler : ILightSource
{
    protected readonly EnemyAI enemy;

    protected EnemyHandler(EnemyAI enemy)
        => this.enemy = enemy;

    public virtual void HandleLightInitialization(ref float absorbDuration) { }

    public virtual bool HandleLightConsumption(float absorbDuration, float timePassed)
    {
        if (enemy is RadMechAI radMech)
        {
            radMech.FlickerFace();
            if (radMech.inFlyingMode) return false;
        }
        return !enemy.isEnemyDead;
    }

    public virtual void HandleLightDepletion()
    {
        if (!GameNetworkManager.Instance.localPlayerController.IsHost && !GameNetworkManager.Instance.localPlayerController.IsServer) return;

        _ = LightEater.enemies.Remove(enemy);

        if (enemy.isEnemyDead) return;

        switch (enemy.enemyType.enemyName)
        {
            case Constants.OLD_BIRD_NAME:
                GameObject gameObject = Object.Instantiate(enemy.enemyType.nestSpawnPrefab, enemy.transform.position, enemy.transform.rotation);
                gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
                break;
        }
        EnemyValue enemyValue = ConfigManager.enemiesValues.FirstOrDefault(e => e.EnemyName.Equals(enemy.enemyType.enemyName));
        enemy.KillEnemyOnOwnerClient(enemyValue?.Destroy ?? true);
    }

    public virtual Vector3 GetClosestNodePosition()
        => enemy.transform.position;

    public virtual Vector3 GetClosestLightPosition()
        => enemy.transform.position;
}

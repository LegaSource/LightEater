using LightEater.Managers;
using LightEater.Values;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem;

public class EnemyAIHandler(LightEaterAI lightEater, EnemyAI enemy) : ILightSource
{
    private readonly LightEaterAI lightEater = lightEater;
    private readonly EnemyAI enemy = enemy;

    public void HandleLightInitialization(ref float absorbDuration) { }

    public bool HandleLightConsumption(float absorbDuration, float timePassed)
    {
        if (enemy is RadMechAI radMech)
        {
            radMech.FlickerFace();
            if (radMech.inFlyingMode) return false;
        }
        return !enemy.isEnemyDead;
    }

    public void HandleLightDepletion()
    {
        EnemyValue enemyValue = ConfigManager.enemiesValues.FirstOrDefault(e => e.EnemyName.Equals(enemy.enemyType.enemyName));
        lightEater.currentCharge += enemyValue?.AbsorbCharge ?? 20;

        if (!lightEater.IsOwner) return;

        _ = LightEater.enemies.Remove(enemy);

        if (enemy.isEnemyDead) return;

        switch (enemy.enemyType.enemyName)
        {
            case Constants.OLD_BIRD_NAME:
                GameObject gameObject = UnityEngine.Object.Instantiate(enemy.enemyType.nestSpawnPrefab, enemy.transform.position, enemy.transform.rotation);
                gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
                break;
        }
        enemy.KillEnemyOnOwnerClient(enemyValue?.Destroy ?? true);
    }

    public Vector3 GetClosestNodePosition()
        => enemy.transform.position;

    public Vector3 GetClosestLightPosition()
        => enemy.transform.position;
}

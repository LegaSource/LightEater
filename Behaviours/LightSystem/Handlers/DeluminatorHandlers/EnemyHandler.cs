using LegaFusionCore.Utilities;
using LightEater.Managers;
using LightEater.Values;
using System.Linq;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers.DeluminatorHandlers;

public class EnemyHandler(Deluminator deluminator, EnemyAI enemy) : Handlers.EnemyHandler(enemy)
{
    private readonly Deluminator deluminator = deluminator;

    public override bool HandleLightConsumption(float absorbDuration, float remainingDuration, float timePassed)
    {
        if (base.HandleLightConsumption(absorbDuration, remainingDuration, timePassed))
        {
            if (LFCUtilities.IsServer && enemy.targetPlayer == null && deluminator.playerHeldBy != null)
                enemy.SetMovingTowardsTargetPlayer(deluminator.playerHeldBy);
            return Vector3.Distance(deluminator.transform.position, enemy.transform.position) <= 7.5f;
        }
        return false;
    }

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();

        EnemyValue enemyValue = ConfigManager.enemiesValues.FirstOrDefault(e => e.EnemyName.Equals(enemy.enemyType.enemyName));
        deluminator.energyNetwork.currentCharge = Mathf.Min(200, deluminator.energyNetwork.currentCharge + enemyValue?.AbsorbCharge ?? 20);
    }
}

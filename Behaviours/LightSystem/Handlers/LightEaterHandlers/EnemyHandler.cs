using LightEater.Managers;
using LightEater.Values;
using System.Linq;

namespace LightEater.Behaviours.LightSystem.Handlers.LightEaterHandlers;

public class EnemyHandler(LightEaterAI lightEater, EnemyAI enemy) : Handlers.EnemyHandler(enemy)
{
    private readonly LightEaterAI lightEater = lightEater;

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();

        EnemyValue enemyValue = ConfigManager.enemiesValues.FirstOrDefault(e => e.EnemyName.Equals(enemy.enemyType.enemyName));
        lightEater.energyNetwork.currentCharge += enemyValue?.AbsorbCharge ?? 20;
    }

    public override void HandleLightRestoration()
    {
        base.HandleLightRestoration();

        EnemyValue enemyValue = ConfigManager.enemiesValues.FirstOrDefault(e => e.EnemyName.Equals(enemy.enemyType.enemyName));
        lightEater.energyNetwork.currentCharge -= enemyValue?.AbsorbCharge ?? 20;
    }
}

using GameNetcodeStuff;
using LightEater.Managers;
using LightEater.Values;
using System.Linq;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers.DeluminatorHandlers;

public class EnemyHandler(Deluminator deluminator, EnemyAI enemy) : Handlers.EnemyHandler(enemy)
{
    private readonly Deluminator deluminator = deluminator;

    public override bool HandleLightConsumption(float absorbDuration, float timePassed)
    {
        if (!base.HandleLightConsumption(absorbDuration, timePassed)) return false;

        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (localPlayer.IsHost || localPlayer.IsServer)
        {
            PlayerControllerB player = deluminator.playerHeldBy;
            if (player != null) enemy.SetMovingTowardsTargetPlayer(player);
        }

        return Vector3.Distance(deluminator.transform.position, enemy.transform.position) <= 15f;
    }

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();

        EnemyValue enemyValue = ConfigManager.enemiesValues.FirstOrDefault(e => e.EnemyName.Equals(enemy.enemyType.enemyName));
        deluminator.energyNetwork.currentCharge += enemyValue?.AbsorbCharge ?? 20;
    }
}

using HarmonyLib;
using LightEater.Managers;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace LightEater.Patches
{
    internal class RoundManagerPatch
    {
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.LoadNewLevel))]
        [HarmonyPostfix]
        private static void StartGame()
        {
            StormyWeatherPatch.lightEaters.Clear();

            LightEater.enemies.Clear();
            foreach (EnemyAI enemy in Object.FindObjectsOfType<EnemyAI>())
                AddEnemy(enemy);

            LightEater.grabbableObjects.Clear();
            foreach (GrabbableObject grabbableObject in Object.FindObjectsOfType<GrabbableObject>())
                AddGrabbableObject(grabbableObject);

            LightEater.turrets.Clear();
            foreach (Turret turret in Object.FindObjectsOfType<Turret>())
                AddTurret(turret);

            LightEater.landmines.Clear();
            foreach (Landmine landmine in Object.FindObjectsOfType<Landmine>())
                AddLandmine(landmine);
        }

        public static void AddEnemy(EnemyAI enemy)
        {
            if (string.IsNullOrEmpty(enemy.enemyType?.enemyName)) return;
            if (!ConfigManager.enemiesValues.Select(e => e.EnemyName).Contains(enemy.enemyType.enemyName)) return;
            if (LightEater.enemies.Contains(enemy)) return;

            LightEater.enemies.Add(enemy);
        }

        public static void AddGrabbableObject(GrabbableObject grabbableObject)
        {
            if (!grabbableObject.itemProperties.requiresBattery) return;
            if (LightEater.grabbableObjects.Contains(grabbableObject)) return;
            
            LightEater.grabbableObjects.Add(grabbableObject);
        }

        public static void AddTurret(Turret turret)
        {
            if (!turret.turretActive) return;
            if (LightEater.turrets.Contains(turret)) return;

            LightEater.turrets.Add(turret);
        }

        public static void AddLandmine(Landmine landmine)
        {
            if (!landmine.mineActivated) return;
            if (LightEater.landmines.Contains(landmine)) return;

            LightEater.landmines.Add(landmine);
        }
    }
}

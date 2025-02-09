using BepInEx.Configuration;
using BepInEx;
using System.IO;
using System.Reflection;
using UnityEngine;
using LethalLib.Modules;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using LightEater.Managers;
using LightEater.Patches;

namespace LightEater
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class LightEater : BaseUnityPlugin
    {
        private const string modGUID = "Lega.LightEater";
        private const string modName = "Light Eater";
        private const string modVersion = "1.0.2";

        private readonly Harmony harmony = new Harmony(modGUID);
        private readonly static AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lighteater"));
        internal static ManualLogSource mls;
        public static ConfigFile configFile;

        public static List<EnemyAI> enemies = new List<EnemyAI>();
        public static List<GrabbableObject> grabbableObjects = new List<GrabbableObject>();
        public static List<Turret> turrets = new List<Turret>();
        public static List<Landmine> landmines = new List<Landmine>();

        public void Awake()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource("LightEater");
            configFile = Config;
            ConfigManager.Load();
            ConfigManager.GetEnemiesValues();

            NetcodePatcher();
            LoadEnemies();

            harmony.PatchAll(typeof(StartOfRoundPatch));
            harmony.PatchAll(typeof(RoundManagerPatch));
            harmony.PatchAll(typeof(GrabbableObjectPatch));
            harmony.PatchAll(typeof(TurretPatch));
            harmony.PatchAll(typeof(LandminePatch));
            harmony.PatchAll(typeof(EnemyAIPatch));
            if (ConfigManager.interactWithStorm.Value) harmony.PatchAll(typeof(StormyWeatherPatch));
            if (ConfigManager.disableShipLights.Value) harmony.PatchAll(typeof(ShipLightsPatch));
            if (ConfigManager.disableShipDoor.Value) harmony.PatchAll(typeof(HangarShipDoorPatch));
            if (ConfigManager.disableItemCharger.Value) harmony.PatchAll(typeof(ItemChargerPatch));
            if (ConfigManager.disableShipScreen.Value) harmony.PatchAll(typeof(ManualCameraRendererPatch));
            if (ConfigManager.disableShipTeleporters.Value) harmony.PatchAll(typeof(ShipTeleporterPatch));
        }

        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length == 0) continue;
                    method.Invoke(null, null);
                }
            }
        }

        public static void LoadEnemies()
        {
            EnemyType lightEaterEnemy = bundle.LoadAsset<EnemyType>("Assets/LightEater/LightEaterEnemy.asset");
            NetworkPrefabs.RegisterNetworkPrefab(lightEaterEnemy.enemyPrefab);
            Enemies.RegisterEnemy(lightEaterEnemy, ConfigManager.rarity.Value, Levels.LevelTypes.All, null, null);
        }
    }
}

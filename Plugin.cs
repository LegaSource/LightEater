﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Modules;
using LightEater.Behaviours;
using LightEater.Managers;
using LightEater.Patches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LightEater;

[BepInPlugin(modGUID, modName, modVersion)]
public class LightEater : BaseUnityPlugin
{
    private const string modGUID = "Lega.LightEater";
    private const string modName = "Light Eater";
    private const string modVersion = "1.0.7";

    private readonly Harmony harmony = new Harmony(modGUID);
    private static readonly AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lighteater"));
    internal static ManualLogSource mls;
    public static ConfigFile configFile;

    // Items
    public static GameObject deluminatorObj;

    // Materials
    public static Material overchargeShader;

    public static bool isSellBodies = false;

    public void Awake()
    {
        mls = BepInEx.Logging.Logger.CreateLogSource("LightEater");
        configFile = Config;
        ConfigManager.Load();
        ConfigManager.GetEnemiesValues();

        NetcodePatcher();
        LoadEnemies();
        LoadItems();
        LoadShaders();

        harmony.PatchAll(typeof(StartOfRoundPatch));
        harmony.PatchAll(typeof(RoundManagerPatch));
        harmony.PatchAll(typeof(GrabbableObjectPatch));
        harmony.PatchAll(typeof(TurretPatch));
        harmony.PatchAll(typeof(LandminePatch));
        harmony.PatchAll(typeof(EnemyAIPatch));
        harmony.PatchAll(typeof(PatcherToolPatch));
        if (ConfigManager.interactWithStorm.Value) harmony.PatchAll(typeof(StormyWeatherPatch));
        if (ConfigManager.disableShipLights.Value) harmony.PatchAll(typeof(ShipLightsPatch));
        if (ConfigManager.disableShipDoor.Value) harmony.PatchAll(typeof(HangarShipDoorPatch));
        if (ConfigManager.disableItemCharger.Value) harmony.PatchAll(typeof(ItemChargerPatch));
        if (ConfigManager.disableShipScreen.Value) harmony.PatchAll(typeof(ManualCameraRendererPatch));
        if (ConfigManager.disableShipTeleporters.Value) harmony.PatchAll(typeof(ShipTeleporterPatch));
        PatchOtherMods();
    }

    private static void NetcodePatcher()
    {
        Type[] types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (Type type in types)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                object[] attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length == 0) continue;

                _ = method.Invoke(null, null);
            }
        }
    }

    public static void LoadEnemies()
    {
        EnemyType lightEaterEnemy = bundle.LoadAsset<EnemyType>("Assets/LightEater/LightEaterEnemy.asset");
        NetworkPrefabs.RegisterNetworkPrefab(lightEaterEnemy.enemyPrefab);

        (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigManager.GetEnemiesSpawns();
        Enemies.RegisterEnemy(lightEaterEnemy,
            spawnRateByLevelType,
            spawnRateByCustomLevelType,
            bundle.LoadAsset<TerminalNode>("Assets/LightEater/LightEaterTN.asset"),
            bundle.LoadAsset<TerminalKeyword>("Assets/LightEater/LightEaterTK.asset"));
    }

    public void LoadItems()
    {
        if (ConfigManager.isDeluminator.Value) deluminatorObj = RegisterItem(typeof(Deluminator), bundle.LoadAsset<Item>("Assets/Deluminator/DeluminatorItem.asset")).spawnPrefab;
    }

    public Item RegisterItem(Type type, Item item)
    {
        if (item.spawnPrefab.GetComponent(type) == null)
        {
            PhysicsProp script = item.spawnPrefab.AddComponent(type) as PhysicsProp;
            script.grabbable = true;
            script.grabbableToEnemies = true;
            script.itemProperties = item;
        }

        NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
        Utilities.FixMixerGroups(item.spawnPrefab);
        Items.RegisterItem(item);

        return item;
    }

    public static void LoadShaders()
        => overchargeShader = bundle.LoadAsset<Material>("Assets/Shaders/OverchargeMaterial.mat");

    public static void PatchOtherMods()
        => isSellBodies = Type.GetType("SellBodies.MyPluginInfo, SellBodies") != null;
}

using BepInEx.Configuration;
using LethalLib.Modules;
using LightEater.Values;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LightEater.Managers;

public class ConfigManager
{
    public static List<EnemyValue> enemiesValues = [];

    // GLOBAL
    public static ConfigEntry<string> spawnWeights;
    public static ConfigEntry<bool> isDeluminator;
    public static ConfigEntry<int> damage;
    // ENEMIES INTERACTIONS
    public static ConfigEntry<string> absorbedEnemies;
    // SHIP INTERACTIONS
    public static ConfigEntry<int> shipMinHour;
    public static ConfigEntry<bool> disableShipLights;
    public static ConfigEntry<bool> disableShipScreen;
    public static ConfigEntry<bool> disableShipDoors;
    public static ConfigEntry<bool> disableShipLever;
    public static ConfigEntry<bool> disableTerminal;
    public static ConfigEntry<bool> disableItemCharger;
    public static ConfigEntry<bool> disableShipTV;
    public static ConfigEntry<bool> disableShipTeleporters;
    public static ConfigEntry<bool> disableSmartCupboard;

    public static void Load()
    {
        // GLOBAL
        spawnWeights = LightEater.configFile.Bind(Constants.GLOBAL, "Spawn weights", "Vanilla:20,Modded:20", $"{Constants.LIGHT_EATER} spawn weights");
        isDeluminator = LightEater.configFile.Bind(Constants.GLOBAL, "Enable Deluminator", true, $"Is {Constants.DELUMINATOR} item enabled?");
        damage = LightEater.configFile.Bind(Constants.GLOBAL, "Damage", 30, $"{Constants.LIGHT_EATER} damage");
        // ENEMIES INTERACTIONS
        absorbedEnemies = LightEater.configFile.Bind(Constants.ENEMIES_INTERACTIONS, "Enemies list", $"{Constants.OLD_BIRD_NAME}:20:100:True,Boomba:5:20:False,Cleaning Drone:5:20:False,Mobile Turret:5:20:True,Shockwave Drone:5:20:True", $"List of enemies that can be absorbed.\nThe format is 'EnemyName:AbsorbDistance:AbsorbCharge'.");
        // SHIP INTERACTIONS
        shipMinHour = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Min hour", 9, $"Hour at which the {Constants.LIGHT_EATER} can absorb the ship (between 1 and 18)");
        disableShipLights = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable lights", true, "Disable ship lights");
        disableShipScreen = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable screen", true, "Disable ship screen");
        disableShipDoors = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable doors", true, "Disable ship doors");
        disableShipLever = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable lever", false, "Disable ship lever");
        disableTerminal = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable terminal", true, "Disable terminal");
        disableItemCharger = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable item charger", true, "Disable item charger");
        disableShipTV = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable TV", true, "Disable TV");
        disableShipTeleporters = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable teleporters", true, "Disable ship teleporters");
        disableSmartCupboard = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable smart cupboard", true, "Disable smart cupboard (from SelfSortingStorage)");
    }

    public static (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) GetEnemiesSpawns()
    {
        Dictionary<Levels.LevelTypes, int> spawnRateByLevelType = [];
        Dictionary<string, int> spawnRateByCustomLevelType = [];
        foreach (string spawnWeight in spawnWeights.Value.Split(',').Select(s => s.Trim()))
        {
            string[] values = spawnWeight.Split(':');
            if (values.Length != 2) continue;

            string name = values[0];
            if (int.TryParse(values[1], out int spawnRate))
            {
                if (Enum.TryParse(name, ignoreCase: true, out Levels.LevelTypes levelType)) spawnRateByLevelType[levelType] = spawnRate;
                else spawnRateByCustomLevelType[name] = spawnRate;
            }
        }
        return (spawnRateByLevelType, spawnRateByCustomLevelType);
    }

    public static void GetEnemiesValues()
    {
        string[] enemies = absorbedEnemies.Value.Split(',');
        foreach (string itemValue in enemies)
        {
            string[] values = itemValue.Split(':');
            if (values.Length == 4)
            {
                enemiesValues.Add(new EnemyValue(values[0],
                    int.Parse(values[1]),
                    int.Parse(values[2]),
                    bool.Parse(values[3])));
            }
        }
    }
}

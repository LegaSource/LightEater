using BepInEx.Configuration;
using LightEater.Values;
using System.Collections.Generic;

namespace LightEater.Managers;

public class ConfigManager
{
    public static List<EnemyValue> enemiesValues = [];

    // GLOBAL
    public static ConfigEntry<int> rarity;
    public static ConfigEntry<float> huntingSpeed;
    public static ConfigEntry<float> chasingSpeed;
    public static ConfigEntry<int> damage;
    public static ConfigEntry<int> lightCharge;
    public static ConfigEntry<int> itemCharge;
    public static ConfigEntry<int> turretCharge;
    public static ConfigEntry<int> landmineCharge;
    // ENEMIES INTERACTIONS
    public static ConfigEntry<string> absorbedEnemies;
    // SHIP INTERACTIONS
    public static ConfigEntry<int> shipMinHour;
    public static ConfigEntry<bool> disableShipLights;
    public static ConfigEntry<bool> disableShipDoor;
    public static ConfigEntry<bool> disableShipLever;
    public static ConfigEntry<bool> disableItemCharger;
    public static ConfigEntry<bool> disableTerminal;
    public static ConfigEntry<bool> disableShipScreen;
    public static ConfigEntry<bool> disableShipTeleporters;
    // STORM INTERACTIONS
    public static ConfigEntry<bool> interactWithStorm;
    public static ConfigEntry<int> minStrikeDuration;
    public static ConfigEntry<int> maxStrikeDuration;
    public static ConfigEntry<int> stormCharge;

    public static void Load()
    {
        // GLOBAL
        rarity = LightEater.configFile.Bind(Constants.GLOBAL, "Rarity", 20, $"{Constants.LIGHT_EATER} rarity");
        huntingSpeed = LightEater.configFile.Bind(Constants.GLOBAL, "Hunting speed", 3f, $"{Constants.LIGHT_EATER} speed when it moves towards a light");
        chasingSpeed = LightEater.configFile.Bind(Constants.GLOBAL, "Chasing speed", 4f, $"{Constants.LIGHT_EATER} speed when it moves towards a player");
        damage = LightEater.configFile.Bind(Constants.GLOBAL, "Damage", 20, $"{Constants.LIGHT_EATER} damage");
        lightCharge = LightEater.configFile.Bind(Constants.GLOBAL, "Light charge", 20, "Electric charge received upon absorption of a dungeon light");
        itemCharge = LightEater.configFile.Bind(Constants.GLOBAL, "Item charge", 20, "Electric charge received upon absorption of an item");
        turretCharge = LightEater.configFile.Bind(Constants.GLOBAL, "Turret charge", 20, "Electric charge received upon absorption of a turret");
        landmineCharge = LightEater.configFile.Bind(Constants.GLOBAL, "Landmine charge", 10, "Electric charge received upon absorption of a landmine");
        // ENEMIES INTERACTIONS
        absorbedEnemies = LightEater.configFile.Bind(Constants.ENEMIES_INTERACTIONS, "Enemies list", $"{Constants.OLD_BIRD_NAME}:20:100:True,Boomba:5:20:False,Cleaning Drone:5:20:False,Mobile Turret:5:20:True,Shockwave Drone:5:20:True", $"List of enemies that can be absorbed.\nThe format is 'EnemyName:AbsorbDistance:AbsorbCharge'.");
        // SHIP INTERACTIONS
        shipMinHour = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Min hour", 9, $"Hour at which the {Constants.LIGHT_EATER} can absorb the ship (between 1 and 18)");
        disableShipLights = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable lights", true, "Disable ship lights");
        disableShipDoor = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable door", true, "Disable ship door");
        disableShipLever = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable lever", false, "Disable ship lever");
        disableItemCharger = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable item charger", true, "Disable item charger");
        disableTerminal = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable terminal", true, "Disable terminal");
        disableShipScreen = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable screen", true, "Disable ship screen");
        disableShipTeleporters = LightEater.configFile.Bind(Constants.SHIP_INTERACTIONS, "Disable teleporters", true, "Disable ship teleporters");
        // STORM INTERACTIONS
        interactWithStorm = LightEater.configFile.Bind(Constants.STORM_INTERACTIONS, "Enable", true, $"Enable {Constants.STORM_INTERACTIONS}");
        minStrikeDuration = LightEater.configFile.Bind(Constants.STORM_INTERACTIONS, "Min strike duration", 7, "Minimum time interval before a new strike");
        maxStrikeDuration = LightEater.configFile.Bind(Constants.STORM_INTERACTIONS, "Max strike duration", 15, "Maximum time interval before a new strike");
        stormCharge = LightEater.configFile.Bind(Constants.STORM_INTERACTIONS, "Storm charge", 100, "Electrical charge received by lightning");
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

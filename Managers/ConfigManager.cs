using BepInEx.Configuration;

namespace LightEater.Managers
{
    public class ConfigManager
    {
        // Global
        public static ConfigEntry<int> rarity;
        public static ConfigEntry<float> huntingSpeed;
        public static ConfigEntry<float> chasingSpeed;
        public static ConfigEntry<int> damage;

        public static void Load()
        {
            // Global
            rarity = LightEater.configFile.Bind(Constants.GLOBAL, "Rarity", 50, $"{Constants.LIGHT_EATER} rarity");
            huntingSpeed = LightEater.configFile.Bind(Constants.GLOBAL, "Hunting speed", 3f, $"{Constants.LIGHT_EATER} speed when it moves towards a light");
            chasingSpeed = LightEater.configFile.Bind(Constants.GLOBAL, "Chasing speed", 4f, $"{Constants.LIGHT_EATER} speed when it moves towards a player");
            damage = LightEater.configFile.Bind(Constants.GLOBAL, "Damage", 20, $"{Constants.LIGHT_EATER} damage");
        }
    }
}

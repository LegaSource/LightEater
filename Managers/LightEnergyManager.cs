using GameNetcodeStuff;
using LightEater.Behaviours;
using LightEater.Patches;
using LightEater.Values;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LightEater.Managers;
public class LightEnergyManager
{
    public static Dictionary<Animator, bool> poweredLights = [];
    public static Dictionary<EnemyAI, bool> enemies = [];
    public static Dictionary<Turret, bool> turrets = [];
    public static Dictionary<Landmine, bool> landmines = [];
    public static HashSet<GrabbableObject> grabbableObjects = [];
    public static HashSet<BeltBagItem> beltBags = [];

    public static void ResetEnemies()
    {
        enemies.Clear();
        foreach (EnemyAI enemy in Object.FindObjectsOfType<EnemyAI>()) AddEnemy(enemy);
    }

    public static void AddEnemy(EnemyAI enemy)
    {
        if (string.IsNullOrEmpty(enemy.enemyType?.enemyName)) return;
        if (!ConfigManager.enemiesValues.Select(e => e.EnemyName).Contains(enemy.enemyType.enemyName)) return;
        if (enemies.ContainsKey(enemy)) return;

        enemies.Add(enemy, true);
    }

    public static void ResetTurrets()
    {
        turrets.Clear();
        foreach (Turret turret in Object.FindObjectsOfType<Turret>()) AddTurret(turret);
    }

    public static void AddTurret(Turret turret)
    {
        if (!turret.turretActive) return;
        if (turrets.ContainsKey(turret)) return;

        turrets.Add(turret, true);
    }

    public static void ResetLandmines()
    {
        landmines.Clear();
        foreach (Landmine landmine in Object.FindObjectsOfType<Landmine>()) AddLandmine(landmine);
    }

    public static void AddLandmine(Landmine landmine)
    {
        if (!landmine.mineActivated) return;
        if (landmines.ContainsKey(landmine)) return;

        landmines.Add(landmine, true);
    }

    public static void ResetPoweredLights()
    {
        poweredLights.Clear();
        foreach (Animator poweredLight in RoundManager.Instance.allPoweredLightsAnimators) AddPoweredLight(poweredLight);
    }

    public static void AddPoweredLight(Animator poweredLight)
    {
        if (poweredLights.ContainsKey(poweredLight)) return;
        poweredLights.Add(poweredLight, true);
    }

    public static void ResetObjects()
    {
        grabbableObjects.Clear();
        foreach (GrabbableObject grabbableObject in Object.FindObjectsOfType<GrabbableObject>()) AddObject(grabbableObject);
    }

    public static void AddObject(GrabbableObject grabbableObject)
    {
        if (!grabbableObject.itemProperties.requiresBattery) return;
        if (grabbableObjects.Contains(grabbableObject)) return;

        _ = grabbableObjects.Add(grabbableObject);
    }

    public static void ResetBeltBags()
    {
        beltBags.Clear();
        foreach (BeltBagItem beltBag in Object.FindObjectsOfType<BeltBagItem>()) AddBeltBag(beltBag);
    }

    public static void AddBeltBag(BeltBagItem beltBag)
    {
        if (beltBags.Contains(beltBag)) return;
        _ = beltBags.Add(beltBag);
    }

    public static List<EnemyAI> GetEnemies(bool value)
        => enemies.Where(e => e.Value == value).Select(e => e.Key).ToList();

    public static List<Turret> GetTurrets(bool value)
        => turrets.Where(t => t.Value == value).Select(t => t.Key).ToList();

    public static List<Landmine> GetLandmines(bool value)
        => landmines.Where(l => l.Value == value).Select(l => l.Key).ToList();

    public static List<Animator> GetPoweredLights(bool value)
        => poweredLights.Where(p => p.Value == value).Select(p => p.Key).ToList();

    public static void SetEnemyValue(EnemyAI enemy, bool value)
        => enemies[enemy] = value;

    public static void SetTurretValue(Turret turret, bool value)
        => turrets[turret] = value;

    public static void SetLandmineValue(Landmine landmine, bool value)
        => landmines[landmine] = value;

    public static void SetPoweredLightValue(GameObject gameObject, bool value)
    {
        Animator poweredLight = RoundManager.Instance.allPoweredLightsAnimators.FirstOrDefault(l => l?.gameObject == gameObject);
        poweredLights[poweredLight] = value;
    }

    public static GameObject GetLightSourceByName(string objectName, Vector3 position, bool enable) => objectName switch
    {
        Constants.SHIP_LIGHTS => StartOfRound.Instance.shipRoomLights.gameObject,
        Constants.TURRET => GetTurrets(enable)
            .OrderBy(t => t != null ? Vector3.Distance(position, t.transform.position) : float.MaxValue)
            .FirstOrDefault()
            .gameObject,
        Constants.LANDMINE => GetLandmines(enable)
            .OrderBy(l => l != null ? Vector3.Distance(position, l.transform.position) : float.MaxValue)
            .FirstOrDefault()
            .gameObject,
        _ => GetPoweredLights(enable)
            .OrderBy(p => p != null ? Vector3.Distance(position, p.transform.position) : float.MaxValue)
            .FirstOrDefault()
            .gameObject,
    };

    public static GameObject GetClosestLightSourceInView(Deluminator deluminator, bool enable)
    {
        PlayerControllerB player = deluminator?.playerHeldBy;
        if (player == null) return null;

        Ray ray = new Ray(player.gameplayCamera.transform.position, player.gameplayCamera.transform.forward);
        RaycastHit[] hits = Physics.SphereCastAll(ray, 2f, 5f);
        foreach (RaycastHit hit in hits)
        {
            GameObject lightSource = GetEnemies(enable).FirstOrDefault(e => e == hit.collider.GetComponent<EnemyAI>())?.gameObject
                ?? grabbableObjects.FirstOrDefault(o => CanBeTransferred(o, enable) && o == hit.collider.GetComponent<GrabbableObject>())?.gameObject
                ?? GetTurrets(enable).FirstOrDefault(o => o == hit.collider.GetComponent<Turret>())?.gameObject
                ?? GetLandmines(enable).FirstOrDefault(o => o == hit.collider.GetComponent<Landmine>())?.gameObject
                ?? GetPoweredLights(enable).FirstOrDefault(o => o == hit.collider.GetComponent<Animator>())?.gameObject
                ?? ((enable != ShipLightsPatch.hasBeenAbsorbed
                        && (Vector3.Distance(player.transform.position, StartOfRound.Instance.shipLandingPosition.position) <= 20f))
                    ? StartOfRound.Instance.shipRoomLights.gameObject
                    : null);
            if (lightSource == null) continue;
            if (!CanChargeBeApplied(deluminator.energyNetwork.currentCharge, lightSource, enable)) continue;

            return lightSource;
        }
        return null;
    }

    public static bool CanBeTransferred(GrabbableObject grabbableObject, bool enable)
        => grabbableObject.itemProperties.requiresBattery
            && grabbableObject.insertedBattery != null
            && ((enable && grabbableObject.insertedBattery.charge > 0f) || (!enable && grabbableObject.insertedBattery.charge < 1f));

    public static bool CanChargeBeApplied(int currentCharge, GameObject lightSource, bool enable)
    {
        switch (DetermineLightSource(lightSource, enable))
        {
            case ShipLights:
                return (enable && currentCharge <= 0f) || (!enable && currentCharge >= 200f);
            case GrabbableObject:
                return (enable && currentCharge + ConfigManager.itemCharge.Value <= 200f) || (!enable && currentCharge >= ConfigManager.itemCharge.Value);
            case Turret:
                return (enable && currentCharge + ConfigManager.turretCharge.Value <= 200f) || (!enable && currentCharge >= ConfigManager.turretCharge.Value);
            case Landmine:
                return (enable && currentCharge + ConfigManager.landmineCharge.Value <= 200f) || (!enable && currentCharge >= ConfigManager.landmineCharge.Value);
            case EnemyAI enemy:
                EnemyValue enemyValue = ConfigManager.enemiesValues.FirstOrDefault(e => e.EnemyName.Equals(enemy.enemyType.enemyName));
                int enemyCharge = enemyValue?.AbsorbCharge ?? 20;
                return enable && currentCharge + enemyCharge <= 200f;
            case Animator:
                return (enable && currentCharge + ConfigManager.lightCharge.Value <= 200f) || (!enable && currentCharge >= ConfigManager.lightCharge.Value);
        }
        return false;
    }

    public static object DetermineLightSource(GameObject lightSource, bool enable)
        => lightSource.GetComponentInParent<ShipLights>() is ShipLights shipLights
            ? shipLights : lightSource.TryGetComponent(out GrabbableObject grabbableObject)
            ? grabbableObject : lightSource.GetComponentInParent<Turret>() is Turret turret
            ? turret : lightSource.GetComponentInParent<Landmine>() is Landmine landmine
            ? landmine : lightSource.GetComponentInParent<EnemyAI>() is EnemyAI enemy
            ? enemy : GetPoweredLights(enable).FirstOrDefault(l => l?.gameObject == lightSource);

    public static bool CanBeAbsorbed(GrabbableObject grabbableObject, Vector3 position, float distance)
        => grabbableObject != null
            && grabbableObject.itemProperties.requiresBattery
            && grabbableObject.insertedBattery?.charge > 0f
            && (grabbableObject is not PatcherTool patcherTool || !patcherTool.isShocking)
            && IsObjectClose(grabbableObject, position, distance);

    public static bool IsObjectClose(GrabbableObject grabbableObject, Vector3 position, float distance)
    {
        if (Vector3.Distance(position, grabbableObject.transform.position) <= distance) return true;

        foreach (BeltBagItem beltBag in beltBags)
        {
            if (beltBag == null || Vector3.Distance(position, beltBag.transform.position) > distance) continue;
            if (beltBag.objectsInBag.FirstOrDefault(o => o == grabbableObject) != null) return true;
        }
        return false;
    }
}

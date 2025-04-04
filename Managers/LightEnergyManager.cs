using System.Linq;
using UnityEngine;

namespace LightEater.Managers;
public class LightEnergyManager
{
    public static GameObject GetLightSourceByName(string objectName, Vector3 position) => objectName switch
    {
        Constants.SHIP_LIGHTS => StartOfRound.Instance.shipRoomLights.gameObject,
        Constants.TURRET => LightEater.turrets
            .OrderBy(t => t != null ? Vector3.Distance(position, t.transform.position) : float.MaxValue)
            .FirstOrDefault()
            .gameObject,
        Constants.LANDMINE => LightEater.landmines
            .OrderBy(l => l != null ? Vector3.Distance(position, l.transform.position) : float.MaxValue)
            .FirstOrDefault()
            .gameObject,
        _ => RoundManager.Instance.allPoweredLightsAnimators
            .OrderBy(l => l != null ? Vector3.Distance(position, l.transform.position) : float.MaxValue)
            .FirstOrDefault()
            .gameObject,
    };

    public static object DetermineLightSource(GameObject lightSource)
        => lightSource.GetComponentInParent<ShipLights>() is ShipLights shipLights
            ? shipLights : lightSource.TryGetComponent(out GrabbableObject grabbableObject)
            ? grabbableObject : lightSource.GetComponentInParent<Turret>() is Turret turret
            ? turret : lightSource.GetComponentInParent<Landmine>() is Landmine landmine
            ? landmine : lightSource.GetComponentInParent<EnemyAI>() is EnemyAI enemy
            ? enemy : RoundManager.Instance.allPoweredLightsAnimators.FirstOrDefault(l => l?.gameObject == lightSource);

    public static bool CanBeAbsorbed(GrabbableObject grabbableObject, Vector3 position, float distance)
        => grabbableObject != null
            && grabbableObject.itemProperties.requiresBattery
            && grabbableObject.insertedBattery != null && grabbableObject.insertedBattery.charge > 0f
            && (grabbableObject is not PatcherTool patcherTool || !patcherTool.isShocking)
            && IsObjectClose(grabbableObject, position, distance);

    public static bool IsObjectClose(GrabbableObject grabbableObject, Vector3 position, float distance)
    {
        if (Vector3.Distance(position, grabbableObject.transform.position) <= distance) return true;

        foreach (BeltBagItem beltBag in LightEater.beltBags)
        {
            if (beltBag == null || Vector3.Distance(position, beltBag.transform.position) > distance) continue;
            if (beltBag.objectsInBag.FirstOrDefault(o => o == grabbableObject) != null) return true;
        }
        return false;
    }
}

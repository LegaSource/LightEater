using HarmonyLib;
using UnityEngine;

namespace LightEater.Patches
{
    internal class RoundManagerPatch
    {
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.LoadNewLevel))]
        [HarmonyPostfix]
        private static void StartGame()
        {
            LightEater.radMechAIs.Clear();
            foreach (RadMechAI radMech in Object.FindObjectsOfType<RadMechAI>())
                AddRadMech(radMech);

            LightEater.grabbableObjects.Clear();
            foreach (GrabbableObject grabbableObject in Object.FindObjectsOfType<GrabbableObject>())
                AddGrabbableObject(grabbableObject);
        }

        public static void AddRadMech(RadMechAI radMech)
        {
            if (!LightEater.radMechAIs.Contains(radMech))
                LightEater.radMechAIs.Add(radMech);
        }

        public static void AddGrabbableObject(GrabbableObject grabbableObject)
        {
            if (!grabbableObject.itemProperties.requiresBattery) return;

            if (!LightEater.grabbableObjects.Contains(grabbableObject))
                LightEater.grabbableObjects.Add(grabbableObject);
        }
    }
}

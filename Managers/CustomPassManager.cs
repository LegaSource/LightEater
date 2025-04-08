using LightEater.Behaviours;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace LightEater.Managers;
public class CustomPassManager : MonoBehaviour
{
    public static OverchargeCustomPass overchargePass;
    public static CustomPassVolume customPassVolume;
    public static Dictionary<LightEaterAI, List<Renderer>> overchargeEnemies = [];

    public static CustomPassVolume CustomPassVolume
    {
        get
        {
            if (customPassVolume == null)
            {
                customPassVolume = GameNetworkManager.Instance.localPlayerController.gameplayCamera.gameObject.AddComponent<CustomPassVolume>();
                if (customPassVolume != null)
                {
                    customPassVolume.targetCamera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
                    customPassVolume.injectionPoint = (CustomPassInjectionPoint)1;
                    customPassVolume.isGlobal = true;

                    overchargePass = new OverchargeCustomPass();
                    customPassVolume.customPasses.Add(overchargePass);
                }
            }
            return customPassVolume;
        }
    }

    public static void SetupCustomPassForEnemy(LightEaterAI lightEater)
    {
        if (overchargeEnemies.ContainsKey(lightEater)) return;

        LayerMask frozenLayer = 524288;
        List<Renderer> enemyRenderers = lightEater.GetComponentsInChildren<Renderer>().Where(r => (frozenLayer & (1 << r.gameObject.layer)) != 0).ToList();

        if (CustomPassVolume == null)
        {
            LightEater.mls.LogError("CustomPassVolume is not assigned.");
            return;
        }

        overchargePass = CustomPassVolume.customPasses.Find(pass => pass is OverchargeCustomPass) as OverchargeCustomPass;
        if (overchargePass == null)
        {
            LightEater.mls.LogError("FrozenCustomPass could not be found in CustomPassVolume.");
            return;
        }

        overchargeEnemies[lightEater] = enemyRenderers;
        overchargePass.AddTargetRenderers(enemyRenderers.ToArray(), LightEater.overchargeShader);
    }

    public static void RemoveAura(LightEaterAI lightEater)
    {
        if (!overchargeEnemies.ContainsKey(lightEater)) return;

        overchargePass.RemoveTargetRenderers(overchargeEnemies[lightEater]);
        _ = overchargeEnemies.Remove(lightEater);
    }
}
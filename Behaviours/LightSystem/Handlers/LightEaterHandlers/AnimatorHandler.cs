﻿using LightEater.Managers;
using UnityEngine;

namespace LightEater.Behaviours.LightSystem.Handlers.LightEaterHandlers;

public class AnimatorHandler(LightEaterAI lightEater, Animator animator) : Handlers.AnimatorHandler(animator)
{
    private readonly LightEaterAI lightEater = lightEater;

    public override void HandleLightDepletion()
    {
        base.HandleLightDepletion();

        lightEater.energyNetwork.currentCharge += ConfigManager.lightCharge.Value;
        LightEnergyManager.SetPoweredLightValue(lightEater.energyNetwork.closestLightSource, false);
    }

    public override void HandleLightRestoration()
    {
        base.HandleLightRestoration();

        lightEater.energyNetwork.currentCharge -= ConfigManager.lightCharge.Value;
        LightEnergyManager.SetPoweredLightValue(lightEater.energyNetwork.closestLightSource, true);
    }

    public override Vector3 GetClosestNodePosition()
        => lightEater.ChooseClosestNodeToPosition(lightEater.energyNetwork.closestLightSource.transform.position).position;

    public override Vector3 GetClosestLightPosition()
        => lightEater.energyNetwork.closestLightSource.transform.position;
}

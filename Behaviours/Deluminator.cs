using LegaFusionCore.Registries;
using LegaFusionCore.Utilities;
using LightEater.Managers;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LightEater.Behaviours;
public class Deluminator : PhysicsProp
{
    public LightEnergyNetworkManager energyNetwork;

    public AudioSource ActionAudio;
    public AudioClip ActionSound;

    public override void Start()
    {
        base.Start();
        energyNetwork.PlayActionSound = () =>
        {
            ActionAudio.PlayOneShot(ActionSound);
            ActionAudio.pitch = 1f;
        };
        energyNetwork.StopActionSound = ActionAudio.Stop;
        energyNetwork.ResetAction = () =>
        {
            if (LFCUtilities.ShouldBeLocalPlayer(playerHeldBy))
                SetControlTipsForItem();
        };
    }

    public override void EquipItem()
    {
        base.EquipItem();
        playerHeldBy.equippedUsableItemQE = true;
    }

    public override void PocketItem()
    {
        base.PocketItem();

        if (playerHeldBy != null)
        {
            playerHeldBy.activatingItem = false;
            playerHeldBy.equippedUsableItemQE = false;
        }
        energyNetwork.StopHandleLightEveryoneRpc();
    }

    public override void DiscardItem()
    {
        energyNetwork.StopHandleLightEveryoneRpc();
        base.DiscardItem();
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        if (buttonDown && playerHeldBy != null)
            StartAbsorption();
    }

    public override void ItemInteractLeftRight(bool right)
    {
        base.ItemInteractLeftRight(right);

        if (!right && playerHeldBy != null && energyNetwork.currentCharge >= 100)
        {
            energyNetwork.currentCharge = Mathf.Max(0, energyNetwork.currentCharge - 100);
            LightEnergyManager.StunExplosion(transform.position, applyStatus: true, playerHeldBy: playerHeldBy);
            StunExplosionEveryoneRpc(transform.position);
            SetControlTipsForItem();
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void StunExplosionEveryoneRpc(Vector3 position)
    {
        if (LFCUtilities.ShouldNotBeLocalPlayer(playerHeldBy))
        {
            LightEnergyManager.StunExplosion(position, applyStatus: true, playerHeldBy: playerHeldBy);
            energyNetwork.currentCharge = Mathf.Max(0, energyNetwork.currentCharge - 100);
        }
    }

    public void StartAbsorption()
    {
        if (!StartOfRound.Instance.shipHasLanded)
        {
            HUDManager.Instance.DisplayTip(Constants.IMPOSSIBLE_ACTION, Constants.MESSAGE_INFO_WAIT_SHIP);
            return;
        }
        if (energyNetwork.handleLightCoroutine != null)
        {
            energyNetwork.StopHandleLightEveryoneRpc();
            return;
        }
        if (energyNetwork.currentCharge >= 200)
        {
            HUDManager.Instance.DisplayTip(Constants.IMPOSSIBLE_ACTION, Constants.MESSAGE_INFO_MAX_CHARGES);
            return;
        }
        energyNetwork.closestLightSource = GetClosestLightSourceInView();
        if (energyNetwork.closestLightSource == null)
        {
            HUDManager.Instance.DisplayTip(Constants.IMPOSSIBLE_ACTION, Constants.MESSAGE_INFO_NO_LIGHT);
            return;
        }
        energyNetwork.HandleLight(energyNetwork.closestLightSource);
    }

    public GameObject GetClosestLightSourceInView()
    {
        foreach (RaycastHit hit in Physics.SphereCastAll(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), 2f, 5f))
        {
            GameObject lightSource = LFCSpawnRegistry.GetAllAs<EnemyAI>()?
                .FirstOrDefault(e => !e.isEnemyDead && ConfigManager.enemiesValues.Select(v => v.EnemyName).Contains(e.enemyType.enemyName) && e == hit.collider.GetComponent<EnemyAI>())?
                .gameObject
                ?? LFCSpawnRegistry.GetAllAs<GrabbableObject>()?
                    .FirstOrDefault(g => CanBeAbsorbed(g) && g == hit.collider.GetComponent<GrabbableObject>())?
                    .gameObject
                ?? LFCSpawnRegistry.GetSetExact<Turret>()?
                    .Cast<Turret>()?
                    .FirstOrDefault(t => t != null && t.turretActive && t == hit.collider.GetComponent<Turret>())?
                    .gameObject
                ?? LFCSpawnRegistry.GetSetExact<Landmine>()?
                    .Cast<Landmine>()?
                    .FirstOrDefault(l => l != null && l.mineActivated && l == hit.collider.GetComponent<Landmine>())?
                    .gameObject
                ?? RoundManager.Instance.allPoweredLightsAnimators?
                    .FirstOrDefault(p => p != null && !LFCPoweredLightsRegistry.IsLocked(p) && p == hit.collider.GetComponent<Animator>())?
                    .gameObject;

            if (lightSource != null)
                return lightSource;
        }
        return null;
    }

    public bool CanBeAbsorbed(GrabbableObject grabbableObject)
        => grabbableObject != null
            && grabbableObject.itemProperties.requiresBattery
            && grabbableObject.insertedBattery != null
            && grabbableObject.insertedBattery.charge > 0f;

    public override void SetControlTipsForItem()
    {
        itemProperties.toolTips[2] = $"[Charges : {energyNetwork.currentCharge}]";
        HUDManager.Instance.ChangeControlTipMultiple(itemProperties.toolTips, holdingItem: true, itemProperties);
    }
}

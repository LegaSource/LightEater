using LightEater.Managers;
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
        energyNetwork.PlayActionSound = PlayActionSound;
        energyNetwork.StopActionSound = StopActionSound;
        energyNetwork.ResetAction = ResetAction;
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

        energyNetwork.StopCoroutineServerRpc(true);
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        energyNetwork.StopCoroutineServerRpc(true);
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        if (!buttonDown || playerHeldBy == null) return;
        StartEnergyTransfer(true);
    }

    public override void ItemInteractLeftRight(bool right)
    {
        base.ItemInteractLeftRight(right);

        if (right || playerHeldBy == null) return;
        StartEnergyTransfer(false);
    }

    public void StartEnergyTransfer(bool enable)
    {
        if (!StartOfRound.Instance.shipHasLanded)
        {
            HUDManager.Instance.DisplayTip(Constants.IMPOSSIBLE_ACTION, Constants.MESSAGE_INFO_WAIT_SHIP);
            return;
        }

        if (energyNetwork.handleLightCoroutine != null)
        {
            energyNetwork.StopCoroutineServerRpc(true);
            return;
        }

        if (enable && energyNetwork.currentCharge >= 200)
        {
            HUDManager.Instance.DisplayTip(Constants.IMPOSSIBLE_ACTION, Constants.MESSAGE_INFO_MAX_CHARGES);
            return;
        }
        if (!enable && energyNetwork.currentCharge <= 0)
        {
            HUDManager.Instance.DisplayTip(Constants.IMPOSSIBLE_ACTION, Constants.MESSAGE_INFO_MIN_CHARGES);
            return;
        }

        energyNetwork.closestLightSource = LightEnergyManager.GetClosestLightSourceInView(this, enable);
        if (energyNetwork.closestLightSource == null)
        {
            HUDManager.Instance.DisplayTip(Constants.IMPOSSIBLE_ACTION, Constants.MESSAGE_INFO_NO_LIGHT);
            return;
        }

        energyNetwork.HandleLight(energyNetwork.closestLightSource,
            enable ? LightEnergyNetworkManager.LightActionType.Absorb : LightEnergyNetworkManager.LightActionType.Release);
    }

    public void PlayActionSound()
    {
        if (energyNetwork.totalDuration > 0f) ActionAudio.pitch = ActionSound.length / energyNetwork.totalDuration;
        ActionAudio.PlayOneShot(ActionSound);
        ActionAudio.pitch = 1f;
    }

    public void StopActionSound()
        => ActionAudio.Stop();

    public void ResetAction()
    {
        if (playerHeldBy == null || GameNetworkManager.Instance.localPlayerController != playerHeldBy) return;
        SetControlTipsForItem();
    }

    public override void SetControlTipsForItem()
    {
        itemProperties.toolTips[2] = $"[Charges : {energyNetwork.currentCharge}]";
        HUDManager.Instance.ChangeControlTipMultiple(itemProperties.toolTips, holdingItem: true, itemProperties);
    }
}

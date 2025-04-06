using LightEater.Managers;

namespace LightEater.Behaviours;
public class Deluminator : PhysicsProp
{
    public LightEnergyNetworkManager energyNetwork;

    public override void Start()
    {
        base.Start();
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

        _ = StopCoroutines();
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        if (!buttonDown || playerHeldBy == null) return;
        if (!StartEnergyTransfer(true)) return;

        energyNetwork.AbsorbLight(energyNetwork.closestLightSource, 10);
    }

    public override void ItemInteractLeftRight(bool right)
    {
        base.ItemInteractLeftRight(right);

        if (right || playerHeldBy == null) return;
        if (!StartEnergyTransfer(false)) return;

        energyNetwork.ReleaseLight(energyNetwork.closestLightSource, 5);
    }

    public bool StartEnergyTransfer(bool enable)
    {
        if (!StartOfRound.Instance.shipHasLanded)
        {
            HUDManager.Instance.DisplayTip(Constants.IMPOSSIBLE_ACTION, Constants.MESSAGE_INFO_WAIT_SHIP);
            return false;
        }

        if (StopCoroutines()) return false;

        energyNetwork.closestLightSource = LightEnergyManager.GetClosestLightSourceInView(this, enable);
        if (energyNetwork.closestLightSource == null)
        {
            HUDManager.Instance.DisplayTip(Constants.IMPOSSIBLE_ACTION, Constants.MESSAGE_INFO_NO_LIGHT);
            return false;
        }

        return true;
    }

    public bool StopCoroutines()
    {
        if (energyNetwork.absorbCoroutine != null)
        {
            energyNetwork.StopCoroutine(energyNetwork.absorbCoroutine);
            energyNetwork.absorbCoroutine = null;
            HUDManager.Instance.DisplayTip(Constants.INFORMATION, Constants.MESSAGE_INFO_ABSORPTION_CANCELED);
            return true;
        }
        if (energyNetwork.releaseCoroutine != null)
        {
            energyNetwork.StopCoroutine(energyNetwork.releaseCoroutine);
            energyNetwork.releaseCoroutine = null;
            HUDManager.Instance.DisplayTip(Constants.INFORMATION, Constants.MESSAGE_INFO_RELEASE_CANCELED);
            return true;
        }
        return false;
    }

    public void ResetAction()
        => SetControlTipsForItem();

    public override void SetControlTipsForItem()
    {
        itemProperties.toolTips[2] = $"[Charges : {energyNetwork.currentCharge}]";
        HUDManager.Instance.ChangeControlTipMultiple(itemProperties.toolTips, holdingItem: true, itemProperties);
    }
}

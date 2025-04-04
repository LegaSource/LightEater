using LightEater.Behaviours.LightSystem.Factories;
using LightEater.Managers;

namespace LightEater.Behaviours;
public class Deluminator : PhysicsProp
{
    public LightEnergyNetworkManager energyNetwork;

    public override void Start()
    {
        base.Start();
        energyNetwork.ResetAbsorption = ResetAbsorption;
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        if (!buttonDown || !StartOfRound.Instance.shipHasLanded || playerHeldBy == null) return;

        if (energyNetwork.absorbCoroutine != null)
        {
            StopCoroutine(energyNetwork.absorbCoroutine);
            energyNetwork.absorbCoroutine = null;
            return;
        }

        energyNetwork.closestLightSource = LightSourceFactory.GetClosestLightSourceInView(this);
        if (energyNetwork.closestLightSource == null)
        {
            HUDManager.Instance.DisplayTip(Constants.IMPOSSIBLE_ACTION, Constants.MESSAGE_INFO_NO_LIGHT);
            return;
        }

        energyNetwork.AbsorbLight(energyNetwork.closestLightSource, 10);
    }

    public void ResetAbsorption()
        => SetControlTipsForItem();

    public override void ItemInteractLeftRight(bool right)
    {
        base.ItemInteractLeftRight(right);

        if (right || !StartOfRound.Instance.shipHasLanded || playerHeldBy == null) return;

        energyNetwork.closestLightSource = LightSourceFactory.GetClosestLightSourceInView(this);
        if (energyNetwork.closestLightSource == null)
        {
            HUDManager.Instance.DisplayTip(Constants.IMPOSSIBLE_ACTION, Constants.MESSAGE_INFO_NO_LIGHT);
            return;
        }
    }

    public override void SetControlTipsForItem()
    {
        itemProperties.toolTips[2] = $"[Charges : {energyNetwork.currentCharge}]";
        HUDManager.Instance.ChangeControlTipMultiple(itemProperties.toolTips, holdingItem: true, itemProperties);
    }
}

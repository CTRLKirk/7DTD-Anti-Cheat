using AntiCheat.Core;
using HarmonyLib;

namespace AntiCheat.Detections
{
    [HarmonyPatch(typeof(NetPackagePlayerInventory), nameof(NetPackagePlayerInventory.ProcessPackage))]
    internal static class InventoryPatch
    {
        private static void Prefix(NetPackagePlayerInventory __instance)
        {
            var sender = __instance.Sender;
            if (sender == null || !ServerContext.IsServer || ServerContext.IsExempt(sender))
            {
                return;
            }

            ItemChecks.CheckStacks(sender.entityId, "toolbelt", InventoryReader.ReadToolbelt(__instance.inventoryData));
            ItemChecks.CheckStacks(sender.entityId, "backpack", InventoryReader.ReadBag(__instance.bagData));
            if (__instance.dragAndDropItem != null)
            {
                ItemChecks.CheckStack(sender.entityId, "cursor", __instance.dragAndDropItem);
            }
        }
    }

    [HarmonyPatch(typeof(NetPackageItemDrop), nameof(NetPackageItemDrop.ProcessPackage))]
    internal static class ItemDropPatch
    {
        private static void Prefix(NetPackageItemDrop __instance)
        {
            var sender = __instance.Sender;
            if (sender == null || !ServerContext.IsServer || ServerContext.IsExempt(sender))
            {
                return;
            }

            ItemChecks.CheckStack(sender.entityId, "dropped item", __instance.itemStack);
        }
    }
}

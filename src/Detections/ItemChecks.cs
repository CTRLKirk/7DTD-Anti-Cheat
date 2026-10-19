using System;
using System.Collections.Generic;
using AntiCheatExtension.Core;

namespace AntiCheatExtension.Detections
{
    internal static class ItemChecks
    {
        public static void CheckStacks(int entityId, string location, List<ItemStack> stacks)
        {
            if (stacks == null)
            {
                return;
            }

            foreach (var stack in stacks)
            {
                CheckStack(entityId, location, stack);
            }
        }

        public static void CheckStack(int entityId, string location, ItemStack stack)
        {
            if (stack == null || stack.IsEmpty())
            {
                return;
            }

            var itemClass = stack.itemValue?.ItemClass;
            if (itemClass == null)
            {
                return;
            }

            var settings = Settings.Current;
            var name = itemClass.GetItemName();

            var illegal = settings.Get(DetectionNames.IllegalItem);
            if (illegal.Enabled && !GameStats.GetBool(EnumGameStats.IsCreativeMenuEnabled) && IsIllegal(illegal, itemClass, name))
            {
                DetectionService.Report(entityId, DetectionNames.IllegalItem,
                    $"{stack.count}x {name} in {location} (CreativeMode {itemClass.CreativeMode})");
            }

            var stackSize = settings.Get(DetectionNames.StackSize);
            var max = itemClass.Stacknumber.Value;
            if (stackSize.Enabled && max > 0 && stack.count > max)
            {
                DetectionService.Report(entityId, DetectionNames.StackSize,
                    $"{stack.count}x {name} in {location} (max stack {max})");
            }
        }

        private static bool IsIllegal(DetectionSettings settings, ItemClass itemClass, string name)
        {
            if (settings.GetList("extraItems").Contains(name))
            {
                return true;
            }

            foreach (var mode in settings.GetList("creativeModes"))
            {
                if (Enum.TryParse(mode, true, out EnumCreativeMode parsed) && itemClass.CreativeMode == parsed)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

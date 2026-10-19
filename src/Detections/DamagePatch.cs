using AntiCheat.Core;
using HarmonyLib;
using UnityEngine;

namespace AntiCheat.Detections
{
    [HarmonyPatch(typeof(NetPackageDamageEntity), nameof(NetPackageDamageEntity.ProcessPackage))]
    internal static class DamagePatch
    {
        private static bool Prefix(NetPackageDamageEntity __instance, World _world)
        {
            var sender = __instance.Sender;
            if (sender == null || _world == null || !ServerContext.IsServer ||
                __instance.attackerEntityId != sender.entityId || ServerContext.IsExempt(sender))
            {
                return true;
            }

            var settings = Settings.Current;
            var damage = settings.Get(DetectionNames.Damage);
            if (damage.Enabled)
            {
                var limit = damage.GetFloat("maxDamage", 10000f) * settings.DamageMultiplier;
                if (__instance.strength > limit)
                {
                    DetectionService.Report(sender.entityId, DetectionNames.Damage,
                        $"hit for {__instance.strength} damage (limit {limit:F0}) with {ItemName(__instance.attackingItem)}");
                    return false;
                }
            }

            var reach = settings.Get(DetectionNames.Reach);
            if (reach.Enabled && IsMelee(__instance.attackingItem))
            {
                var attacker = _world.GetEntity(__instance.attackerEntityId);
                var target = _world.GetEntity(__instance.entityId);
                if (attacker != null && target != null)
                {
                    var distance = Vector3.Distance(attacker.position, target.position);
                    var limit = reach.GetFloat("maxDistance", 8f) * settings.ReachMultiplier;
                    if (distance > limit)
                    {
                        DetectionService.Report(sender.entityId, DetectionNames.Reach,
                            $"melee hit from {distance:F1}m (limit {limit:F1}) with {ItemName(__instance.attackingItem)}");
                    }
                }
            }

            return true;
        }

        private static bool IsMelee(ItemValue item)
        {
            var actions = item?.ItemClass?.Actions;
            if (actions == null || actions.Length == 0)
            {
                return false;
            }
            return actions[0] is ItemActionDynamicMelee || actions[0] is ItemActionMelee;
        }

        private static string ItemName(ItemValue item) => item?.ItemClass?.GetItemName() ?? "unknown item";
    }
}

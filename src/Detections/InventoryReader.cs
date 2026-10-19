using System;
using System.Collections.Generic;
using System.IO;
using AntiCheatExtension.Core;

namespace AntiCheatExtension.Detections
{
    /// <summary>
    /// Reads item stacks out of the inventory blobs a client sends, without touching the live player.
    /// Mirrors Inventory.ReadInto, Bag.readInto and ItemStackGrid.ReadInto.
    /// </summary>
    internal static class InventoryReader
    {
        private static bool loggedFailure;

        public static List<ItemStack> ReadToolbelt(MemoryStream blob)
        {
            return Read(blob, reader =>
            {
                reader.ReadByte();
                return ReadGridItems(reader);
            });
        }

        public static List<ItemStack> ReadBag(MemoryStream blob)
        {
            return Read(blob, reader =>
            {
                var version = reader.ReadByte();
                if (version >= 2)
                {
                    return ReadGridItems(reader);
                }

                int count = reader.ReadUInt16();
                return ReadStacks(reader, count);
            });
        }

        private static List<ItemStack> ReadGridItems(PooledBinaryReader reader)
        {
            reader.ReadUInt16();
            StreamUtils.ReadVector2i(reader);
            int count = reader.ReadInt16();
            return ReadStacks(reader, count);
        }

        private static List<ItemStack> ReadStacks(PooledBinaryReader reader, int count)
        {
            var stacks = new List<ItemStack>(Math.Max(0, count));
            for (var i = 0; i < count; i++)
            {
                var stack = ItemStack.Empty.Clone();
                stack.Read(reader);
                stacks.Add(stack);
            }
            return stacks;
        }

        private static List<ItemStack> Read(MemoryStream blob, Func<PooledBinaryReader, List<ItemStack>> read)
        {
            List<ItemStack> result = null;
            if (blob == null || blob.Length == 0)
            {
                return result;
            }

            try
            {
                StreamUtils.FromBlob(blob, reader => result = read(reader));
            }
            catch (Exception e)
            {
                if (!loggedFailure)
                {
                    loggedFailure = true;
                    ModLog.Warning($"Could not read a player inventory update, item checks may be incomplete: {e.Message}");
                }
            }
            return result;
        }
    }
}

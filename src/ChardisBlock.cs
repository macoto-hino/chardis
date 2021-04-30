using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Chardis
{
    public class ChardisBlock : Block
    {
        public const string Name = "ChardisBlock";

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var blockDrops = base.GetDrops(world, pos, byPlayer);

            if (world.BlockAccessor.GetBlockEntity(pos) is ChardisBlockEntity chardisBlockEntity)
            {
                var chardisDrops = chardisBlockEntity.GetDrops();
                if (blockDrops != null && chardisDrops != null)
                {
                    var len = blockDrops.Length;
                    Array.Resize<ItemStack>(ref blockDrops, len + chardisDrops.Length);
                    Array.Copy(chardisDrops, 0, blockDrops, len, chardisDrops.Length);
                }
                else if (chardisDrops != null)
                {
                    return chardisDrops;
                }
            }

            return blockDrops;
        }
    }
}
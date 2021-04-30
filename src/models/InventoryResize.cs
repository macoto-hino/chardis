using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Chardis.Models
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class InventoryResize
    {
        public int NumSlots;
        public int NumInstalledUpgrades;
        public BlockPos ChardisBlockPos;
    }
}
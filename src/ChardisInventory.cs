using System;
using Vintagestory.API.Common;

namespace Chardis
{
    /**
     * The dynamic ChARDIS inventory. We treat num slots and num installed chardis' as separate so that if config is
     * changed (i.e., to reduce the number of slots per chardis), items won't be voided.
     */
    public class ChardisInventory : InventoryGeneric
    {
        public ChardisInventory(string className, string instanceId, ICoreAPI api, int numSlots) : base(numSlots, className + "-" + instanceId, api)
        {
        }

        public void InitSlots(int numSlots)
        {
            // no new slots.
            if (numSlots <= Count)
            {
                return;
            }

            var startIndex = Count;
            Array.Resize(ref slots, numSlots);

            for (var index = startIndex; index < numSlots; index += 1)
            {
                slots[index] = NewSlot(index);
                slots[index].MarkDirty();
            }
        }
    }
}
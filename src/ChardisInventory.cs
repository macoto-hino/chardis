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

        public bool InitSlots(int count)
        {
            var currentSlots = slots;

            // same length, return fast.
            if (currentSlots?.Length == count)
            {
                return true;
            }

            // slots reduced, make sure all slots that are about to be removed are empty.
            if (currentSlots?.Length > count)
            {
                var startEmptyCheckI = 0;
                for (var i = slots.Length; i < currentSlots?.Length; i += 1)
                {
                    if (slots[i].Empty)
                    {
                        continue;
                    }

                    // move itemstack into first empty slot.
                    for (; startEmptyCheckI < count; startEmptyCheckI += 1)
                    {
                        if (!slots[startEmptyCheckI].Empty)
                        {
                            continue;
                        }

                        slots[startEmptyCheckI].Itemstack = currentSlots[i].Itemstack;
                        slots[startEmptyCheckI].MarkDirty();
                        currentSlots[i].Itemstack = null;
                        currentSlots[i].MarkDirty();
                        break;
                    }

                    if (startEmptyCheckI == count)
                    {
                        return false;
                    }
                }
            }

            var newSlots = new ItemSlot[count];

            for (var i = 0; i < count; i += 1)
            {
                if (i < currentSlots?.Length && currentSlots[i] != null)
                {
                    newSlots[i] = currentSlots[i];

                }
                else
                {
                    newSlots[i] = NewSlot(i);
                }
            }

            slots = newSlots;

            return true;
        }
    }
}
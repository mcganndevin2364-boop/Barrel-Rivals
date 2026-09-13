using System.Collections.Generic;
using BarrelRacing.Data;

namespace BarrelRacing.Runtime.Race
{
    public sealed class TackLoadoutValidator
    {
        public static bool ValidateLoadout(IEnumerable<TackData> tackItems, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (tackItems == null) return true;

            var usedSlots = new HashSet<TackSlot>();
            foreach (var tack in tackItems)
            {
                if (tack == null) continue;
                if (usedSlots.Contains(tack.Slot))
                {
                    errorMessage = $"Duplicate tack equip slot: {tack.Slot}";
                    return false;
                }
                usedSlots.Add(tack.Slot);
            }
            return true;
        }
    }
}

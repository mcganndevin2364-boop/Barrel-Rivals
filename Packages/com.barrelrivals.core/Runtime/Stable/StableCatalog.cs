using System;
using System.Collections.Generic;

namespace BarrelRivals.Core.Stable
{
    // Local starter cosmetics only. This is not an account entitlement or competitive loadout.
    public enum StableSlot { Saddle, Pad, Reins }

    public sealed class StableGear
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public StableSlot Slot { get; }
        internal StableGear(string id, string name, string description, StableSlot slot)
        { Id=id; Name=name; Description=description; Slot=slot; }
    }

    public static class StableCatalog
    {
        public const string HorseId = "starter-bay";
        public const string HorseName = "Copper";
        public const string Revision = "starter-cosmetics-v1";
        private static readonly StableGear[] gear = {
            new StableGear("saddle-ranch", "Ranch leather", "Warm, oiled leather with a raised cantle and a western horn.", StableSlot.Saddle),
            new StableGear("saddle-midnight", "Midnight leather", "Dark leather, silver hardware and the same western silhouette.", StableSlot.Saddle),
            new StableGear("pad-desert", "Desert weave", "Sand and rust saddle blanket with woven stripe detailing.", StableSlot.Pad),
            new StableGear("pad-turquoise", "Turquoise weave", "Deep turquoise saddle blanket with warm ivory stripes.", StableSlot.Pad),
            new StableGear("reins-turquoise", "Turquoise braid", "Turquoise and flax braid. Visible from the rider's seat.", StableSlot.Reins),
            new StableGear("reins-crimson", "Crimson braid", "Crimson and flax braid. Visible from the rider's seat.", StableSlot.Reins)
        };
        public static IReadOnlyList<StableGear> Gear { get; } = Array.AsReadOnly(gear);
        public static StableGear Find(string id)
        { foreach(var item in gear) if(item.Id==id) return item; return null; }
        public static string Default(StableSlot slot)
        {
            switch(slot) {
                case StableSlot.Saddle: return "saddle-ranch";
                case StableSlot.Pad: return "pad-desert";
                case StableSlot.Reins: return "reins-turquoise";
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }
    }

    [Serializable]
    public sealed class StableProfile
    {
        // Require the envelope when deserializing; a missing version is not a valid save.
        public int version;
        public string catalog;
        public string horseId;
        public string saddleId;
        public string padId;
        public string reinsId;
        public static StableProfile Starter() => new StableProfile {
            version=1, catalog=StableCatalog.Revision, horseId=StableCatalog.HorseId,
            saddleId=StableCatalog.Default(StableSlot.Saddle), padId=StableCatalog.Default(StableSlot.Pad),
            reinsId=StableCatalog.Default(StableSlot.Reins)
        };
        public StableProfile Copy() => (StableProfile)MemberwiseClone();
        public string Equipped(StableSlot slot)
        {
            switch(slot) {
                case StableSlot.Saddle: return saddleId;
                case StableSlot.Pad: return padId;
                case StableSlot.Reins: return reinsId;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }
        public bool IsValid => version==1 && catalog==StableCatalog.Revision && horseId==StableCatalog.HorseId
            && IsSlot(saddleId,StableSlot.Saddle) && IsSlot(padId,StableSlot.Pad) && IsSlot(reinsId,StableSlot.Reins);
        private static bool IsSlot(string id,StableSlot slot) => StableCatalog.Find(id)?.Slot==slot;
        public bool TryEquip(StableSlot slot,string id)
        {
            if(!IsValid || !IsSlot(id,slot))return false;
            switch(slot) {
                case StableSlot.Saddle: saddleId=id;break;
                case StableSlot.Pad: padId=id;break;
                case StableSlot.Reins: reinsId=id;break;
                default:return false;
            }
            return true;
        }
    }
}

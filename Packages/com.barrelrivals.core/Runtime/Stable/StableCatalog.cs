using System;
using System.Collections.Generic;

namespace BarrelRivals.Core.Stable
{
    // Local starter cosmetics only. This is not an account entitlement or competitive loadout.
    // Persisted values: append slots; never reorder the original three.
    public enum StableSlot { Saddle=0, Pad=1, Reins=2, Headstall=3, Gloves=4 }

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
        public const string Revision = "starter-cosmetics-v2";
        public const string LegacyRevision = "starter-cosmetics-v1";
        private static readonly StableGear[] gear = {
            new StableGear("saddle-ranch", "Ranch leather", "Warm, oiled leather with a raised cantle and a western horn.", StableSlot.Saddle),
            new StableGear("saddle-midnight", "Midnight leather", "Dark leather, silver hardware and the same western silhouette.", StableSlot.Saddle),
            new StableGear("saddle-rodeo-gold", "Rodeo Gold", "Golden leather on the classic western saddle.", StableSlot.Saddle),
            new StableGear("saddle-turquoise-trail", "Turquoise Trail", "Turquoise leather accents with silver trim.", StableSlot.Saddle),
            new StableGear("saddle-ember", "Ember", "Deep red leather on the classic western saddle.", StableSlot.Saddle),
            new StableGear("saddle-champion", "Champion", "Chestnut leather on the classic western saddle.", StableSlot.Saddle),
            new StableGear("saddle-outlaw", "Outlaw", "Dark leather on the classic western saddle.", StableSlot.Saddle),
            new StableGear("saddle-sunset", "Sunset", "Warm amber leather on the classic western saddle.", StableSlot.Saddle),
            new StableGear("pad-desert", "Desert weave", "Sand and rust saddle blanket with woven stripe detailing.", StableSlot.Pad),
            new StableGear("pad-turquoise", "Turquoise weave", "Deep turquoise saddle blanket with warm ivory stripes.", StableSlot.Pad),
            new StableGear("reins-turquoise", "Turquoise braid", "Turquoise and flax braid. Visible from the rider's seat.", StableSlot.Reins),
            new StableGear("reins-crimson", "Crimson braid", "Crimson and flax braid. Visible from the rider's seat.", StableSlot.Reins),
            new StableGear("headstall-ranch", "Ranch headstall", "Warm leather straps with silver hardware.", StableSlot.Headstall),
            new StableGear("headstall-midnight", "Midnight headstall", "Black leather straps with silver hardware.", StableSlot.Headstall),
            new StableGear("gloves-classic", "Classic Tan", "Tan leather riding gloves.", StableSlot.Gloves),
            new StableGear("gloves-blackout", "Blackout", "Black leather riding gloves.", StableSlot.Gloves),
            new StableGear("gloves-whiskey", "Whiskey", "Rich brown leather riding gloves.", StableSlot.Gloves),
            new StableGear("gloves-rodeo-red", "Rodeo Red", "Deep red leather riding gloves.", StableSlot.Gloves),
            new StableGear("gloves-steelhide", "Steelhide", "Gray leather riding gloves.", StableSlot.Gloves),
            new StableGear("gloves-midnight", "Midnight", "Navy leather riding gloves.", StableSlot.Gloves)
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
                case StableSlot.Headstall: return "headstall-ranch";
                case StableSlot.Gloves: return "gloves-classic";
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }
    }

    [Serializable]
    public sealed class StableProfile
    {
        public const int CurrentVersion=2;
        // Require the envelope when deserializing; a missing version is not a valid save.
        public int version;
        public string catalog;
        public string horseId;
        public string saddleId;
        public string padId;
        public string reinsId;
        public string headstallId;
        public string glovesId;
        public static StableProfile Starter() => new StableProfile {
            version=CurrentVersion, catalog=StableCatalog.Revision, horseId=StableCatalog.HorseId,
            saddleId=StableCatalog.Default(StableSlot.Saddle), padId=StableCatalog.Default(StableSlot.Pad),
            reinsId=StableCatalog.Default(StableSlot.Reins), headstallId=StableCatalog.Default(StableSlot.Headstall),
            glovesId=StableCatalog.Default(StableSlot.Gloves)
        };
        public StableProfile Copy() => (StableProfile)MemberwiseClone();
        public string Equipped(StableSlot slot)
        {
            switch(slot) {
                case StableSlot.Saddle: return saddleId;
                case StableSlot.Pad: return padId;
                case StableSlot.Reins: return reinsId;
                case StableSlot.Headstall: return headstallId;
                case StableSlot.Gloves: return glovesId;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }
        public bool IsValid => version==CurrentVersion && catalog==StableCatalog.Revision && horseId==StableCatalog.HorseId
            && IsSlot(saddleId,StableSlot.Saddle) && IsSlot(padId,StableSlot.Pad) && IsSlot(reinsId,StableSlot.Reins)
            && IsSlot(headstallId,StableSlot.Headstall) && IsSlot(glovesId,StableSlot.Gloves);
        public static bool TryMigrateVersion1(StableProfile source,out StableProfile migrated)
        {
            migrated=null;
            // Only the six IDs that existed in v1 are valid in its schema. A v1 label
            // must not authorize new content, a different horse or future fields.
            if(source==null || source.version!=1 || source.catalog!=StableCatalog.LegacyRevision
                || source.horseId!=StableCatalog.HorseId
                || (source.saddleId!="saddle-ranch" && source.saddleId!="saddle-midnight")
                || (source.padId!="pad-desert" && source.padId!="pad-turquoise")
                || (source.reinsId!="reins-turquoise" && source.reinsId!="reins-crimson")
                || !string.IsNullOrEmpty(source.headstallId) || !string.IsNullOrEmpty(source.glovesId))return false;
            migrated=Starter();
            migrated.saddleId=source.saddleId; migrated.padId=source.padId; migrated.reinsId=source.reinsId;
            return true;
        }
        private static bool IsSlot(string id,StableSlot slot) => StableCatalog.Find(id)?.Slot==slot;
        public bool TryEquip(StableSlot slot,string id)
        {
            if(!IsValid || !IsSlot(id,slot))return false;
            switch(slot) {
                case StableSlot.Saddle: saddleId=id;break;
                case StableSlot.Pad: padId=id;break;
                case StableSlot.Reins: reinsId=id;break;
                case StableSlot.Headstall: headstallId=id;break;
                case StableSlot.Gloves: glovesId=id;break;
                default:return false;
            }
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using BarrelRacing.Data;

namespace BarrelRacing.Runtime.Race
{
    public sealed class TackDurabilityTracker
    {
        private readonly Dictionary<string, float> _durabilityMap = new Dictionary<string, float>();

        public void SetDurability(string itemId, float durability)
        {
            _durabilityMap[itemId] = Mathf.Clamp(durability, 0f, 100f);
        }

        public float GetDurability(string itemId)
        {
            return _durabilityMap.TryGetValue(itemId, out float d) ? d : 100f;
        }

        public void ConsumeDurability(string itemId, float loss)
        {
            float current = GetDurability(itemId);
            SetDurability(itemId, current - Mathf.Max(0f, loss));
        }
    }
}

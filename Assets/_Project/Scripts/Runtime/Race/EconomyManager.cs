using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class EconomyManager
    {
        public long Coins { get; private set; }
        public int Trophies { get; private set; }
        private readonly HashSet<string> _processedTransactions = new HashSet<string>();

        public event Action<long> OnCoinsChanged;
        public event Action<int> OnTrophiesChanged;

        public void Initialize(long initialCoins, int initialTrophies)
        {
            Coins = System.Math.Max(0, initialCoins);
            Trophies = Mathf.Max(0, initialTrophies);
        }

        public bool DebitEntryFee(long fee, string txId)
        {
            if (string.IsNullOrEmpty(txId) || _processedTransactions.Contains(txId)) return false;
            if (Coins < fee) return false;

            Coins -= fee;
            _processedTransactions.Add(txId);
            OnCoinsChanged?.Invoke(Coins);
            return true;
        }

        public void CreditPrize(long prize, string txId)
        {
            if (string.IsNullOrEmpty(txId) || _processedTransactions.Contains(txId)) return;
            Coins = checked(Coins + prize);
            _processedTransactions.Add(txId);
            OnCoinsChanged?.Invoke(Coins);
        }

        public void ModifyTrophies(int delta)
        {
            Trophies = Mathf.Max(0, Trophies + delta);
            OnTrophiesChanged?.Invoke(Trophies);
        }
    }
}

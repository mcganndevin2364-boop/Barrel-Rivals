using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class NetworkedRaceState : MonoBehaviour
    {
        public int NetworkRound { get; private set; }
        public bool IsNetworkAuthority { get; private set; } = true;

        public void SetRound(int round) => NetworkRound = round;
    }
}

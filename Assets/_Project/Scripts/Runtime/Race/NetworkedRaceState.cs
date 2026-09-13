using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    [System.Serializable]
    public struct QuantizedGhostKeyframe : INetworkStruct
    {
        public short PosX, PosY, PosZ, RotY;
        public byte Speed, DriftTier;
        public ushort TimeMs;

        public static QuantizedGhostKeyframe Encode(GhostPlaybackKeyframe k) => new QuantizedGhostKeyframe
        {
            PosX = (short)(k.Position.x * 100f), PosY = (short)(k.Position.y * 100f), PosZ = (short)(k.Position.z * 100f),
            RotY = (short)(k.Rotation.eulerAngles.y * 100f), Speed = (byte)(k.SpeedMps * 10f), DriftTier = (byte)k.Drift,
            TimeMs = (ushort)(k.Timestamp * 1000f)
        };

        public GhostPlaybackKeyframe Decode() => new GhostPlaybackKeyframe(TimeMs / 1000f, new Vector3(PosX / 100f, PosY / 100f, PosZ / 100f), Quaternion.Euler(0f, RotY / 100f, 0f), Speed / 10f, (DriftTier)DriftTier);
    }

    [DisallowMultipleComponent]
    public sealed class NetworkedRaceState : NetworkBehaviour
    {
        [Networked] public NetworkString<_32> MatchId { get; set; }
        [Networked] public int CurrentRunIndex { get; set; }
        [Networked] public int Player1TotalScore { get; set; }
        [Networked] public int Player2TotalScore { get; set; }

        public void BroadcastRunKeyframes(IReadOnlyList<GhostPlaybackKeyframe> keyframes) { }
    }
}

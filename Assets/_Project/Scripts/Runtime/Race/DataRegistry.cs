using System.Collections.Generic;
using UnityEngine;
using BarrelRacing.Data;

namespace BarrelRacing.Runtime.Race
{
    public sealed class DataRegistry : MonoBehaviour
    {
        [SerializeField] private List<HorseBreedData> _breeds = new List<HorseBreedData>();
        [SerializeField] private List<TrackData> _tracks = new List<TrackData>();

        public IReadOnlyList<HorseBreedData> Breeds => _breeds;
        public IReadOnlyList<TrackData> Tracks => _tracks;

        public HorseBreedData GetBreedById(string id) => _breeds.Find(b => b != null && b.BreedId == id);
        public TrackData GetTrackById(string id) => _tracks.Find(t => t != null && t.TrackId == id);
    }
}

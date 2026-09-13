using System;
using UnityEngine;
using BarrelRacing.Data;

namespace BarrelRacing.Runtime.Race
{
    [Serializable]
    public struct HorseStatBlock
    {
        public float Speed;
        public float Agility;
        public float Temperament;
        public float Stamina;
        public float Acceleration;

        public static HorseStatBlock FromBreed(HorseBreedData breed)
        {
            if (breed == null)
            {
                return new HorseStatBlock
                {
                    Speed = 50f,
                    Agility = 50f,
                    Temperament = 50f,
                    Stamina = 50f,
                    Acceleration = 50f
                };
            }

            return new HorseStatBlock
            {
                Speed = breed.BaseSpeed,
                Agility = breed.BaseAgility,
                Temperament = breed.BaseTemperament,
                Stamina = breed.BaseStamina,
                Acceleration = breed.BaseAcceleration
            };
        }

        public float GetStat(StatType type)
        {
            return type switch
            {
                StatType.Speed => Speed,
                StatType.Agility => Agility,
                StatType.Temperament => Temperament,
                StatType.Stamina => Stamina,
                StatType.Acceleration => Acceleration,
                _ => 50f
            };
        }

        public void ApplyModifier(StatType type, float amount)
        {
            switch (type)
            {
                case StatType.Speed: Speed = Mathf.Max(1f, Speed + amount); break;
                case StatType.Agility: Agility = Mathf.Max(1f, Agility + amount); break;
                case StatType.Temperament: Temperament = Mathf.Max(1f, Temperament + amount); break;
                case StatType.Stamina: Stamina = Mathf.Max(1f, Stamina + amount); break;
                case StatType.Acceleration: Acceleration = Mathf.Max(1f, Acceleration + amount); break;
            }
        }
    }
}

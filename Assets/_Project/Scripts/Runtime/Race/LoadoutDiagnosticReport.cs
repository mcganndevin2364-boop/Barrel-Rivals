using System.Text;
using BarrelRacing.Data;

namespace BarrelRacing.Runtime.Race
{
    public sealed class LoadoutDiagnosticReport
    {
        public static string GenerateReport(HorseStatBlock stats, HorseBreedData breed, RiderData rider)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== LOADOUT DIAGNOSTIC REPORT ===");
            sb.AppendLine($"Breed: {(breed != null ? breed.DisplayName : "Default")}");
            sb.AppendLine($"Rider: {(rider != null ? rider.DisplayName : "Default")}");
            sb.AppendLine($"Resolved Speed: {stats.Speed:F1}");
            sb.AppendLine($"Resolved Agility: {stats.Agility:F1}");
            sb.AppendLine($"Resolved Stamina: {stats.Stamina:F1}");
            sb.AppendLine($"Resolved Temperament: {stats.Temperament:F1}");
            sb.AppendLine($"Resolved Acceleration: {stats.Acceleration:F1}");
            return sb.ToString();
        }
    }
}

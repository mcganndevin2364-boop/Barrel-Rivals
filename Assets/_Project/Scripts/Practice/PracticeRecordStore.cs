using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using BarrelRivals.Core;
using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Device-local, bounded practice records. Corrupt/old files cannot stop play; no cloud or rewards.</summary>
    public sealed class PracticeRecordStore
    {
        public const int SchemaVersion = 1;
        public const int MaximumDocumentBytes = 8 * 1024 * 1024;
        public PracticeRecords Records { get; } = new PracticeRecords();
        public bool LastSaveSucceeded { get; private set; } = true;
        public bool DiscardedInvalidData { get; private set; }
        private readonly string _filePath;

        public PracticeRecordStore(string filePath = null)
        {
            _filePath = filePath ?? Path.Combine(Application.persistentDataPath, "practice-bests-v1.json");
            Load();
        }

        public PracticeRecordUpdate Record(PracticeRun run, PracticeReplay replay = null)
        {
            PracticeRecordUpdate update = Records.Record(run, replay);
            if (update.IsNewBest) LastSaveSucceeded = Save();
            return update;
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return;
                long size = new FileInfo(_filePath).Length;
                if (size <= 0 || size > MaximumDocumentBytes) { DiscardedInvalidData = true; return; }
                string json = File.ReadAllText(_filePath);
                if (json.Length > MaximumDocumentBytes) { DiscardedInvalidData = true; return; }
                var document = JsonUtility.FromJson<RecordDocument>(json);
                if (document == null || document.schemaVersion != SchemaVersion || document.rulesVersion != PracticeRun.RulesVersion ||
                    document.entries == null || document.entries.Length > PracticeRecords.MaximumCapacity)
                { DiscardedInvalidData = true; return; }
                var records = new List<PracticeRecord>();
                foreach (var entry in document.entries)
                {
                    if (entry == null || entry.seed < 0 || entry.seed > uint.MaxValue || entry.rulesVersion != PracticeRun.RulesVersion ||
                        entry.rawTimeMs <= 0 || entry.rawTimeMs > PracticeReplay.MaximumDurationMs || entry.knockCount < 0 || entry.knockCount > 1)
                    { DiscardedInvalidData = true; continue; }
                    PracticeReplay replay = null;
                    if (entry.events != null && entry.events.Length > 0)
                    {
                        if (entry.events.Length <= PracticeReplay.MaximumEvents)
                        {
                            var events = new List<PracticeInputEvent>(entry.events.Length);
                            bool valid = true;
                            foreach (var input in entry.events)
                            {
                                if (input == null) { valid = false; break; }
                                events.Add(new PracticeInputEvent((PracticeInputKind)input.kind, input.timeMs, new TracePoint(input.x, input.y)));
                            }
                            if (valid) PracticeReplay.TryCreate(entry.rulesVersion, (uint)entry.seed, events, entry.completedAtMs, out replay);
                        }
                        if (replay == null || replay.Result.RawTimeMs != entry.rawTimeMs || replay.Result.KnockCount != entry.knockCount)
                        { replay = null; DiscardedInvalidData = true; }
                    }
                    records.Add(new PracticeRecord(entry.rulesVersion, (uint)entry.seed, entry.rawTimeMs, entry.knockCount, replay));
                }
                Records.Restore(records);
            }
            catch (Exception error) when (ExpectedStorageError(error)) { DiscardedInvalidData = true; }
        }

        private bool Save()
        {
            string temporary = _filePath + ".tmp";
            try
            {
                var document = new RecordDocument
                {
                    schemaVersion = SchemaVersion, rulesVersion = PracticeRun.RulesVersion,
                    entries = new RecordEntry[Records.Entries.Count]
                };
                for (int i = 0; i < document.entries.Length; i++)
                {
                    var record = Records.Entries[i];
                    var entry = new RecordEntry
                    {
                        rulesVersion = record.RulesVersion, seed = record.Seed,
                        rawTimeMs = record.RawTimeMs, knockCount = record.KnockCount
                    };
                    if (record.Replay != null)
                    {
                        entry.completedAtMs = record.Replay.CompletedAtMs;
                        entry.events = new InputEntry[record.Replay.Events.Count];
                        for (int j = 0; j < entry.events.Length; j++)
                        {
                            var input = record.Replay.Events[j];
                            entry.events[j] = new InputEntry { kind = (int)input.Kind, timeMs = input.TimeMs, x = input.Point.X, y = input.Point.Y };
                        }
                    }
                    document.entries[i] = entry;
                }
                string json = JsonUtility.ToJson(document);
                if (Encoding.UTF8.GetByteCount(json) > MaximumDocumentBytes) return false;
                string directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(temporary, json, new UTF8Encoding(false));
                // Replacement leaves the last completed save intact if writing the temporary file fails.
                if (File.Exists(_filePath)) File.Replace(temporary, _filePath, null);
                else File.Move(temporary, _filePath);
                return true;
            }
            catch (Exception error) when (ExpectedStorageError(error)) { return false; }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (Exception error) when (ExpectedStorageError(error)) { }
            }
        }

        private static bool ExpectedStorageError(Exception error) => error is IOException || error is UnauthorizedAccessException ||
            error is ArgumentException || error is NotSupportedException || error is SecurityException;

        [Serializable] private sealed class RecordDocument
        {
            public int schemaVersion;
            public int rulesVersion;
            public RecordEntry[] entries;
        }
        [Serializable] private sealed class RecordEntry
        {
            public int rulesVersion;
            public long seed;
            public long rawTimeMs;
            public int knockCount;
            public long completedAtMs;
            public InputEntry[] events;
        }
        [Serializable] private sealed class InputEntry
        {
            public int kind;
            public long timeMs;
            public double x;
            public double y;
        }
    }
}

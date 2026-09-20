using System;
using System.IO;
using BarrelRivals.Core.Stable;
using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Device-only cosmetic preferences, independent of competitive replays and future cloud ownership.</summary>
    public sealed class StableProfileStore
    {
        public const string FileName="stable-cosmetics-v2.json";
        public const string LegacyFileName="stable-cosmetics-v1.json";
        private readonly string path;
        private bool protectUnrecognizedSave;
        private StableProfile current;
        public StableProfile Current => current.Copy();
        public string Notice { get; private set; }
        public bool IsSessionOnly { get; private set; }
        public StableProfileStore(string directory)
        {
            path=Path.Combine(directory,FileName);
            var primary=Read(path);
            var backup=Read(path+".bak");
            if(primary.Exists || backup.Exists) {
                current=primary.Profile ?? backup.Profile ?? StableProfile.Starter();
                protectUnrecognizedSave=primary.Protected || backup.Protected
                    || (primary.Profile==null && backup.Profile==null);
                if(protectUnrecognizedSave) {
                    IsSessionOnly=true;
                    Notice="An unrecognized save was preserved. Gear changes are session-only.";
                } else Notice=primary.Profile==null ? "Recovered your previous saved gear." : "Saved on this device.";
                return;
            }

            // Separate namespaces make migration one-way and leave every v1 byte,
            // including the last recovery copy, available to the previous client.
            string legacyPath=Path.Combine(directory,LegacyFileName);
            var legacy=Read(legacyPath,true);
            var legacyBackup=Read(legacyPath+".bak",true);
            current=legacy.Profile ?? legacyBackup.Profile ?? StableProfile.Starter();
            protectUnrecognizedSave=legacy.Protected || legacyBackup.Protected
                || ((legacy.Exists || legacyBackup.Exists) && legacy.Profile==null && legacyBackup.Profile==null);
            if(protectUnrecognizedSave) {
                IsSessionOnly=true;
                Notice="An unrecognized legacy save was preserved. Gear changes are session-only.";
                return;
            }
            if(legacy.Profile!=null || legacyBackup.Profile!=null) {
                if(Persist(current)) Notice="Saved gear migrated. Your original save and backup are preserved.";
                return;
            }
            Notice="Starter collection · saved on this device when equipped.";
        }

        private sealed class SaveRead
        {
            public bool Exists;
            public bool Protected;
            public StableProfile Profile;
        }
        private static SaveRead Read(string file,bool legacy=false)
        {
            var result=new SaveRead();
            try {
                if(!File.Exists(file))return result;
                result.Exists=true;
                if(new FileInfo(file).Length>16384) { result.Protected=true;return result; }
                var profile=JsonUtility.FromJson<StableProfile>(File.ReadAllText(file));
                if(profile==null)return result;
                int expectedVersion=legacy ? 1 : StableProfile.CurrentVersion;
                string expectedCatalog=legacy ? StableCatalog.LegacyRevision : StableCatalog.Revision;
                if((profile.version!=0 && profile.version!=expectedVersion)
                    || (!string.IsNullOrEmpty(profile.catalog) && profile.catalog!=expectedCatalog)) {
                    result.Protected=true;return result;
                }
                if(legacy) {
                    if(StableProfile.TryMigrateVersion1(profile,out var migrated))result.Profile=migrated;
                } else if(profile.IsValid)result.Profile=profile;
            }
            catch(Exception e) when(IsStorageError(e)) {
                // Invalid JSON is recoverable from a valid backup; an unreadable
                // file must not be replaced just because it could not be checked.
                result.Exists=true;
                result.Protected=!(e is ArgumentException);
            }
            return result;
        }
        private static bool IsStorageError(Exception e) => e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException;
        public bool Equip(StableSlot slot,string id)
        {
            var next=Current.Copy(); if(!next.TryEquip(slot,id))return false;
            current=next;
            if(protectUnrecognizedSave){IsSessionOnly=true;return true;}
            Persist(next);
            return true;
        }
        private bool Persist(StableProfile next)
        {
            string temp=path+".tmp";
            try {
                // Recheck after construction so another client cannot leave a
                // newer envelope that this session then silently overwrites.
                var primary=Read(path);var backup=Read(path+".bak");
                if(primary.Protected || backup.Protected) {
                    protectUnrecognizedSave=true;IsSessionOnly=true;
                    Notice="An unrecognized save was preserved. Gear changes are session-only.";
                    return false;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                // Flush the candidate before an atomic same-directory replace.
                using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
                using(var writer=new StreamWriter(stream)) { writer.Write(JsonUtility.ToJson(next));writer.Flush();stream.Flush(true); }
                if(File.Exists(path)) {
                    // Never replace a valid recovery copy with corrupt primary bytes.
                    if(primary.Profile!=null) {
                        PreserveCorruptFile(path+".bak",backup);
                        File.Replace(temp,path,path+".bak");
                    } else {
                        PreserveCorruptFile(path,primary);
                        File.Replace(temp,path,null);
                    }
                } else File.Move(temp,path);
                IsSessionOnly=false;Notice="Gear equipped · saved on this device.";
                return true;
            }
            catch(Exception e) when(IsStorageError(e)) {
                IsSessionOnly=true;Notice="Gear equipped for this session. Device storage is unavailable.";
                return false;
            }
            finally { try { if(File.Exists(temp))File.Delete(temp); } catch(Exception e) when(IsStorageError(e)) {} }
        }
        private static void PreserveCorruptFile(string file,SaveRead state)
        {
            if(state.Exists && state.Profile==null)
                File.Copy(file,file+".recovered-"+Guid.NewGuid().ToString("N"),false);
        }
    }

    internal static class StableSession
    {
        private static StableProfileStore store;
        internal static StableProfileStore Store => store ?? (store=new StableProfileStore(Application.persistentDataPath));
        internal static void UseForTests(StableProfileStore replacement) { store=replacement; }
    }
}

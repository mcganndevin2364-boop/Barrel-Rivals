using System;
using System.IO;
using BarrelRivals.Core.Stable;
using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Device-only cosmetic preferences, independent of competitive replays and future cloud ownership.</summary>
    public sealed class StableProfileStore
    {
        public const string FileName="stable-cosmetics-v1.json";
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
            current=primary ?? Read(path+".bak");
            if(current!=null) {
                protectUnrecognizedSave=HasNewerEnvelope(path);
                if(protectUnrecognizedSave) { IsSessionOnly=true;Notice="A newer save was preserved. Using recovered gear for this session.";return; }
                Notice=File.Exists(path) && primary==null ? "Recovered your previous saved gear." : "Saved on this device.";
                return;
            }
            protectUnrecognizedSave=File.Exists(path) || File.Exists(path+".bak");
            current=StableProfile.Starter(); IsSessionOnly=protectUnrecognizedSave;
            Notice=protectUnrecognizedSave ? "Save could not be read. Gear changes are session-only; the original is preserved." : "Starter collection · saved on this device when equipped.";
        }
        private static bool HasNewerEnvelope(string file)
        {
            try {
                if(!File.Exists(file) || new FileInfo(file).Length>16384)return false;
                var p=JsonUtility.FromJson<StableProfile>(File.ReadAllText(file));
                return p!=null && (p.version>1 || (p.version==1 && p.catalog!=StableCatalog.Revision));
            }
            catch(Exception e) when(IsStorageError(e)) { return false; }
        }
        private static StableProfile Read(string file)
        {
            try {
                if(!File.Exists(file) || new FileInfo(file).Length>16384)return null;
                var profile=JsonUtility.FromJson<StableProfile>(File.ReadAllText(file));
                return profile!=null && profile.IsValid ? profile : null;
            }
            catch(Exception e) when(IsStorageError(e)) { return null; }
        }
        private static bool IsStorageError(Exception e) => e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException;
        public bool Equip(StableSlot slot,string id)
        {
            var next=Current.Copy(); if(!next.TryEquip(slot,id))return false;
            current=next;
            if(protectUnrecognizedSave){IsSessionOnly=true;return true;}
            string temp=path+".tmp";
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                // Flush the candidate before an atomic same-directory replace.
                using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
                using(var writer=new StreamWriter(stream)) { writer.Write(JsonUtility.ToJson(next));writer.Flush();stream.Flush(true); }
                if(File.Exists(path)) {
                    // Never replace a valid recovery copy with corrupt primary bytes.
                    if(Read(path)!=null) File.Replace(temp,path,path+".bak");
                    else File.Replace(temp,path,null);
                } else File.Move(temp,path);
                IsSessionOnly=false;Notice="Gear equipped · saved on this device.";
            }
            catch(Exception e) when(IsStorageError(e)) {
                IsSessionOnly=true;Notice="Gear equipped for this session. Device storage is unavailable.";
            }
            finally { try { if(File.Exists(temp))File.Delete(temp); } catch(Exception e) when(IsStorageError(e)) {} }
            return true;
        }
    }

    internal static class StableSession
    {
        private static StableProfileStore store;
        internal static StableProfileStore Store => store ?? (store=new StableProfileStore(Application.persistentDataPath));
        internal static void UseForTests(StableProfileStore replacement) { store=replacement; }
    }
}

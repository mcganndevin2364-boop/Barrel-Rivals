using System;
using System.IO;
using System.Collections.Generic;
using BarrelRivals.Core.Stable;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;

namespace BarrelRivals.Tests
{
    public sealed class StableProfileTests
    {
        private string directory;
        [SetUp] public void SetUp(){directory=Path.Combine(Path.GetTempPath(),"BarrelStableTests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);}
        [TearDown] public void TearDown(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
        private string CurrentPath => Path.Combine(directory,StableProfileStore.FileName);
        private string LegacyPath => Path.Combine(directory,StableProfileStore.LegacyFileName);
        private static string LegacyJson(string saddle="saddle-midnight",string pad="pad-turquoise",string reins="reins-crimson")
            => "{\n  \"version\":1,\"catalog\":\"starter-cosmetics-v1\",\"horseId\":\"starter-bay\","
                + "\"saddleId\":\""+saddle+"\",\"padId\":\""+pad+"\",\"reinsId\":\""+reins+"\"\n}";
        [Test] public void EveryFreeCosmeticHasAUniqueIdAndCanEquipOnlyInItsOwnSlot()
        {
            Assert.AreEqual(0,(int)StableSlot.Saddle);Assert.AreEqual(1,(int)StableSlot.Pad);Assert.AreEqual(2,(int)StableSlot.Reins);
            Assert.AreEqual(3,(int)StableSlot.Headstall);Assert.AreEqual(4,(int)StableSlot.Gloves);
            var ids=new HashSet<string>();var counts=new int[5];
            foreach(var item in StableCatalog.Gear) {
                Assert.IsTrue(ids.Add(item.Id),item.Id);counts[(int)item.Slot]++;
                var profile=StableProfile.Starter();Assert.IsTrue(profile.TryEquip(item.Slot,item.Id));
                Assert.AreEqual(item.Id,profile.Equipped(item.Slot));Assert.IsTrue(profile.IsValid);
                for(int slot=0;slot<5;slot++)if(slot!=(int)item.Slot)Assert.IsFalse(profile.TryEquip((StableSlot)slot,item.Id));
            }
            Assert.AreEqual(20,ids.Count);CollectionAssert.AreEqual(new[]{8,2,2,2,6},counts);
        }
        [Test] public void WrongSlotUnknownAndNullGearCannotChangeLoadout()
        {
            var p=StableProfile.Starter();Assert.IsTrue(p.IsValid);
            foreach(string id in new[]{"pad-desert","paid-super-saddle",null,""})Assert.IsFalse(p.TryEquip(StableSlot.Saddle,id));
            Assert.AreEqual("saddle-ranch",p.saddleId);Assert.IsFalse(p.TryEquip((StableSlot)99,"saddle-ranch"));
            p.horseId="unowned-horse";Assert.IsFalse(p.TryEquip(StableSlot.Pad,"pad-turquoise"));
        }
        [Test] public void CatalogIdentityAndMissingVersionCannotBeSilentlyImported()
        {
            var p=StableProfile.Starter();p.catalog="future-catalog";Assert.IsFalse(p.IsValid);
            p=JsonUtility.FromJson<StableProfile>("{}");Assert.IsFalse(p.IsValid);
            p=StableProfile.Starter();p.padId="saddle-ranch";Assert.IsFalse(p.IsValid);
        }
        [Test] public void EquipmentRoundTripPreservesAllSlotsAndAtomicBackup()
        {
            var store=new StableProfileStore(directory);
            Assert.IsTrue(store.Equip(StableSlot.Saddle,"saddle-midnight"));
            Assert.IsTrue(store.Equip(StableSlot.Pad,"pad-turquoise"));
            Assert.IsTrue(store.Equip(StableSlot.Reins,"reins-crimson"));
            Assert.IsTrue(store.Equip(StableSlot.Headstall,"headstall-midnight"));
            Assert.IsTrue(store.Equip(StableSlot.Gloves,"gloves-rodeo-red"));
            var loaded=new StableProfileStore(directory);
            Assert.AreEqual("saddle-midnight",loaded.Current.saddleId);Assert.AreEqual("pad-turquoise",loaded.Current.padId);Assert.AreEqual("reins-crimson",loaded.Current.reinsId);
            Assert.AreEqual("headstall-midnight",loaded.Current.headstallId);Assert.AreEqual("gloves-rodeo-red",loaded.Current.glovesId);
            Assert.IsFalse(loaded.IsSessionOnly);Assert.IsTrue(File.Exists(Path.Combine(directory,StableProfileStore.FileName)+".bak"));
            Assert.IsFalse(File.Exists(Path.Combine(directory,StableProfileStore.FileName)+".tmp"));
        }
        [Test] public void CorruptPrimaryRecoversValidPreviousWriteWithoutLosingBackup()
        {
            var store=new StableProfileStore(directory);store.Equip(StableSlot.Saddle,"saddle-midnight");store.Equip(StableSlot.Pad,"pad-turquoise");
            string path=Path.Combine(directory,StableProfileStore.FileName);File.WriteAllText(path,"corrupt");
            var loaded=new StableProfileStore(directory);Assert.AreEqual("saddle-midnight",loaded.Current.saddleId);Assert.AreEqual("pad-desert",loaded.Current.padId);
            StringAssert.Contains("Recovered",loaded.Notice);loaded.Equip(StableSlot.Reins,"reins-crimson");
            var recovered=Directory.GetFiles(directory,StableProfileStore.FileName+".recovered-*");
            Assert.AreEqual(1,recovered.Length);Assert.AreEqual("corrupt",File.ReadAllText(recovered[0]));
            Assert.IsTrue(JsonUtility.FromJson<StableProfile>(File.ReadAllText(path+".bak")).IsValid);
            Assert.AreEqual("reins-crimson",new StableProfileStore(directory).Current.reinsId);
        }
        [TestCase("{\"version\":99}")]
        [TestCase("not-json")]
        [TestCase("{}")]
        public void UnrecognizedSaveIsPreservedAndDoesNotBlockSession(string bytes)
        {
            string path=Path.Combine(directory,StableProfileStore.FileName);File.WriteAllText(path,bytes);
            var store=new StableProfileStore(directory);Assert.IsTrue(store.Current.IsValid);Assert.IsTrue(store.IsSessionOnly);
            Assert.IsTrue(store.Equip(StableSlot.Pad,"pad-turquoise"));Assert.AreEqual(bytes,File.ReadAllText(path));
            Assert.AreEqual("pad-turquoise",store.Current.padId);
        }
        [Test] public void OversizedSaveRemainsUntouched()
        {
            string path=Path.Combine(directory,StableProfileStore.FileName);File.WriteAllText(path,new string('x',16385));
            var store=new StableProfileStore(directory);store.Equip(StableSlot.Reins,"reins-crimson");
            Assert.IsTrue(store.IsSessionOnly);Assert.AreEqual(16385,new FileInfo(path).Length);
        }
        [Test] public void UnavailableStorageKeepsWorkingSessionAndReportsFailure()
        {
            string file=Path.Combine(directory,"not-a-directory");File.WriteAllText(file,"preserve");
            var store=new StableProfileStore(file);Assert.IsTrue(store.Equip(StableSlot.Saddle,"saddle-midnight"));
            Assert.IsTrue(store.IsSessionOnly);StringAssert.Contains("session",store.Notice);Assert.AreEqual("preserve",File.ReadAllText(file));
        }
        [Test] public void NewerPrimaryIsPreservedEvenWhenOlderBackupCanBeRecovered()
        {
            var store=new StableProfileStore(directory);store.Equip(StableSlot.Saddle,"saddle-midnight");store.Equip(StableSlot.Pad,"pad-turquoise");
            string path=Path.Combine(directory,StableProfileStore.FileName);File.WriteAllText(path,"{\"version\":99}");
            var loaded=new StableProfileStore(directory);loaded.Equip(StableSlot.Reins,"reins-crimson");
            Assert.IsTrue(loaded.IsSessionOnly);Assert.AreEqual("{\"version\":99}",File.ReadAllText(path));
        }
        [Test] public void VersionOneMigrationPreservesSelectionsAndBothOriginalFilesByteForByte()
        {
            byte[] original=System.Text.Encoding.UTF8.GetBytes(LegacyJson());
            byte[] backup=System.Text.Encoding.UTF8.GetBytes(LegacyJson("saddle-ranch","pad-desert","reins-turquoise"));
            File.WriteAllBytes(LegacyPath,original);File.WriteAllBytes(LegacyPath+".bak",backup);
            var store=new StableProfileStore(directory);Assert.IsFalse(store.IsSessionOnly);
            Assert.AreEqual(StableProfile.CurrentVersion,store.Current.version);Assert.AreEqual(StableCatalog.Revision,store.Current.catalog);
            Assert.AreEqual("saddle-midnight",store.Current.saddleId);Assert.AreEqual("pad-turquoise",store.Current.padId);Assert.AreEqual("reins-crimson",store.Current.reinsId);
            Assert.AreEqual("headstall-ranch",store.Current.headstallId);Assert.AreEqual("gloves-classic",store.Current.glovesId);
            Assert.IsTrue(File.Exists(CurrentPath));StringAssert.Contains("migrated",store.Notice);
            store.Equip(StableSlot.Gloves,"gloves-steelhide");store.Equip(StableSlot.Saddle,"saddle-ember");
            CollectionAssert.AreEqual(original,File.ReadAllBytes(LegacyPath));CollectionAssert.AreEqual(backup,File.ReadAllBytes(LegacyPath+".bak"));
            var restarted=new StableProfileStore(directory);Assert.AreEqual("gloves-steelhide",restarted.Current.glovesId);Assert.AreEqual("saddle-ember",restarted.Current.saddleId);
        }
        [TestCase(false)] [TestCase(true)]
        public void LegacyBackupCanMigrateWithoutChangingMissingOrCorruptPrimary(bool corruptPrimary)
        {
            if(corruptPrimary)File.WriteAllText(LegacyPath,"old corrupt bytes");
            string backup=LegacyJson();File.WriteAllText(LegacyPath+".bak",backup);
            var store=new StableProfileStore(directory);Assert.IsFalse(store.IsSessionOnly);
            Assert.AreEqual("saddle-midnight",store.Current.saddleId);Assert.IsTrue(File.Exists(CurrentPath));
            if(corruptPrimary)Assert.AreEqual("old corrupt bytes",File.ReadAllText(LegacyPath));else Assert.IsFalse(File.Exists(LegacyPath));
            Assert.AreEqual(backup,File.ReadAllText(LegacyPath+".bak"));
        }
        [TestCase("{\"version\":99}")]
        [TestCase("{\"version\":1,\"catalog\":\"future-catalog\"}")]
        public void UnknownLegacyEnvelopeKeepsRecoveredChoicesSessionOnly(string unknown)
        {
            File.WriteAllText(LegacyPath,unknown);string backup=LegacyJson();File.WriteAllText(LegacyPath+".bak",backup);
            var store=new StableProfileStore(directory);Assert.IsTrue(store.IsSessionOnly);Assert.AreEqual("saddle-midnight",store.Current.saddleId);
            Assert.IsTrue(store.Equip(StableSlot.Gloves,"gloves-whiskey"));Assert.IsFalse(File.Exists(CurrentPath));
            Assert.AreEqual(unknown,File.ReadAllText(LegacyPath));Assert.AreEqual(backup,File.ReadAllText(LegacyPath+".bak"));
        }
        [Test] public void VersionOneCannotSmuggleNewSlotsNewItemsOrInvalidHorseThroughMigration()
        {
            var p=JsonUtility.FromJson<StableProfile>(LegacyJson());Assert.IsTrue(StableProfile.TryMigrateVersion1(p,out var migrated));
            Assert.AreEqual(1,p.version);Assert.IsNull(p.glovesId);Assert.AreEqual("gloves-classic",migrated.glovesId);
            foreach(string id in new[]{"saddle-ember","pad-desert",null}) {
                var invalid=JsonUtility.FromJson<StableProfile>(LegacyJson());invalid.saddleId=id;
                Assert.IsFalse(StableProfile.TryMigrateVersion1(invalid,out _));
            }
            p.glovesId="gloves-whiskey";Assert.IsFalse(StableProfile.TryMigrateVersion1(p,out _));p.glovesId=null;
            p.headstallId="headstall-ranch";Assert.IsFalse(StableProfile.TryMigrateVersion1(p,out _));p.headstallId=null;
            p.horseId="different-horse";Assert.IsFalse(StableProfile.TryMigrateVersion1(p,out _));
            Assert.IsFalse(StableProfile.TryMigrateVersion1(null,out _));
        }
        [Test] public void ValidVersionTwoSaveTakesPrecedenceAndNeverReimportsLegacyChoices()
        {
            File.WriteAllText(LegacyPath,LegacyJson());var store=new StableProfileStore(directory);
            store.Equip(StableSlot.Saddle,"saddle-champion");store.Equip(StableSlot.Gloves,"gloves-midnight");
            File.WriteAllText(LegacyPath,LegacyJson("saddle-ranch"));
            var loaded=new StableProfileStore(directory);Assert.AreEqual("saddle-champion",loaded.Current.saddleId);Assert.AreEqual("gloves-midnight",loaded.Current.glovesId);
        }
        [Test] public void CorruptVersionTwoCannotBeOverwrittenByAMigrationFromLegacy()
        {
            string legacy=LegacyJson();File.WriteAllText(LegacyPath,legacy);File.WriteAllText(CurrentPath,"broken v2 bytes");
            var store=new StableProfileStore(directory);Assert.IsTrue(store.IsSessionOnly);store.Equip(StableSlot.Gloves,"gloves-blackout");
            Assert.AreEqual("broken v2 bytes",File.ReadAllText(CurrentPath));Assert.AreEqual(legacy,File.ReadAllText(LegacyPath));
        }
        [Test] public void UnknownBackupCannotBeOverwrittenByAValidPrimary()
        {
            var store=new StableProfileStore(directory);store.Equip(StableSlot.Saddle,"saddle-midnight");
            string original=File.ReadAllText(CurrentPath);File.WriteAllText(CurrentPath+".bak","{\"version\":99}");
            var loaded=new StableProfileStore(directory);loaded.Equip(StableSlot.Gloves,"gloves-whiskey");
            Assert.IsTrue(loaded.IsSessionOnly);Assert.AreEqual("saddle-midnight",loaded.Current.saddleId);
            Assert.AreEqual(original,File.ReadAllText(CurrentPath));Assert.AreEqual("{\"version\":99}",File.ReadAllText(CurrentPath+".bak"));
        }
        [Test] public void NewerSaveWrittenDuringSessionIsPreservedBeforeEquip()
        {
            var store=new StableProfileStore(directory);File.WriteAllText(CurrentPath,"{\"version\":99}");
            Assert.IsTrue(store.Equip(StableSlot.Headstall,"headstall-midnight"));Assert.IsTrue(store.IsSessionOnly);
            Assert.AreEqual("headstall-midnight",store.Current.headstallId);Assert.AreEqual("{\"version\":99}",File.ReadAllText(CurrentPath));
        }
        [Test] public void MigrationStorageFailureKeepsRecoveredGearAndOriginalBytes()
        {
            string legacy=LegacyJson();File.WriteAllText(LegacyPath,legacy);Directory.CreateDirectory(CurrentPath);
            var store=new StableProfileStore(directory);Assert.IsTrue(store.IsSessionOnly);Assert.AreEqual("saddle-midnight",store.Current.saddleId);
            Assert.AreEqual(legacy,File.ReadAllText(LegacyPath));Assert.IsTrue(Directory.Exists(CurrentPath));
            Assert.IsFalse(File.Exists(CurrentPath+".tmp"));
        }
        [Test] public void CallerCannotMutateStoreThroughItsSnapshot()
        {
            var store=new StableProfileStore(directory);var snapshot=store.Current;snapshot.horseId="unowned";
            Assert.IsTrue(store.Current.IsValid);
        }
        [Test] public void CosmeticSaveNeverTouchesV1ReplayBytes()
        {
            string path=Path.Combine(directory,"reins-lab-v1-0.json");File.WriteAllText(path,"historical race bytes");
            var store=new StableProfileStore(directory);store.Equip(StableSlot.Saddle,"saddle-midnight");
            Assert.AreEqual("historical race bytes",File.ReadAllText(path));
        }
    }
}

using System;
using System.IO;
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
            var loaded=new StableProfileStore(directory);
            Assert.AreEqual("saddle-midnight",loaded.Current.saddleId);Assert.AreEqual("pad-turquoise",loaded.Current.padId);Assert.AreEqual("reins-crimson",loaded.Current.reinsId);
            Assert.IsFalse(loaded.IsSessionOnly);Assert.IsTrue(File.Exists(Path.Combine(directory,StableProfileStore.FileName)+".bak"));
            Assert.IsFalse(File.Exists(Path.Combine(directory,StableProfileStore.FileName)+".tmp"));
        }
        [Test] public void CorruptPrimaryRecoversValidPreviousWriteWithoutLosingBackup()
        {
            var store=new StableProfileStore(directory);store.Equip(StableSlot.Saddle,"saddle-midnight");store.Equip(StableSlot.Pad,"pad-turquoise");
            string path=Path.Combine(directory,StableProfileStore.FileName);File.WriteAllText(path,"corrupt");
            var loaded=new StableProfileStore(directory);Assert.AreEqual("saddle-midnight",loaded.Current.saddleId);Assert.AreEqual("pad-desert",loaded.Current.padId);
            StringAssert.Contains("Recovered",loaded.Notice);loaded.Equip(StableSlot.Reins,"reins-crimson");
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
            string path=Path.Combine(directory,StableProfileStore.FileName);File.WriteAllText(path,"{\"version\":2}");
            var loaded=new StableProfileStore(directory);loaded.Equip(StableSlot.Reins,"reins-crimson");
            Assert.IsTrue(loaded.IsSessionOnly);Assert.AreEqual("{\"version\":2}",File.ReadAllText(path));
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

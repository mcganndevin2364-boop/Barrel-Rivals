using System;
using System.IO;
using BarrelRivals.Core.Reins;
using NUnit.Framework;
using UnityEngine;

namespace BarrelRivals.Tests
{
    internal static class V2FixtureExpected
    {
        public static CompletedResult Result
        {
            get
            {
                var value=JsonUtility.FromJson<Envelope>(File.ReadAllText(Path.Combine(Application.dataPath,"../Contracts/Reins/complete-response.v2.json")));
                Assert.AreEqual(2,value.contractVersion);Assert.AreEqual(ReinsRun.RulesVersion,value.rulesVersion);
                Assert.AreEqual(ReinsRuleFingerprint.Sha256,value.ruleFingerprint);Assert.IsTrue(value.accepted);
                Assert.IsNotNull(value.result);Assert.AreEqual("Perfect",value.result.launchOutcome);
                Assert.AreEqual(0,value.result.launchReleaseErrorMs);
                return value.result;
            }
        }
        [Serializable] private sealed class Envelope {public int contractVersion,rulesVersion;public string ruleFingerprint;public bool accepted;public CompletedResult result;}
        [Serializable] internal sealed class CompletedResult {public long raceTimeMs,finalTimeMs;public int knockCount,stylePoints,launchReleaseErrorMs;public string launchOutcome;}
    }
}

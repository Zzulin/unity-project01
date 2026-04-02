using System;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnitySkills.Tests.Core
{
    [TestFixture]
    public class BatchExecutorTests
    {
        // ──────────────────────────────────────────────
        //  Test item type
        // ──────────────────────────────────────────────

        private class TestItem
        {
            public string Name;
            public int Value;
        }

        // ──────────────────────────────────────────────
        //  Helpers: extract fields from anonymous result
        // ──────────────────────────────────────────────

        /// <summary>
        /// Convert anonymous object to JObject for easy field access.
        /// </summary>
        private static JObject ToJObject(object result)
        {
            var json = JsonConvert.SerializeObject(result);
            return JObject.Parse(json);
        }

        private static string GetError(object result)
        {
            if (result == null) return null;
            var j = ToJObject(result);
            return j["error"]?.ToString();
        }

        // ══════════════════════════════════════════════
        //  Invalid input tests
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_NullItemsJson_ReturnsError()
        {
            var result = BatchExecutor.Execute<TestItem>(null, item => new { ok = true });

            Assert.IsNotNull(GetError(result));
            StringAssert.Contains("required", GetError(result));
        }

        [Test]
        public void Execute_EmptyItemsJson_ReturnsError()
        {
            var result = BatchExecutor.Execute<TestItem>("", item => new { ok = true });

            Assert.IsNotNull(GetError(result));
            StringAssert.Contains("required", GetError(result));
        }

        [Test]
        public void Execute_InvalidJson_ReturnsError()
        {
            var result = BatchExecutor.Execute<TestItem>("not valid json", item => new { ok = true });

            Assert.IsNotNull(GetError(result));
            StringAssert.Contains("Failed to parse", GetError(result));
        }

        [Test]
        public void Execute_EmptyArray_ReturnsError()
        {
            var result = BatchExecutor.Execute<TestItem>("[]", item => new { ok = true });

            Assert.IsNotNull(GetError(result));
            StringAssert.Contains("empty", GetError(result));
        }

        // ══════════════════════════════════════════════
        //  All items succeed
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_AllItemsSucceed_ReturnsSuccessTrue()
        {
            var json = "[{\"Name\":\"A\",\"Value\":1},{\"Name\":\"B\",\"Value\":2}]";

            var result = BatchExecutor.Execute<TestItem>(json, item => new { name = item.Name, doubled = item.Value * 2 });
            var j = ToJObject(result);

            Assert.IsTrue(j["success"].Value<bool>());
            Assert.AreEqual(2, j["totalItems"].Value<int>());
            Assert.AreEqual(2, j["successCount"].Value<int>());
            Assert.AreEqual(0, j["failCount"].Value<int>());
        }

        [Test]
        public void Execute_AllItemsSucceed_ResultsContainProcessorOutput()
        {
            var json = "[{\"Name\":\"Alpha\",\"Value\":10}]";

            var result = BatchExecutor.Execute<TestItem>(json, item => new { name = item.Name, doubled = item.Value * 2 });
            var j = ToJObject(result);

            var results = j["results"] as JArray;
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Alpha", results[0]["name"].ToString());
            Assert.AreEqual(20, results[0]["doubled"].Value<int>());
        }

        // ══════════════════════════════════════════════
        //  Some items fail
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_SomeItemsFail_ReturnsSuccessFalseWithCorrectCounts()
        {
            var json = "[{\"Name\":\"Good\",\"Value\":1},{\"Name\":\"Bad\",\"Value\":-1},{\"Name\":\"AlsoGood\",\"Value\":3}]";

            var result = BatchExecutor.Execute<TestItem>(json, item =>
            {
                if (item.Value < 0) throw new InvalidOperationException("Negative value not allowed");
                return new { name = item.Name };
            });
            var j = ToJObject(result);

            Assert.IsFalse(j["success"].Value<bool>());
            Assert.AreEqual(3, j["totalItems"].Value<int>());
            Assert.AreEqual(2, j["successCount"].Value<int>());
            Assert.AreEqual(1, j["failCount"].Value<int>());
        }

        [Test]
        public void Execute_FailedItem_ResultContainsErrorMessage()
        {
            var json = "[{\"Name\":\"Fail\",\"Value\":0}]";

            var result = BatchExecutor.Execute<TestItem>(json, item =>
            {
                throw new InvalidOperationException("deliberate error");
            });
            var j = ToJObject(result);

            var results = j["results"] as JArray;
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count);
            StringAssert.Contains("deliberate error", results[0]["error"].ToString());
            Assert.IsFalse(results[0]["success"].Value<bool>());
        }

        // ══════════════════════════════════════════════
        //  All items fail
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_AllItemsFail_ReturnsSuccessFalse()
        {
            var json = "[{\"Name\":\"A\",\"Value\":1},{\"Name\":\"B\",\"Value\":2}]";

            var result = BatchExecutor.Execute<TestItem>(json, item =>
            {
                throw new Exception("always fails");
            });
            var j = ToJObject(result);

            Assert.IsFalse(j["success"].Value<bool>());
            Assert.AreEqual(2, j["totalItems"].Value<int>());
            Assert.AreEqual(0, j["successCount"].Value<int>());
            Assert.AreEqual(2, j["failCount"].Value<int>());
        }

        // ══════════════════════════════════════════════
        //  itemIdentifier
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_WithItemIdentifier_ErrorTargetUsesIdentifier()
        {
            var json = "[{\"Name\":\"SpecialItem\",\"Value\":42}]";

            var result = BatchExecutor.Execute<TestItem>(
                json,
                item => { throw new Exception("boom"); },
                item => item.Name);
            var j = ToJObject(result);

            var results = j["results"] as JArray;
            Assert.IsNotNull(results);
            Assert.AreEqual("SpecialItem", results[0]["target"].ToString());
        }

        [Test]
        public void Execute_WithoutItemIdentifier_ErrorTargetUsesToString()
        {
            var json = "[{\"Name\":\"X\",\"Value\":1}]";

            var result = BatchExecutor.Execute<TestItem>(
                json,
                item => { throw new Exception("boom"); },
                itemIdentifier: null);
            var j = ToJObject(result);

            var results = j["results"] as JArray;
            Assert.IsNotNull(results);
            // Without identifier, falls back to item.ToString() which is the class name
            Assert.IsNotNull(results[0]["target"].ToString());
        }

        // ══════════════════════════════════════════════
        //  Single item
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_SingleItem_ReturnsCorrectTotalItems()
        {
            var json = "[{\"Name\":\"Only\",\"Value\":99}]";

            var result = BatchExecutor.Execute<TestItem>(json, item => new { name = item.Name });
            var j = ToJObject(result);

            Assert.AreEqual(1, j["totalItems"].Value<int>());
            Assert.AreEqual(1, j["successCount"].Value<int>());
        }

        // ══════════════════════════════════════════════
        //  setup / teardown hooks
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_SetupCalledBeforeProcessing()
        {
            var json = "[{\"Name\":\"A\",\"Value\":1}]";
            bool setupCalled = false;
            bool setupBeforeProcessor = false;

            var result = BatchExecutor.Execute<TestItem>(json, item =>
            {
                setupBeforeProcessor = setupCalled;
                return new { ok = true };
            }, setup: () => { setupCalled = true; });

            Assert.IsTrue(setupCalled, "setup should have been called");
            Assert.IsTrue(setupBeforeProcessor, "setup should run before processor");
        }

        [Test]
        public void Execute_TeardownCalledAfterProcessing()
        {
            var json = "[{\"Name\":\"A\",\"Value\":1}]";
            bool teardownCalled = false;

            var result = BatchExecutor.Execute<TestItem>(json, item =>
            {
                return new { ok = true };
            }, teardown: () => { teardownCalled = true; });

            Assert.IsTrue(teardownCalled, "teardown should have been called");
        }

        [Test]
        public void Execute_TeardownCalledEvenWhenAllItemsFail()
        {
            var json = "[{\"Name\":\"A\",\"Value\":1}]";
            bool teardownCalled = false;

            var result = BatchExecutor.Execute<TestItem>(json, item =>
            {
                throw new Exception("fail");
            }, teardown: () => { teardownCalled = true; });

            Assert.IsTrue(teardownCalled, "teardown should run even when items fail");
            var j = ToJObject(result);
            Assert.IsFalse(j["success"].Value<bool>());
        }

        [Test]
        public void Execute_SetupAndTeardownOrder()
        {
            var json = "[{\"Name\":\"A\",\"Value\":1},{\"Name\":\"B\",\"Value\":2}]";
            var callOrder = new System.Collections.Generic.List<string>();

            var result = BatchExecutor.Execute<TestItem>(json, item =>
            {
                callOrder.Add($"process:{item.Name}");
                return new { ok = true };
            },
            setup: () => { callOrder.Add("setup"); },
            teardown: () => { callOrder.Add("teardown"); });

            Assert.AreEqual(4, callOrder.Count);
            Assert.AreEqual("setup", callOrder[0]);
            Assert.AreEqual("process:A", callOrder[1]);
            Assert.AreEqual("process:B", callOrder[2]);
            Assert.AreEqual("teardown", callOrder[3]);
        }

        [Test]
        public void Execute_NullSetupAndTeardown_NoError()
        {
            var json = "[{\"Name\":\"A\",\"Value\":1}]";

            var result = BatchExecutor.Execute<TestItem>(json, item => new { ok = true },
                setup: null, teardown: null);
            var j = ToJObject(result);

            Assert.IsTrue(j["success"].Value<bool>());
        }
    }
}

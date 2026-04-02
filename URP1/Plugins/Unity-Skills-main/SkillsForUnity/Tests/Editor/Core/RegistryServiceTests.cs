using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace UnitySkills.Tests.Core
{
    [TestFixture]
    public class RegistryServiceTests
    {
        // ──────────────────────────────────────────────
        //  Reflection accessor for private static method
        // ──────────────────────────────────────────────

        private static readonly MethodInfo ComputeStableHashMethod =
            typeof(RegistryService).GetMethod(
                "ComputeStableHash",
                BindingFlags.NonPublic | BindingFlags.Static);

        private static string InvokeComputeStableHash(string input)
        {
            Assert.IsNotNull(ComputeStableHashMethod,
                "ComputeStableHash method not found via reflection. " +
                "Ensure the method exists as a private static member of RegistryService.");
            return (string)ComputeStableHashMethod.Invoke(null, new object[] { input });
        }

        // ══════════════════════════════════════════════
        //  Determinism
        // ══════════════════════════════════════════════

        [Test]
        public void ComputeStableHash_SameInput_AlwaysProducesSameOutput()
        {
            var result1 = InvokeComputeStableHash("deterministic-test");
            var result2 = InvokeComputeStableHash("deterministic-test");

            Assert.AreEqual(result1, result2);
        }

        [Test]
        public void ComputeStableHash_CalledMultipleTimes_RemainsStable()
        {
            const string input = "stability-check";
            var first = InvokeComputeStableHash(input);

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(first, InvokeComputeStableHash(input));
            }
        }

        // ══════════════════════════════════════════════
        //  Uniqueness
        // ══════════════════════════════════════════════

        [Test]
        public void ComputeStableHash_DifferentInputs_ProduceDifferentOutputs()
        {
            var result1 = InvokeComputeStableHash("input-alpha");
            var result2 = InvokeComputeStableHash("input-beta");

            Assert.AreNotEqual(result1, result2);
        }

        [Test]
        public void ComputeStableHash_SimilarInputs_ProduceDifferentOutputs()
        {
            var result1 = InvokeComputeStableHash("path/to/projectA");
            var result2 = InvokeComputeStableHash("path/to/projectB");

            Assert.AreNotEqual(result1, result2);
        }

        // ══════════════════════════════════════════════
        //  Output format
        // ══════════════════════════════════════════════

        [Test]
        public void ComputeStableHash_OutputIs8HexCharacters()
        {
            var result = InvokeComputeStableHash("format-test");

            Assert.AreEqual(8, result.Length,
                $"Expected 8 hex characters (4 bytes), got {result.Length}: '{result}'");
        }

        [Test]
        public void ComputeStableHash_OutputContainsOnlyHexCharacters()
        {
            var result = InvokeComputeStableHash("hex-check");

            Assert.IsTrue(Regex.IsMatch(result, @"^[0-9A-F]{8}$"),
                $"Expected uppercase hex string matching [0-9A-F]{{8}}, got: '{result}'");
        }

        [TestCase("short")]
        [TestCase("a-much-longer-input-string-that-exceeds-typical-lengths")]
        [TestCase("C:\\Users\\Dev\\Projects\\MyUnityProject")]
        public void ComputeStableHash_VariousInputs_AlwaysReturns8HexChars(string input)
        {
            var result = InvokeComputeStableHash(input);

            Assert.AreEqual(8, result.Length);
            Assert.IsTrue(Regex.IsMatch(result, @"^[0-9A-F]{8}$"),
                $"Invalid format for input '{input}': '{result}'");
        }

        // ══════════════════════════════════════════════
        //  Known value (SHA256 verification)
        // ══════════════════════════════════════════════

        [Test]
        public void ComputeStableHash_KnownInput_Test_ReturnsExpectedHash()
        {
            // SHA256("test") = 9f86d081884c7d659a2feaa0c55ad015...
            // First 4 bytes: 9F-86-D0-81 -> "9F86D081"
            var result = InvokeComputeStableHash("test");

            Assert.AreEqual("9F86D081", result);
        }

        [Test]
        public void ComputeStableHash_KnownInput_Hello_ReturnsExpectedHash()
        {
            // SHA256("hello") = 2cf24dba5fb0a30e26e83b2ac5b9e29e...
            // First 4 bytes: 2C-F2-4D-BA -> "2CF24DBA"
            var result = InvokeComputeStableHash("hello");

            Assert.AreEqual("2CF24DBA", result);
        }

        // ══════════════════════════════════════════════
        //  Edge cases
        // ══════════════════════════════════════════════

        [Test]
        public void ComputeStableHash_EmptyString_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => InvokeComputeStableHash(""));
        }

        [Test]
        public void ComputeStableHash_EmptyString_ReturnsExpectedHash()
        {
            // SHA256("") = e3b0c44298fc1c149afbf4c8996fb924...
            // First 4 bytes: E3-B0-C4-42 -> "E3B0C442"
            var result = InvokeComputeStableHash("");

            Assert.AreEqual("E3B0C442", result);
        }

        [Test]
        public void ComputeStableHash_UnicodeInput_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => InvokeComputeStableHash("\u4f60\u597d\u4e16\u754c"));
        }

        [Test]
        public void ComputeStableHash_UnicodeInput_ReturnsValidHex()
        {
            var result = InvokeComputeStableHash("\u4f60\u597d\u4e16\u754c");

            Assert.AreEqual(8, result.Length);
            Assert.IsTrue(Regex.IsMatch(result, @"^[0-9A-F]{8}$"),
                $"Unicode input produced invalid hex: '{result}'");
        }

        [Test]
        public void ComputeStableHash_UnicodeInput_IsDeterministic()
        {
            const string unicode = "\u00e4\u00f6\u00fc\u00df\u2603\u2764";
            var result1 = InvokeComputeStableHash(unicode);
            var result2 = InvokeComputeStableHash(unicode);

            Assert.AreEqual(result1, result2);
        }
    }
}

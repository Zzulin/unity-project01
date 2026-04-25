using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnitySkills
{
    /// <summary>
    /// Test runner skills.
    /// </summary>
    public static class TestSkills
    {
        private const string TestDiscoveryMode = "source_scan_with_file_dependencies";

        internal sealed class SmokeOutcome
        {
            public string Skill;
            public string Category;
            public string ProbeMode;
            public string Status;
            public bool? Valid;
            public string Error;
            public string[] MissingParams;
            public string[] SemanticWarnings;
            public string[] MetadataWarnings;
        }

        [UnitySkill("test_run", "Run Unity tests asynchronously. Returns a platform jobId immediately. Poll with job_status/job_wait or test_get_result(jobId).",
            Category = SkillCategory.Test, Operation = SkillOperation.Execute,
            Tags = new[] { "test", "run", "async", "editmode", "playmode", "job" },
            Outputs = new[] { "jobId", "testMode", "message" },
            SupportsDryRun = false)]
        public static object TestRun(string testMode = "EditMode", string filter = null)
        {
            if (!AsyncJobService.TryStartTestJob(testMode, filter, out var job, out var error))
                return new { success = false, error };

            return new
            {
                success = true,
                status = "accepted",
                jobId = job.jobId,
                kind = job.kind,
                testMode,
                filter,
                message = "Tests started. Use job_status/job_wait or test_get_result(jobId) to monitor progress."
            };
        }

        [UnitySkill("test_get_result", "Get the result of a test run. Compatible wrapper over the unified job model.",
            Category = SkillCategory.Test, Operation = SkillOperation.Query,
            Tags = new[] { "test", "result", "status", "poll", "job" },
            Outputs = new[] { "jobId", "status", "totalTests", "passedTests", "failedTests", "skippedTests", "inconclusiveTests", "otherTests", "failedTestNames" },
            RequiresInput = new[] { "jobId" },
            ReadOnly = true)]
        public static object TestGetResult(string jobId)
        {
            if (Validate.Required(jobId, "jobId") is object err)
                return err;

            var job = AsyncJobService.Get(jobId);
            if (job == null || job.kind != "test")
                return new { error = $"Test job not found: {jobId}" };

            return new
            {
                success = true,
                jobId,
                status = job.status,
                totalTests = GetResultInt(job, "totalTests"),
                passedTests = GetResultInt(job, "passedTests"),
                failedTests = GetResultInt(job, "failedTests"),
                skippedTests = GetResultInt(job, "skippedTests"),
                inconclusiveTests = GetResultInt(job, "inconclusiveTests"),
                otherTests = GetResultInt(job, "otherTests"),
                failedTestNames = GetResultStringList(job, "failedTestNames").ToArray(),
                elapsedSeconds = System.Math.Max(0, System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() - job.startedAt),
                resultSummary = job.resultSummary,
                error = job.error
            };
        }

        [UnitySkill("test_list", "List available tests",
            Category = SkillCategory.Test, Operation = SkillOperation.Query,
            Tags = new[] { "test", "list", "discover", "enumerate" },
            Outputs = new[] { "testMode", "count", "tests" },
            ReadOnly = true)]
        public static object TestList(string testMode = "EditMode", int limit = 100)
        {
            var tests = DiscoverTests(testMode)
                .Take(Mathf.Max(1, limit))
                .Select(test => new
                {
                    name = test.Name,
                    fullName = test.FullName,
                    runState = test.RunState
                })
                .ToArray();

            return new
            {
                success = true,
                testMode,
                count = tests.Length,
                discoveryMode = TestDiscoveryMode,
                tests
            };
        }

        [UnitySkill("test_cancel", "Cancel a running test job if supported. Unity TestRunner itself does not provide a hard cancel.",
            Category = SkillCategory.Test, Operation = SkillOperation.Execute,
            Tags = new[] { "test", "cancel", "abort", "stop", "job" },
            Outputs = new[] { "cancelled" },
            RequiresInput = new[] { "jobId" })]
        public static object TestCancel(string jobId = null)
        {
            if (Validate.Required(jobId, "jobId") is object err)
                return err;

            var job = AsyncJobService.Cancel(jobId);
            if (job == null || job.kind != "test")
                return new { error = $"Test job not found: {jobId}" };

            return new
            {
                success = true,
                jobId = job.jobId,
                status = job.status,
                cancelled = job.status == "cancelled",
                note = "Unity TestRunnerApi does not support direct cancellation. The unified job layer only reports supported cancellation states.",
                warnings = job.warnings
            };
        }

        private static void CollectTests(ITestAdaptor test, List<object> tests, int limit)
        {
            if (tests.Count >= limit)
                return;

            if (!test.HasChildren)
            {
                tests.Add(new
                {
                    name = test.Name,
                    fullName = test.FullName,
                    runState = test.RunState.ToString()
                });
                return;
            }

            foreach (var child in test.Children)
                CollectTests(child, tests, limit);
        }

        [UnitySkill("test_run_by_name", "Run specific tests by class or method name. Returns a unified jobId.",
            Category = SkillCategory.Test, Operation = SkillOperation.Execute,
            Tags = new[] { "test", "run", "name", "specific", "job" },
            Outputs = new[] { "jobId", "testName", "testMode" },
            SupportsDryRun = false)]
        public static object TestRunByName(string testName, string testMode = "EditMode")
        {
            if (Validate.Required(testName, "testName") is object err)
                return err;

            if (!AsyncJobService.TryStartTestJob(testMode, testName, out var job, out var error))
                return new { success = false, error };

            return new
            {
                success = true,
                status = "accepted",
                jobId = job.jobId,
                testName,
                testMode
            };
        }

        [UnitySkill("test_get_last_result", "Get the most recent test run result",
            Category = SkillCategory.Test, Operation = SkillOperation.Query,
            Tags = new[] { "test", "result", "last", "recent" },
            Outputs = new[] { "jobId", "status", "total", "passed", "failed", "skipped", "inconclusive", "other", "failedNames" },
            ReadOnly = true)]
        public static object TestGetLastResult()
        {
            var last = EnumerateRealTestRuns(100)
                .OrderByDescending(job => job.startedAt)
                .FirstOrDefault();
            if (last == null)
                return new { error = "No test runs found" };

            return new
            {
                success = true,
                jobId = last.jobId,
                status = last.status,
                total = GetResultInt(last, "totalTests"),
                passed = GetResultInt(last, "passedTests"),
                failed = GetResultInt(last, "failedTests"),
                skipped = GetResultInt(last, "skippedTests"),
                inconclusive = GetResultInt(last, "inconclusiveTests"),
                other = GetResultInt(last, "otherTests"),
                failedNames = GetResultStringList(last, "failedTestNames").ToArray()
            };
        }

        [UnitySkill("test_list_categories", "List test categories",
            Category = SkillCategory.Test, Operation = SkillOperation.Query,
            Tags = new[] { "test", "categories", "list", "nunit" },
            Outputs = new[] { "count", "categories" },
            ReadOnly = true)]
        public static object TestListCategories(string testMode = "EditMode")
        {
            var categories = DiscoverTests(testMode)
                .SelectMany(test => test.Categories ?? Array.Empty<string>())
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new
            {
                success = true,
                count = categories.Length,
                categories,
                discoveryMode = TestDiscoveryMode,
                note = categories.Length == 0
                    ? "No [Category] attributes were found in discovered tests."
                    : null
            };
        }

        [UnitySkill("test_smoke_skills", "Run a reusable smoke test across registered skills. Executes safe read-only skills and dry-runs the rest for broad regression coverage.",
            Category = SkillCategory.Test, Operation = SkillOperation.Analyze,
            Tags = new[] { "test", "smoke", "skills", "regression", "coverage" },
            Outputs = new[] { "totalSkills", "executedCount", "dryRunCount", "failureCount", "results" },
            ReadOnly = true)]
        public static object TestSmokeSkills(
            string category = null,
            string nameContains = null,
            string excludeNamesCsv = null,
            bool executeReadOnly = true,
            bool includeMutating = true,
            int limit = 0,
            bool runAsync = true,
            int chunkSize = 25,
            int failureItemLimit = 50)
        {
            var request = BuildSmokeRequest(category, nameContains, excludeNamesCsv, executeReadOnly, includeMutating, limit);

            if (runAsync)
            {
                var job = AsyncJobService.StartSmokeJob(request.SelectedSkills, request.MetadataIssues, executeReadOnly, chunkSize, failureItemLimit);
                return new
                {
                    success = true,
                    status = "accepted",
                    jobId = job.jobId,
                    kind = job.kind,
                    totalSkills = request.SelectedSkills.Length,
                    filters = new
                    {
                        category,
                        nameContains,
                        excludeNames = request.ExcludedNames.OrderBy(name => name).ToArray(),
                        executeReadOnly,
                        includeMutating,
                        limit,
                        chunkSize,
                        failureItemLimit
                    },
                    message = "Smoke test job created. Use job_status/job_wait to monitor progress."
                };
            }

            var results = new List<object>(request.SelectedSkills.Length);
            int executedCount = 0;
            int dryRunCount = 0;
            int skippedCount = 0;
            int failureCount = 0;

            foreach (var skill in request.SelectedSkills)
            {
                var outcome = EvaluateSmokeSkill(skill, request.MetadataIssues, executeReadOnly);
                if (string.Equals(outcome.ProbeMode, "execute", StringComparison.OrdinalIgnoreCase))
                    executedCount++;
                else if (string.Equals(outcome.ProbeMode, "dryRun", StringComparison.OrdinalIgnoreCase))
                    dryRunCount++;

                if (string.Equals(outcome.Status, "error", StringComparison.OrdinalIgnoreCase))
                    failureCount++;

                if (string.Equals(outcome.Status, "skipped", StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(outcome.Status, "dryRun", StringComparison.OrdinalIgnoreCase) && !outcome.Valid.GetValueOrDefault(true)))
                {
                    skippedCount++;
                }

                results.Add(new
                {
                    skill = outcome.Skill,
                    category = outcome.Category,
                    readOnly = skill.ReadOnly,
                    riskLevel = skill.RiskLevel,
                    probeMode = outcome.ProbeMode,
                    status = outcome.Status,
                    valid = outcome.Valid,
                    missingParams = outcome.MissingParams ?? Array.Empty<string>(),
                    semanticWarnings = outcome.SemanticWarnings ?? Array.Empty<string>(),
                    metadataWarnings = outcome.MetadataWarnings ?? Array.Empty<string>(),
                    error = outcome.Error
                });
            }

            return new
            {
                success = failureCount == 0,
                totalSkills = request.SelectedSkills.Length,
                executedCount,
                dryRunCount,
                skippedCount,
                failureCount,
                filters = new
                {
                    category,
                    nameContains,
                    excludeNames = request.ExcludedNames.OrderBy(name => name).ToArray(),
                    executeReadOnly,
                    includeMutating,
                    limit
                },
                note = "Read-only skills with no required inputs are executed directly; all other skills are smoke-tested via dryRun with empty arguments.",
                results
            };
        }

        internal static SmokeOutcome EvaluateSmokeSkill(SkillRouter.SkillInfo skill, string[] metadataIssues, bool executeReadOnly)
        {
            var validation = SkillRouter.ValidateParameters(skill, "{}");
            var canExecuteReadOnly = executeReadOnly &&
                                     skill.ReadOnly &&
                                     validation.MissingParams.Count == 0 &&
                                     validation.TypeErrors.Count == 0 &&
                                     !skill.MayTriggerReload;

            if (skill.MayTriggerReload)
            {
                return new SmokeOutcome
                {
                    Skill = skill.Name,
                    Category = skill.Category != SkillCategory.Uncategorized ? skill.Category.ToString() : null,
                    ProbeMode = "skipped",
                    Status = "skipped",
                    Valid = false,
                    Error = "MayTriggerReload — executing would cause Domain Reload and break subsequent skills",
                    MetadataWarnings = FindMetadataWarnings(metadataIssues, skill.Name)
                };
            }

            var probeMode = canExecuteReadOnly ? "execute" : "dryRun";
            JObject response;
            try
            {
                response = probeMode == "execute"
                    ? ExecuteSmokeProbe(skill, validation)
                    : JObject.Parse(SkillRouter.DryRun(skill.Name, "{}"));
            }
            catch (Exception ex)
            {
                return new SmokeOutcome
                {
                    Skill = skill.Name,
                    Category = skill.Category != SkillCategory.Uncategorized ? skill.Category.ToString() : null,
                    ProbeMode = probeMode,
                    Status = "error",
                    Valid = false,
                    Error = $"Smoke test produced non-JSON response: {ex.Message}",
                    MetadataWarnings = FindMetadataWarnings(metadataIssues, skill.Name)
                };
            }

            return new SmokeOutcome
            {
                Skill = skill.Name,
                Category = skill.Category != SkillCategory.Uncategorized ? skill.Category.ToString() : null,
                ProbeMode = probeMode,
                Status = response["status"]?.ToString() ?? "unknown",
                Valid = response["valid"]?.Value<bool?>(),
                Error = response["error"]?.ToString(),
                MissingParams = response["validation"]?["missingParams"]?.ToObject<string[]>() ?? Array.Empty<string>(),
                SemanticWarnings = response["validation"]?["warnings"]?.ToObject<string[]>() ?? Array.Empty<string>(),
                MetadataWarnings = FindMetadataWarnings(metadataIssues, skill.Name)
            };
        }

        private static void CollectCategories(ITestAdaptor test, HashSet<string> categories)
        {
            if (test.Categories != null)
                foreach (var cat in test.Categories)
                    categories.Add(cat);
            if (test.HasChildren)
                foreach (var child in test.Children)
                    CollectCategories(child, categories);
        }

        [UnitySkill("test_create_editmode", "Create an EditMode test script template and return a compile-monitor job.",
            Category = SkillCategory.Test, Operation = SkillOperation.Create,
            Tags = new[] { "test", "create", "editmode", "template", "job" },
            Outputs = new[] { "path", "testName", "jobId" })]
        public static object TestCreateEditMode(string testName, string folder = "Assets/Tests/Editor")
        {
            if (Validate.Required(testName, "testName") is object nameErr) return nameErr;
            if (testName.Contains("/") || testName.Contains("\\") || testName.Contains(".."))
                return new { error = "testName must not contain path separators" };
            if (Validate.SafePath(folder, "folder") is object folderErr) return folderErr;
            if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
            var path = System.IO.Path.Combine(folder, testName + ".cs");
            if (System.IO.File.Exists(path)) return new { error = $"File already exists: {path}" };
            var content = $@"using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class {testName}
{{
    [Test]
    public void SampleTest()
    {{
        Assert.Pass();
    }}
}}
";
            System.IO.File.WriteAllText(path, content, SkillsCommon.Utf8NoBom);
            AssetDatabase.ImportAsset(path);
            var job = AsyncJobService.StartScriptMutationJob("test_create_editmode", path.Replace("\\", "/"), true, 20);
            return new
            {
                success = true,
                status = "accepted",
                path,
                testName,
                jobId = job.jobId,
                serverAvailability = ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                    $"已创建测试脚本: {path}。Unity 可能短暂重载脚本域。",
                    alwaysInclude: true)
            };
        }

        [UnitySkill("test_create_playmode", "Create a PlayMode test script template and return a compile-monitor job.",
            Category = SkillCategory.Test, Operation = SkillOperation.Create,
            Tags = new[] { "test", "create", "playmode", "template", "job" },
            Outputs = new[] { "path", "testName", "jobId" })]
        public static object TestCreatePlayMode(string testName, string folder = "Assets/Tests/Runtime")
        {
            if (Validate.Required(testName, "testName") is object nameErr) return nameErr;
            if (testName.Contains("/") || testName.Contains("\\") || testName.Contains(".."))
                return new { error = "testName must not contain path separators" };
            if (Validate.SafePath(folder, "folder") is object folderErr) return folderErr;
            if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
            var path = System.IO.Path.Combine(folder, testName + ".cs");
            if (System.IO.File.Exists(path)) return new { error = $"File already exists: {path}" };
            var content = $@"using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class {testName}
{{
    [UnityTest]
    public IEnumerator SamplePlayModeTest()
    {{
        yield return null;
        Assert.Pass();
    }}
}}
";
            System.IO.File.WriteAllText(path, content, SkillsCommon.Utf8NoBom);
            AssetDatabase.ImportAsset(path);
            var job = AsyncJobService.StartScriptMutationJob("test_create_playmode", path.Replace("\\", "/"), true, 20);
            return new
            {
                success = true,
                status = "accepted",
                path,
                testName,
                jobId = job.jobId,
                serverAvailability = ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                    $"已创建测试脚本: {path}。Unity 可能短暂重载脚本域。",
                    alwaysInclude: true)
            };
        }

        [UnitySkill("test_get_summary", "Get aggregated test summary across all runs",
            Category = SkillCategory.Test, Operation = SkillOperation.Query,
            Tags = new[] { "test", "summary", "aggregate", "report" },
            Outputs = new[] { "totalRuns", "completedRuns", "totalPassed", "totalFailed", "totalSkipped", "totalInconclusive", "totalOther", "allFailedTests" },
            ReadOnly = true)]
        public static object TestGetSummary()
        {
            var runs = EnumerateRealTestRuns(200).ToList();
            return new
            {
                success = true,
                totalRuns = runs.Count,
                completedRuns = runs.Count(r => r.status == "completed"),
                totalPassed = runs.Sum(r => GetResultInt(r, "passedTests")),
                totalFailed = runs.Sum(r => GetResultInt(r, "failedTests")),
                totalSkipped = runs.Sum(r => GetResultInt(r, "skippedTests")),
                totalInconclusive = runs.Sum(r => GetResultInt(r, "inconclusiveTests")),
                totalOther = runs.Sum(r => GetResultInt(r, "otherTests")),
                allFailedTests = runs
                    .SelectMany(r => GetResultStringList(r, "failedTestNames"))
                    .Distinct()
                    .ToArray()
            };
        }

        private static JObject ExecuteSmokeProbe(SkillRouter.SkillInfo skill, SkillRouter.ParameterValidationResult validation)
        {
            using (BatchPersistence.BeginTransientScope())
            {
                if (validation.UnknownParams.Count > 0)
                {
                    return JObject.FromObject(new
                    {
                        status = "error",
                        error = $"Unknown parameters: {validation.UnknownParams.Count}"
                    });
                }

                if (validation.MissingParams.Count > 0)
                {
                    return JObject.FromObject(new
                    {
                        status = "dryRun",
                        valid = false,
                        validation = new
                        {
                            missingParams = validation.MissingParams.ToArray(),
                            semanticErrors = validation.SemanticErrors.ToArray(),
                            warnings = validation.Warnings.ToArray()
                        }
                    });
                }

                if (validation.TypeErrors.Count > 0 || validation.SemanticErrors.Count > 0)
                {
                    return JObject.FromObject(new
                    {
                        status = "dryRun",
                        valid = false,
                        validation = new
                        {
                            missingParams = validation.MissingParams.ToArray(),
                            typeErrors = validation.TypeErrors.ToArray(),
                            semanticErrors = validation.SemanticErrors.ToArray(),
                            warnings = validation.Warnings.ToArray()
                        }
                    });
                }

                try
                {
                    var result = skill.Method.Invoke(null, validation.InvokeArgs);
                    if (SkillResultHelper.TryGetError(result, out var errorText))
                    {
                        return JObject.FromObject(new
                        {
                            status = "error",
                            error = errorText
                        });
                    }

                    return JObject.FromObject(new
                    {
                        status = "success"
                    });
                }
                catch (Exception ex)
                {
                    var actual = ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null
                        ? tie.InnerException
                        : ex;

                    return JObject.FromObject(new
                    {
                        status = "error",
                        error = actual.Message
                    });
                }
            }
        }

        private static SmokeRequest BuildSmokeRequest(
            string category,
            string nameContains,
            string excludeNamesCsv,
            bool executeReadOnly,
            bool includeMutating,
            int limit)
        {
            SkillRouter.Initialize();

            var excludedNames = ParseCsv(excludeNamesCsv);
            var metadataIssues = SkillRouter.ValidateMetadata().ToArray();
            IEnumerable<SkillRouter.SkillInfo> skills = SkillRouter.GetAllSkillsSnapshot();

            if (!string.IsNullOrWhiteSpace(category) &&
                Enum.TryParse(category, true, out SkillCategory parsedCategory))
            {
                skills = skills.Where(skill => skill.Category == parsedCategory);
            }

            if (!string.IsNullOrWhiteSpace(nameContains))
            {
                skills = skills.Where(skill =>
                    skill.Name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (excludedNames.Count > 0)
            {
                skills = skills.Where(skill => !excludedNames.Contains(skill.Name));
            }

            if (!includeMutating)
            {
                skills = skills.Where(skill => skill.ReadOnly);
            }

            if (limit > 0)
                skills = skills.Take(limit);

            return new SmokeRequest
            {
                SelectedSkills = skills.ToArray(),
                MetadataIssues = metadataIssues,
                ExcludedNames = excludedNames
            };
        }

        private static IEnumerable<BatchJobRecord> EnumerateRealTestRuns(int limit)
        {
            return AsyncJobService.List(limit)
                .Where(IsRealTestRun)
                .ToArray();
        }

        private static bool IsRealTestRun(BatchJobRecord job)
        {
            if (job == null || !string.Equals(job.kind, "test", StringComparison.OrdinalIgnoreCase))
                return false;

            if (job.metadata != null &&
                job.metadata.TryGetValue("synthetic", out var syntheticValue) &&
                syntheticValue is bool synthetic &&
                synthetic)
            {
                return false;
            }

            return true;
        }

        private static string[] FindMetadataWarnings(IEnumerable<string> metadataIssues, string skillName)
        {
            var issueTag = $"] {skillName}: ";
            return metadataIssues?
                .Where(issue => issue.IndexOf(issueTag, StringComparison.Ordinal) >= 0)
                .ToArray() ?? Array.Empty<string>();
        }

        private static IReadOnlyList<DiscoveredTestCase> DiscoverTests(string testMode)
        {
            var discovered = new List<DiscoveredTestCase>();
            var includePlayMode = string.Equals(testMode, "PlayMode", StringComparison.OrdinalIgnoreCase);
            foreach (var filePath in EnumerateTestSourceFiles(includePlayMode))
                discovered.AddRange(DiscoverTestsFromSource(filePath, includePlayMode));

            return discovered
                .OrderBy(test => test.FullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        internal static string[] ResolveExactTestNames(string testMode, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return Array.Empty<string>();

            return DiscoverTests(testMode)
                .Where(test =>
                    string.Equals(test.FullName, filter, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(test.Name, filter, StringComparison.OrdinalIgnoreCase))
                .Select(test => test.FullName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        internal static bool MatchesDiscoveredTestGroup(string testMode, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return false;

            return DiscoverTests(testMode).Any(test =>
                test.FullName.StartsWith(filter + ".", StringComparison.OrdinalIgnoreCase) ||
                test.FullName.IndexOf("." + filter + ".", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        internal static string[] ResolveGroupedTestNames(string testMode, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return Array.Empty<string>();

            return DiscoverTests(testMode)
                .Where(test =>
                    test.FullName.StartsWith(filter + ".", StringComparison.OrdinalIgnoreCase) ||
                    test.FullName.IndexOf("." + filter + ".", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(test => test.FullName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        internal static string[] ResolveGroupAssemblyNames(string testMode, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return Array.Empty<string>();

            return DiscoverTests(testMode)
                .Where(test =>
                    test.FullName.StartsWith(filter + ".", StringComparison.OrdinalIgnoreCase) ||
                    test.FullName.IndexOf("." + filter + ".", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(test => test.AssemblyName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static IEnumerable<string> EnumerateTestSourceFiles(bool includePlayMode)
        {
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Application.dataPath,
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages"))
            };

            foreach (var fileDependencyRoot in EnumerateFileDependencyRoots())
                roots.Add(fileDependencyRoot);

            foreach (var root in roots.Where(Directory.Exists))
            {
                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories);
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                {
                    var normalized = file.Replace('\\', '/');
                    if (normalized.IndexOf("/Tests/", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    var isPlayModeFile = normalized.IndexOf("/Tests/Runtime/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         normalized.IndexOf("/Tests/PlayMode/", StringComparison.OrdinalIgnoreCase) >= 0;
                    var isEditModeFile = normalized.IndexOf("/Tests/Editor/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         normalized.IndexOf("/Editor/Tests/", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (includePlayMode)
                    {
                        if (!isPlayModeFile)
                            continue;
                    }
                    else if (isPlayModeFile && !isEditModeFile)
                    {
                        continue;
                    }

                    yield return file;
                }
            }
        }

        private static IEnumerable<string> EnumerateFileDependencyRoots()
        {
            var manifestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));
            if (!File.Exists(manifestPath))
                yield break;

            JObject manifest;
            try
            {
                manifest = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(manifestPath));
            }
            catch
            {
                yield break;
            }

            var dependencies = manifest?["dependencies"] as JObject;
            if (dependencies == null)
                yield break;

            foreach (var property in dependencies.Properties())
            {
                var value = property.Value?.ToString();
                if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                    continue;

                var root = value.Substring("file:".Length).Replace('/', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(root))
                    yield return root;
                else
                    yield return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifestPath) ?? string.Empty, root));
            }
        }

        private static IEnumerable<DiscoveredTestCase> DiscoverTestsFromSource(string filePath, bool includePlayMode)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(filePath);
            }
            catch
            {
                yield break;
            }

            var assemblyName = ResolveAssemblyNameForSourceFile(filePath);
            var namespaceName = string.Empty;
            var currentClass = string.Empty;
            var classCategories = Array.Empty<string>();
            var pendingAttributes = new List<string>();

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    pendingAttributes.Clear();
                    continue;
                }

                if (line.StartsWith("[", StringComparison.Ordinal))
                {
                    pendingAttributes.Add(line);
                    continue;
                }

                var namespaceMatch = Regex.Match(line, @"^namespace\s+(?<ns>[\w\.]+)");
                if (namespaceMatch.Success)
                {
                    namespaceName = namespaceMatch.Groups["ns"].Value;
                    pendingAttributes.Clear();
                    continue;
                }

                var classMatch = Regex.Match(line, @"^(?:public|internal|private|protected|sealed|abstract|static|partial|\s)*class\s+(?<name>\w+)");
                if (classMatch.Success)
                {
                    currentClass = classMatch.Groups["name"].Value;
                    classCategories = ExtractCategoryNames(pendingAttributes).ToArray();
                    pendingAttributes.Clear();
                    continue;
                }

                var methodMatch = Regex.Match(line, @"^(?:public|internal|private|protected|static|virtual|override|sealed|async|\s)+[\w<>\[\],\.\s]+\s+(?<name>\w+)\s*\(");
                if (!methodMatch.Success)
                {
                    pendingAttributes.Clear();
                    continue;
                }

                var isUnityTest = ContainsAttribute(pendingAttributes, "UnityTest");
                var isTest = ContainsAttribute(pendingAttributes, "Test") ||
                             ContainsAttribute(pendingAttributes, "TestCase") ||
                             isUnityTest;
                if (!isTest || string.IsNullOrWhiteSpace(currentClass))
                {
                    pendingAttributes.Clear();
                    continue;
                }

                var categories = classCategories
                    .Concat(ExtractCategoryNames(pendingAttributes))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var methodName = methodMatch.Groups["name"].Value;
                yield return new DiscoveredTestCase
                {
                    Name = methodName,
                    FullName = string.IsNullOrWhiteSpace(namespaceName)
                        ? $"{currentClass}.{methodName}"
                        : $"{namespaceName}.{currentClass}.{methodName}",
                    AssemblyName = assemblyName,
                    RunState = ContainsAttribute(pendingAttributes, "Ignore") ? "Ignored" :
                               ContainsAttribute(pendingAttributes, "Explicit") ? "Explicit" : "Runnable",
                    Categories = categories
                };

                pendingAttributes.Clear();
            }
        }

        private static bool ContainsAttribute(IEnumerable<string> attributes, string attributeName)
        {
            return attributes.Any(attribute =>
                attribute.IndexOf($"[{attributeName}", StringComparison.OrdinalIgnoreCase) >= 0 ||
                attribute.IndexOf($"[{attributeName}Attribute", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static IEnumerable<string> ExtractCategoryNames(IEnumerable<string> attributes)
        {
            foreach (var attribute in attributes)
            {
                foreach (Match match in Regex.Matches(attribute, @"Category(?:Attribute)?\s*\(\s*""(?<name>[^""]+)""\s*\)", RegexOptions.IgnoreCase))
                {
                    var category = match.Groups["name"].Value;
                    if (!string.IsNullOrWhiteSpace(category))
                        yield return category;
                }
            }
        }

        private static string ResolveAssemblyNameForSourceFile(string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                while (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    var asmdefPath = Directory.EnumerateFiles(directory, "*.asmdef", SearchOption.TopDirectoryOnly)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(asmdefPath))
                    {
                        var asmdefJson = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(asmdefPath));
                        var assemblyName = asmdefJson?["name"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(assemblyName))
                            return assemblyName;
                    }

                    directory = Path.GetDirectoryName(directory);
                }
            }
            catch
            {
            }

            return null;
        }

        private static int GetResultInt(BatchJobRecord job, string key)
        {
            if (job?.resultData == null || !job.resultData.TryGetValue(key, out var value) || value == null)
                return 0;

            if (value is int intValue)
                return intValue;
            if (value is long longValue)
                return (int)longValue;
            return int.TryParse(value.ToString(), out var parsed) ? parsed : 0;
        }

        private static IEnumerable<string> GetResultStringList(BatchJobRecord job, string key)
        {
            if (job?.resultData == null || !job.resultData.TryGetValue(key, out var value) || value == null)
                return Enumerable.Empty<string>();

            if (value is IEnumerable<string> stringList)
                return stringList;

            if (value is IEnumerable<object> objectList)
                return objectList.Select(item => item?.ToString()).Where(item => !string.IsNullOrEmpty(item));

            return Enumerable.Empty<string>();
        }

        private static HashSet<string> ParseCsv(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return csv
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class SmokeRequest
        {
            public SkillRouter.SkillInfo[] SelectedSkills;
            public string[] MetadataIssues;
            public HashSet<string> ExcludedNames;
        }

        private sealed class DiscoveredTestCase
        {
            public string Name;
            public string FullName;
            public string AssemblyName;
            public string RunState;
            public string[] Categories;
        }
    }
}

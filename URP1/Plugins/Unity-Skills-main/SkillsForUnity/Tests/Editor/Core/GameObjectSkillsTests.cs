using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnitySkills.Tests.Core
{
    [TestFixture]
    public class GameObjectSkillsTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObjectFinder.InvalidateCache();
        }

        [TearDown]
        public void TearDown()
        {
            GameObjectFinder.InvalidateCache();
        }

        [Test]
        public void GameObjectGetInfo_IncludesParentAndChildPaths()
        {
            var root = new GameObject("Root");
            var parent = new GameObject("Parent");
            parent.transform.SetParent(root.transform);
            var child = new GameObject("Child");
            child.transform.SetParent(parent.transform);

            GameObjectFinder.InvalidateCache();

            var result = GameObjectSkills.GameObjectGetInfo(path: "Root/Parent");

            Assert.AreEqual("Root/Parent", GetProperty<string>(result, "path"));
            Assert.AreEqual("Root", GetProperty<string>(result, "parentPath"));

            var children = ((IEnumerable)GetProperty<object>(result, "children")).Cast<object>().ToArray();
            Assert.AreEqual(1, children.Length);
            Assert.AreEqual("Root/Parent/Child", GetProperty<string>(children[0], "path"));
        }

        private static T GetProperty<T>(object target, string name)
        {
            var property = target.GetType().GetProperty(name);
            Assert.IsNotNull(property, $"Property '{name}' not found.");
            return (T)property.GetValue(target);
        }
    }
}

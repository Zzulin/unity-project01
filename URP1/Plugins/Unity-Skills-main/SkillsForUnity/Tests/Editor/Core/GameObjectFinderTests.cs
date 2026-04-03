using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnitySkills.Tests.Core
{
    [TestFixture]
    public class GameObjectFinderTests
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
        public void GetPath_AndGetDepth_ReturnExpectedHierarchyMetadata()
        {
            var root = new GameObject("Root");
            var child = new GameObject("Child");
            child.transform.SetParent(root.transform);
            var leaf = new GameObject("Leaf");
            leaf.transform.SetParent(child.transform);

            GameObjectFinder.InvalidateCache();

            Assert.AreEqual("Root/Child/Leaf", GameObjectFinder.GetPath(leaf));
            Assert.AreEqual("Root/Child/Leaf", GameObjectFinder.GetCachedPath(leaf));
            Assert.AreEqual(2, GameObjectFinder.GetDepth(leaf));
        }

        [Test]
        public void FindByPath_SupportsPlainAndScenePrefixedPaths()
        {
            var scene = SceneManager.GetActiveScene();
            var canvas = new GameObject("Canvas");
            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvas.transform);
            var button = new GameObject("Button");
            button.transform.SetParent(panel.transform);

            GameObjectFinder.InvalidateCache();

            Assert.AreSame(button, GameObjectFinder.FindByPath("Canvas/Panel/Button"));
            Assert.AreSame(button, GameObjectFinder.FindByPath(scene.name + "/Canvas/Panel/Button"));
        }

        [Test]
        public void GetSceneObjects_ReturnsLoadedSceneHierarchy()
        {
            var rootA = new GameObject("RootA");
            var rootB = new GameObject("RootB");
            var child = new GameObject("Child");
            child.transform.SetParent(rootA.transform);

            GameObjectFinder.InvalidateCache();

            var objects = GameObjectFinder.GetSceneObjects();

            CollectionAssert.IsSupersetOf(objects, new[] { rootA, rootB, child });
        }
    }
}

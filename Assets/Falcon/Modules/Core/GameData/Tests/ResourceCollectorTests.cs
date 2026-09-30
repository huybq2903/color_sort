/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-10
     */


using System.Collections;
using Falcon.Modules.Core.GameData.Runtime;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Falcon.Modules.Core.GameData.Tests
{
    using Falcon.Modules.Core.AccountData;
    using Falcon.Modules.Core.Network;
    using UnityEngine;

    public class ResourceCollectorTests
    {
        private bool   _onResourceChangeCalled;
        private string _changedResource;
        private int    _changedAmount;

        [SetUp]
        public void SetUp()
        {
            _onResourceChangeCalled = false;
            _changedResource = string.Empty;
            _changedAmount = 0;
        }

        [Test]
        public void Init_LoadsGoldResourceFromGameDataCore()
        {
            // Assert
            Assert.DoesNotThrow(() => ResourceCollector.Instance.ResourceAdd("gold", 10, null, "test"));
        }

        [Test]
        public void AddToResourcesMap_AddsCustomResource()
        {
            // Arrange
            var customResource = new CustomTestResource();
            
            // Act
            ResourceCollector.Instance.AddToResourcesMap(customResource);
            ResourceCollector.Instance.ResourceAdd("resource_custom_test", 50, null, "test");

            // Assert
            Assert.AreEqual(50, customResource.CurrentAmount);
        }
        
        [UnityTest]
        public IEnumerator SaveAndUpdateServerOfResourceData_Act()
        {
            // Wait
            yield return null;
            
            // Act
            ResourceCollector.Instance.SaveAndUpdateServerOfResourceData("gold");
        }

        [UnityTest]
        public IEnumerator SaveResourcesAndUpdateToServer_SavesAllData()
        {
            // Wait
            yield return null;
            
            // Act
            ResourceCollector.Instance.SaveResourcesAndUpdateToServer();
        }

        [Test]
        public void AddResources_AddsToGoldAndInvokesEvent()
        {
            // Arrange
            ResourceCollector.Instance.AddResourceChangeListener(OnResourceChangeHandler);

            // Act
            ResourceCollector.Instance.ResourceAdd("gold", 100, "test_data", "test");

            // Assert
            Assert.IsTrue(_onResourceChangeCalled);
            Assert.AreEqual("gold", _changedResource);
            Assert.AreEqual(100, _changedAmount);
            
            // Cleanup
            ResourceCollector.Instance.RemoveResourceChangeListener(OnResourceChangeHandler);
        }

        [UnityTest]
        public IEnumerator AddResources_AddResourceToServer()
        {
            AccountManager.Instance.Init();
            
            yield return new WaitForSeconds(5f);
            
            ResourceCollector.Instance.ResourceAdd("gold", 100, "test_data", "test");
            ResourceCollector.Instance.ResourceAdd("lives", 1, "test_data", "test");
            AccountManager.Instance.SaveGameDatas();
            AccountManager.Instance.UpdateToServer();
        }
        
        [UnityTest]
        public IEnumerator AddResources_UpdateResourceFromServer()
        {
            AccountManager.Instance.Init();

            yield return new WaitForSeconds(5f);

            foreach (var kvp in GameDataCore.Instance.resources)
            {
                Debug.Log(kvp.Value);
            }
        }

        [Test]
        public void RemoveResources_RemovesFromGoldAndInvokesEvent()
        {
            // Arrange
            ResourceCollector.Instance.ResourceAdd("gold", 200, null);
            ResourceCollector.Instance.AddResourceChangeListener(OnResourceChangeHandler);

            // Act
            ResourceCollector.Instance.ResourceRemove("gold", 50, "test_data", "test");

            // Assert
            Assert.IsTrue(_onResourceChangeCalled);
            Assert.AreEqual("gold", _changedResource);
            Assert.AreEqual(-50, _changedAmount);
            
            // Cleanup
            ResourceCollector.Instance.RemoveResourceChangeListener(OnResourceChangeHandler);
        }

        [Test]
        public void AddRemoveResourceChangeListener_WorksCorrectly()
        {
            // Arrange
            ResourceCollector.Instance.AddResourceChangeListener(OnResourceChangeHandler);
            
            // Act
            ResourceCollector.Instance.ResourceAdd("gold", 10, null, "test");
            
            // Assert
            Assert.IsTrue(_onResourceChangeCalled, "Listener should have been called.");

            // Arrange 2
            _onResourceChangeCalled = false;
            ResourceCollector.Instance.RemoveResourceChangeListener(OnResourceChangeHandler);
            
            // Act 2
            ResourceCollector.Instance.ResourceAdd("gold", 20, null);

            // Assert 2
            Assert.IsFalse(_onResourceChangeCalled, "Listener should not have been called after removal.");
        }

        [Test]
        public void GetResourceConfigById_WhenConfigExists_ReturnsCorrectConfig()
        {
            // Arrange
            var resourceId = "gold";

            // Act
            var config = ResourceCollector.Instance.GetResourceConfigById(resourceId);

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(resourceId, config.id);
            Assert.AreEqual("gold", config.name);
            Assert.IsNotNull(config.iconSmall);
            Assert.IsNotNull(config.iconLarge);
        }
        
        [Test]
        public void GetResourceConfigById_WhenConfigNonExists()
        {
            // Arrange
            var resourceId = "non_exists";

            // Act
            var config = ResourceCollector.Instance.GetResourceConfigById(resourceId);

            // Assert
            Assert.IsNull(config);
        }

        private void OnResourceChangeHandler(string name, int amount, string data)
        {
            _onResourceChangeCalled = true;
            _changedResource = name;
            _changedAmount = amount;
        }
        
        // Helper class for testing
        [ResourceInfo("resource_custom_test")]
        private class CustomTestResource : AResource
        {
            public int CurrentAmount { get; private set; }
            protected override bool AddInternal(int amount, string data)
            {
                CurrentAmount += amount;
                return true;
            }

            protected override bool RemoveInternal(int amount, string data)
            {
                CurrentAmount -= amount;
                return true;
            }
            
            protected override int SetInternal(int value, string data)
            {
                int valueChanged = value - CurrentAmount;
                CurrentAmount = value;
                return valueChanged;
            }

            protected override int ResetInternal()
            {
                int before = CurrentAmount;
                CurrentAmount = 0;
                return before;
            }
            
            public override    object Get     => CurrentAmount;
        }
    }
} 
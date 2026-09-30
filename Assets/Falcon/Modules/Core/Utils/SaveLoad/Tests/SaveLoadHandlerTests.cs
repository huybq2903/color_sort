/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-04
     */

namespace Falcon.Modules.Core.SaveLoad.Tests
{
    using Falcon.Modules.Core.SaveLoad.Runtime;
    using NUnit.Framework;

    public class SaveLoadHandlerTests
    {
        private const string TestStringKey = "TestStringKey";
        private const string TestIntKey    = "TestIntKey";
        private const string TestBoolKey   = "TestBoolKey";
        private const string TestFloatKey  = "TestFloatKey";
        private const string TestObjectKey = "TestObjectKey";

        [System.Serializable]
        private class TestObject
        {
            public int    id;
            public string value;
            public bool   isActive;

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                {
                    return false;
                }

                TestObject other = (TestObject)obj;
                return id == other.id && value == other.value && isActive == other.isActive;
            }

            public override int GetHashCode() { return id.GetHashCode() ^ value.GetHashCode() ^ isActive.GetHashCode(); }
        }

        [SetUp]
        public void Setup()
        {
            // Clean up keys before each test
            SaveLoadHandler.DeleteKey(TestStringKey);
            SaveLoadHandler.DeleteKey(TestIntKey);
            SaveLoadHandler.DeleteKey(TestBoolKey);
            SaveLoadHandler.DeleteKey(TestFloatKey);
            SaveLoadHandler.DeleteKey(TestObjectKey);
            SaveLoadHandler.DeleteKey(SaveLoadHandler.GenerateKeyBy("TestObjectForGenerateKey"));
            SaveLoadHandler.DeleteKey(SaveLoadHandler.GenerateKeyBy(123));
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up keys after each test
            SaveLoadHandler.DeleteKey(TestStringKey);
            SaveLoadHandler.DeleteKey(TestIntKey);
            SaveLoadHandler.DeleteKey(TestBoolKey);
            SaveLoadHandler.DeleteKey(TestFloatKey);
            SaveLoadHandler.DeleteKey(TestObjectKey);
            SaveLoadHandler.DeleteKey(SaveLoadHandler.GenerateKeyBy("TestObjectForGenerateKey"));
            SaveLoadHandler.DeleteKey(SaveLoadHandler.GenerateKeyBy(123));
        }

        [Test]
        public void SaveAndLoadString_ShouldReturnSavedValue()
        {
            string valueToSave = "Hello, Falcon!";
            SaveLoadHandler.Save(TestStringKey, valueToSave);
            string loadedValue = SaveLoadHandler.Load<string>(TestStringKey);
            Assert.AreEqual(valueToSave, loadedValue);
        }

        [Test]
        public void SaveAndLoadInt_ShouldReturnSavedValue()
        {
            int valueToSave = 12345;
            SaveLoadHandler.Save(TestIntKey, valueToSave);
            int loadedValue = SaveLoadHandler.Load<int>(TestIntKey);
            Assert.AreEqual(valueToSave, loadedValue);
        }

        [Test]
        public void SaveAndLoadBool_ShouldReturnSavedValue()
        {
            bool valueToSave = true;
            SaveLoadHandler.Save(TestBoolKey, valueToSave);
            bool loadedValue = SaveLoadHandler.Load<bool>(TestBoolKey);
            Assert.AreEqual(valueToSave, loadedValue);
        }

        [Test]
        public void SaveAndLoadFloat_ShouldReturnSavedValue()
        {
            float valueToSave = 123.45f;
            SaveLoadHandler.Save(TestFloatKey, valueToSave);
            float loadedValue = SaveLoadHandler.Load<float>(TestFloatKey);
            Assert.AreEqual(valueToSave, loadedValue);
        }

        [Test]
        public void SaveAndLoadObject_ShouldReturnSavedObject()
        {
            TestObject valueToSave = new TestObject { id = 1, value = "TestData", isActive = true };
            SaveLoadHandler.Save(TestObjectKey, valueToSave);
            TestObject loadedValue = SaveLoadHandler.Load<TestObject>(TestObjectKey);
            Assert.AreEqual(valueToSave, loadedValue);
        }

        [Test]
        public void LoadNonExistentKey_ShouldReturnDefaultValue()
        {
            string defaultValue = "Default";
            string loadedValue  = SaveLoadHandler.Load<string>("NonExistentStringKey", defaultValue);
            Assert.AreEqual(defaultValue, loadedValue);

            int defaultIntValue = 99;
            int loadedIntValue  = SaveLoadHandler.Load<int>("NonExistentIntKey", defaultIntValue);
            Assert.AreEqual(defaultIntValue, loadedIntValue);

            TestObject defaultObjectValue = new TestObject { id = 0, value = "DefaultObj", isActive = false };
            TestObject loadedObjectValue  = SaveLoadHandler.Load<TestObject>("NonExistentObjectKey", defaultObjectValue);
            Assert.AreEqual(defaultObjectValue, loadedObjectValue);

            TestObject loadedNullObject = SaveLoadHandler.Load<TestObject>("NonExistentObjectKeyNullDefault");
            Assert.IsNull(loadedNullObject);
        }

        [Test]
        public void ExistsKey_ShouldReturnTrueForExistingKey()
        {
            string valueToSave = "Check Exists";
            SaveLoadHandler.Save(TestStringKey, valueToSave);
            Assert.IsTrue(SaveLoadHandler.ExistsKey(TestStringKey));
        }

        [Test]
        public void ExistsKey_ShouldReturnFalseForNonExistingKey() { Assert.IsFalse(SaveLoadHandler.ExistsKey("NonExistentKeyForCheck")); }

        [Test]
        public void DeleteKey_ShouldRemoveKey()
        {
            string valueToSave = "DataToDelete";
            SaveLoadHandler.Save(TestStringKey, valueToSave);
            Assert.IsTrue(SaveLoadHandler.ExistsKey(TestStringKey)); // Ensure it exists first
            SaveLoadHandler.DeleteKey(TestStringKey);
            Assert.IsFalse(SaveLoadHandler.ExistsKey(TestStringKey));
        }

        [Test]
        public void DeleteNonExistentKey_ShouldNotThrowError()
        {
            Assert.DoesNotThrow(() => SaveLoadHandler.DeleteKey("NonExistentKeyForDelete"));
            Assert.IsFalse(SaveLoadHandler.ExistsKey("NonExistentKeyForDelete"));
        }

        [Test]
        public void GenerateKeyBy_DifferentObjects_ShouldReturnDifferentKeys()
        {
            string testObject1 = "TestObject1";
            string testObject2 = "TestObject2";
            string key1        = SaveLoadHandler.GenerateKeyBy(testObject1);
            string key2        = SaveLoadHandler.GenerateKeyBy(testObject2);
            Assert.AreNotEqual(key1, key2);
        }

        [Test]
        public void SaveWithGeneratedKey_AndLoad_ShouldWork()
        {
            string data                = "SomeDataWithGeneratedKey";
            string objectToGenerateKey = "MyUniqueObject";
            string generatedKey        = SaveLoadHandler.GenerateKeyBy(objectToGenerateKey);

            SaveLoadHandler.Save(generatedKey, data);
            string loadedData = SaveLoadHandler.Load<string>(generatedKey);
            Assert.AreEqual(data, loadedData);

            // Clean up
            SaveLoadHandler.DeleteKey(generatedKey);
        }
    }
}
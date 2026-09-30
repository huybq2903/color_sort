using NUnit.Framework;

namespace Falcon.Modules.Core.GameData.Tests
{
    using Falcon.Modules.Core.GameData.Runtime;

    public class AResourceTests
    {
        [ResourceInfo("resource_test")]
        private class TestResource : Runtime.AResource
        {
            private int _value;

            protected override bool AddInternal(int amount, string data)
            {
                if (amount <= 0) return false;
                _value += amount;
                return true;
            }

            protected override bool RemoveInternal(int amount, string data)
            {
                if (amount <= 0 || _value < amount) return false;
                _value -= amount;
                return true;
            }
            
            protected override int SetInternal(int value, string data)
            {
                int valueChanged = value - _value;
                _value = value;
                return valueChanged;
            }

            protected override int ResetInternal()
            {
                int before = _value;
                _value = 0;
                return before;
            }

            public override object Get => _value;
        }

        [Test]
        public void OnChanged_Is_Called_When_Resource_Is_Added()
        {
            // Arrange
            var testResource = new TestResource();
            bool onChangedCalled = false;
            int changedAmount = 0;
            string changedData = null;

            testResource.OnChanged += (amount, data) =>
            {
                onChangedCalled = true;
                changedAmount = amount;
                changedData = data;
            };

            // Act
            testResource.Add(10, "test_add");

            // Assert
            Assert.IsTrue(onChangedCalled);
            Assert.AreEqual(10, changedAmount);
            Assert.AreEqual("test_add", changedData);
        }

        [Test]
        public void OnChanged_Is_Called_When_Resource_Is_Removed()
        {
            // Arrange
            var testResource = new TestResource();
            testResource.Add(20, null); // Initial amount

            bool onChangedCalled = false;
            int changedAmount = 0;
            string changedData = null;
            
            testResource.OnChanged += (amount, data) =>
            {
                onChangedCalled = true;
                changedAmount = amount;
                changedData = data;
            };

            // Act
            testResource.Remove(5, "test_remove");

            // Assert
            Assert.IsTrue(onChangedCalled);
            Assert.AreEqual(-5, changedAmount);
            Assert.AreEqual("test_remove", changedData);
        }
        
        [Test]
        public void OnChanged_Is_Called_When_Resource_Is_Reset()
        {
            // Arrange
            var testResource = new TestResource();
            testResource.Add(50, null);
            
            bool onChangedCalled = false;
            int changedAmount = 0;
            
            testResource.OnChanged += (amount, data) =>
            {
                onChangedCalled = true;
                changedAmount = amount;
            };

            // Act
            testResource.Reset();

            // Assert
            Assert.IsTrue(onChangedCalled);
            Assert.AreEqual(-50, changedAmount);
        }

        [Test]
        public void OnChanged_Is_Not_Called_When_Add_Fails()
        {
            // Arrange
            var testResource = new TestResource();
            bool onChangedCalled = false;
            testResource.OnChanged += (amount, data) => onChangedCalled = true;

            // Act
            testResource.Add(0, "test_fail");

            // Assert
            Assert.IsFalse(onChangedCalled);
        }

        [Test]
        public void OnChanged_Is_Not_Called_When_Remove_Fails()
        {
            // Arrange
            var testResource = new TestResource();
            testResource.Add(10, null);
            bool onChangedCalled = false;
            testResource.OnChanged += (amount, data) => onChangedCalled = true;

            // Act
            testResource.Remove(20, "test_fail");

            // Assert
            Assert.IsFalse(onChangedCalled);
        }

        [Test]
        public void Get_Property_Returns_Correct_Value()
        {
            // Arrange
            var testResource = new TestResource();
            testResource.Add(100, null);

            // Act
            var value = testResource.Get;

            // Assert
            Assert.AreEqual(100, value);
        }
    }
} 
using NUnit.Framework;
using UnityEngine;
using ARSandbox.Systems; 

namespace ARSandbox.Tests
{
    public class CalibrationLogicTests
    {
        private CalibrationLogic _logic;

        [SetUp]
        public void Setup()
        {
            _logic = new CalibrationLogic();
        }

        [Test]
        public void GetClosestHandle_ReturnsCorrectIndex()
        {
            // Arrange
            Vector2[] handles = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(0, 100),
                new Vector2(100, 100),
                new Vector2(100, 0)
            };
            Vector2 mousePos = new Vector2(5, 5); // Close to index 0

            // Act
            int index = _logic.GetClosestHandle(mousePos, handles, 50f);

            // Assert
            Assert.AreEqual(0, index);
        }

        [Test]
        public void GetClosestHandle_ReturnsNegativeOne_WhenFar()
        {
            // Arrange
            Vector2[] handles = new Vector2[] { new Vector2(0, 0) };
            Vector2 mousePos = new Vector2(100, 100);

            // Act
            int index = _logic.GetClosestHandle(mousePos, handles, 10f);

            // Assert
            Assert.AreEqual(-1, index);
        }

        [Test]
        public void CalculateUV_ClampsValues()
        {
            // Arrange
            Vector2 screenPos = new Vector2(2000, -50);
            float width = 1920;
            float height = 1080;

            // Act
            Vector2 uv = _logic.CalculateUV(screenPos, width, height);

            // Assert
            Assert.AreEqual(1.0f, uv.x);
            Assert.AreEqual(0.0f, uv.y);
        }

        [Test]
        public void ProcessVisualToData_InvertsY()
        {
            // Arrange
            Vector2 input = new Vector2(0.5f, 0.2f);

            // Act
            Vector2 result = _logic.ProcessVisualToData(input, invertY: true);

            // Assert
            Assert.AreEqual(0.5f, result.x);
            Assert.AreEqual(0.8f, result.y, 0.001f);
        }
    }
}

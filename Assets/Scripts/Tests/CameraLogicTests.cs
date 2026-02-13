using NUnit.Framework;
using UnityEngine;
using ARSandbox.Systems;
using ARSandbox.Core;

namespace ARSandbox.Tests
{
    public class CameraLogicTests
    {
        private CameraLogic _logic;

        [SetUp]
        public void Setup()
        {
            _logic = new CameraLogic();
        }

        [Test]
        public void CalculateCameraTransform_TopView_CorrectRotation()
        {
            // Act
            Pose pose = _logic.CalculateCameraTransform(CamView.Top, 10f, 0f);

            // Assert
            // Euler(90, 0, 180) -> Quaternion check
            // We can check if forward vector is pointing Down
            Vector3 forward = pose.rotation * Vector3.forward;
            // In Unity: Down is (0, -1, 0)
            // Initial (0,0,1).  Rot(90,0,180):
            // 90 X -> (0,-1,0). 180 Z -> (0, -1, 0)? No.
            // Let's verify against logic: Quaternion.Euler(90, 0, 180)
            
            Quaternion expected = Quaternion.Euler(90, 0, 180);
            
            // Quaternion equality is tricky, check angle
            float angle = Quaternion.Angle(pose.rotation, expected);
            Assert.Less(angle, 1.0f);
        }

        [Test]
        public void CalculateCameraTransform_ZoomOffset_AffectsHeight()
        {
            // Arrange
            float meshSize = 10f;
            float zoom = 5f; // Pull closer by 5 units

            // Act
            Pose pose = _logic.CalculateCameraTransform(CamView.Top, meshSize, zoom);

            // Assert
            // Effective Size = 10 - 5 = 5.
            // Pos Y should be 5.
            Assert.AreEqual(5f, pose.position.y, 0.01f);
        }

        [Test]
        public void GetNextView_CyclesCorrectly()
        {
            // Arrange
            CamView current = CamView.Top;

            // Act
            CamView next = _logic.GetNextView(current, 1);

            // Assert
            Assert.AreEqual(CamView.Perspective, next);
        }
    }
}

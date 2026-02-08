using NUnit.Framework;
using ARSandbox.Core;

namespace ARSandbox.Tests
{
    public class CameraStateLogicTests
    {
        [Test]
        public void TestCycleForward()
        {
            Assert.AreEqual(CamView.Perspective, CameraStateLogic.GetNextView(CamView.Top, 1));
            Assert.AreEqual(CamView.Side, CameraStateLogic.GetNextView(CamView.Perspective, 1));
            Assert.AreEqual(CamView.Top, CameraStateLogic.GetNextView(CamView.Side, 1));
        }

        [Test]
        public void TestCycleBackward()
        {
            Assert.AreEqual(CamView.Side, CameraStateLogic.GetNextView(CamView.Top, -1));
            Assert.AreEqual(CamView.Top, CameraStateLogic.GetNextView(CamView.Perspective, -1));
            Assert.AreEqual(CamView.Perspective, CameraStateLogic.GetNextView(CamView.Side, -1));
        }
    }
}

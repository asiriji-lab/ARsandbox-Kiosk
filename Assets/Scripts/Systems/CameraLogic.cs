using UnityEngine;
using ARSandbox.Core;

namespace ARSandbox.Systems
{
    
    public class CameraLogic
    {
        private const float MIN_MESH_SIZE = 10f;
        private const float MIN_EFFECTIVE_SIZE = 2f;
        private const float PERSPECTIVE_HEIGHT_RATIO = 0.8f;
        private const float PERSPECTIVE_DEPTH_RATIO = 0.8f;
        private const float SIDE_HEIGHT_RATIO = 0.2f;

        public CamView GetNextView(CamView current, int direction)
        {
            return CameraStateLogic.GetNextView(current, direction);
        }

        public Pose CalculateCameraTransform(CamView view, float meshSize, float zoomOffset)
        {
            if (meshSize < MIN_MESH_SIZE) meshSize = MIN_MESH_SIZE;

            float effectiveSize = meshSize - zoomOffset;
            effectiveSize = Mathf.Max(effectiveSize, MIN_EFFECTIVE_SIZE);

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            switch (view)
            {
                case CamView.Top:
                    position = new Vector3(0, effectiveSize, 0);
                    rotation = Quaternion.Euler(90, 0, 180);
                    break;
                case CamView.Perspective:
                    position = new Vector3(0, effectiveSize * PERSPECTIVE_HEIGHT_RATIO, -effectiveSize * PERSPECTIVE_DEPTH_RATIO);
                    rotation = Quaternion.LookRotation(Vector3.zero - position);
                    break;
                case CamView.Side:
                    position = new Vector3(0, effectiveSize * SIDE_HEIGHT_RATIO, -effectiveSize);
                    rotation = Quaternion.LookRotation(Vector3.zero - position);
                    break;
            }

            return new Pose(position, rotation);
        }
    }
}

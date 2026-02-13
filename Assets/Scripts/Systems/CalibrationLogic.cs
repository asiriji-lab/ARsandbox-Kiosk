using UnityEngine;

namespace ARSandbox.Systems
{
    public class CalibrationLogic
    {
        //  Find closest handle index
        public int GetClosestHandle(Vector2 mousePos, Vector2[] handlePositions, float threshold)
        {
            float closestDist = threshold;
            int index = -1;

            for (int i = 0; i < handlePositions.Length; i++)
            {
                float d = Vector2.Distance(mousePos, handlePositions[i]);
                if (d < closestDist)
                {
                    closestDist = d;
                    index = i;
                }
            }
            return index;
        }

        //  Convert Screen Position to UV (0-1)
        public Vector2 CalculateUV(Vector2 screenPos, float screenWidth, float screenHeight)
        {
            float u = Mathf.Clamp01(screenPos.x / screenWidth);
            float v = Mathf.Clamp01(screenPos.y / screenHeight);
            return new Vector2(u, v);
        }

        //  Invert Y for Keystone (if needed by projector/camera setup)
        public Vector2 ProcessVisualToData(Vector2 normalizedPos, bool invertY = true)
        {
            if (invertY)
                return new Vector2(normalizedPos.x, 1.0f - normalizedPos.y);
            return normalizedPos;
        }

        //  Retrieve Visual Position from Data (Invert back)
        public Vector2 ProcessDataToVisual(Vector2 dataPos, float screenWidth, float screenHeight, bool invertY = true)
        {
            float y = invertY ? (1.0f - dataPos.y) : dataPos.y;
            return new Vector2(dataPos.x * screenWidth, y * screenHeight);
        }
    }
}

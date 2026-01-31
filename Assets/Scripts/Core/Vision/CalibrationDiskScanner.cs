using UnityEngine;
using System.Collections.Generic;

namespace ARSandbox.Core.Vision
{
    /// <summary>
    /// Processes depth data to find the calibration disk based on geometric height differences.
    /// Assumption: The disk is significantly "higher" (closer to camera) than the surrounding sand.
    /// </summary>
    public class CalibrationDiskScanner
    {
        private const float DISK_HEIGHT_THRESHOLD_METERS = 0.005f; // 5mm
        private const float MIN_VALID_DEPTH = 0.1f; // 10cm, ignore near-plane noise

        /// <summary>
        /// Scans a Region of Interest (ROI) in the point cloud for a floating object (the disk).
        /// </summary>
        /// <param name="pointCloud">Flat array of 3D points (Camera Space) from Azure Kinect.</param>
        /// <param name="width">Width of the depth map (e.g., 640).</param>
        /// <param name="expectedUV">Normalized UV coordinates (0-1) of where the projector is pointing.</param>
        /// <returns>Centroid of the detected disk, or null if not found.</returns>
        public Vector3? ScanForDisk(Vector3[] pointCloud, int width, int height, Vector2 expectedUV)
        {
            if (pointCloud == null || pointCloud.Length == 0) return null;

            // 1. Define ROI in pixel coordinates
            int centerX = (int)(expectedUV.x * width);
            int centerY = (int)(expectedUV.y * height);
            
            // ROI Size: 40x40 pixels (approx 5-10cm depending on height)
            int roiHalfSize = 20;
            int startX = Mathf.Clamp(centerX - roiHalfSize, 0, width - 1);
            int endX = Mathf.Clamp(centerX + roiHalfSize, 0, width - 1);
            int startY = Mathf.Clamp(centerY - roiHalfSize, 0, height - 1);
            int endY = Mathf.Clamp(centerY + roiHalfSize, 0, height - 1);

            List<Vector3> sandSamples = new List<Vector3>();
            List<Vector3> diskSamples = new List<Vector3>();

            // 2. First Pass: Sample the Perimeter to find "Sea Level" (Sand Height)
            // We sample the top/bottom/left/right edges of the ROI box
            for (int x = startX; x <= endX; x++)
            {
                AddValidPoint(pointCloud, width, x, startY, sandSamples); // Top
                AddValidPoint(pointCloud, width, x, endY, sandSamples);   // Bottom
            }
            for (int y = startY; y <= endY; y++)
            {
                AddValidPoint(pointCloud, width, startX, y, sandSamples); // Left
                AddValidPoint(pointCloud, width, endX, y, sandSamples);   // Right
            }

            if (sandSamples.Count < 10) return null; // Not enough valid data to estimate background

            float averageSandZ = CalculateAverageZ(sandSamples);

            // 3. Second Pass: Scan Interior for "Floating" Pixels
            // Inner loops (startX + margin ... endX - margin)
            int margin = 5; 
            for (int y = startY + margin; y <= endY - margin; y++)
            {
                for (int x = startX + margin; x <= endX - margin; x++)
                {
                    int idx = y * width + x;
                    if (idx < 0 || idx >= pointCloud.Length) continue;

                    Vector3 pt = pointCloud[idx];
                    
                    // Kinect Z is positive forward. "Closer" means Smaller Z.
                    // So if pt.z < (averageSandZ - 5mm), it is floating above the sand.
                    if (pt.z > MIN_VALID_DEPTH && pt.z < (averageSandZ - DISK_HEIGHT_THRESHOLD_METERS))
                    {
                        diskSamples.Add(pt);
                    }
                }
            }

            // 4. Verification & Centroid
            // We need a decent number of pixels (e.g. > 20) to confirm it's a disk and not noise
            if (diskSamples.Count < 20) return null;

            return CalculateCentroid(diskSamples);
        }

        private void AddValidPoint(Vector3[] cloud, int w, int x, int y, List<Vector3> list)
        {
            int idx = y * w + x;
            if (idx >= 0 && idx < cloud.Length)
            {
                Vector3 pt = cloud[idx];
                if (pt.z > MIN_VALID_DEPTH) list.Add(pt);
            }
        }

        private float CalculateAverageZ(List<Vector3> points)
        {
            float sum = 0;
            foreach (var p in points) sum += p.z;
            return sum / points.Count;
        }

        private Vector3 CalculateCentroid(List<Vector3> points)
        {
            Vector3 sum = Vector3.zero;
            foreach (var p in points) sum += p;
            return sum / points.Count;
        }
    }
}

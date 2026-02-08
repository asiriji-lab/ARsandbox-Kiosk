using UnityEngine;
using System.IO;

namespace ARSandbox.Core
{
    /// <summary>
    /// Handles persistent storage of SandboxSettingsSO to JSON.
    /// Location: Application.persistentDataPath/sandbox_settings.json
    /// </summary>
    public static class SandboxSettingsManager
    {
        private static string SettingsPath => Path.Combine(Application.persistentDataPath, "sandbox_settings.json");

        [System.Serializable]
        private class SettingsData
        {
            // Depth Range
            public float MinDepth;
            public float MaxDepth;

            // Projection
            public float HeightScale;
            public float MeshSize;
            public SerializableMatrix4x4 ProjectionMatrix;
            public Vector2[] BoundaryPoints;

            public bool FlatMode; // Added for persistence

            // Visuals
            public float TintStrength;
            public float SandScale;
            public int ColorScheme; // Enum as int
            public bool UseDiscreteBands;
            public bool ShowWalls;

            // Water
            public float WaterLevel;
            public float WaterOpacity;

            // Filtering
            public float MinCutoff;
            public float Beta;
            public float HandThreshold;
            public int SpatialBlur;
        }

        public static void Save(SandboxSettingsSO settings)
        {
            if (settings == null)
            {
                Debug.LogWarning("[SettingsManager] Save called with null settings!");
                return;
            }

            var data = new SettingsData
            {
                MinDepth = settings.MinDepth,
                MaxDepth = settings.MaxDepth,
                HeightScale = settings.HeightScale,
                MeshSize = settings.MeshSize,
                ProjectionMatrix = new SerializableMatrix4x4(settings.ProjectionMatrix),
                BoundaryPoints = settings.BoundaryPoints,
                FlatMode = settings.FlatMode,
                TintStrength = settings.TintStrength,
                SandScale = settings.SandScale,
                ColorScheme = (int)settings.ColorScheme,
                UseDiscreteBands = settings.UseDiscreteBands,
                ShowWalls = settings.ShowWalls,
                WaterLevel = settings.WaterLevel,
                WaterOpacity = settings.WaterOpacity,
                MinCutoff = settings.MinCutoff,
                Beta = settings.Beta,
                HandThreshold = settings.HandThreshold,
                SpatialBlur = settings.SpatialBlur
            };

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SettingsPath, json);
                Debug.Log($"[SettingsManager] Saved to: {SettingsPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SettingsManager] Save failed: {e.Message}");
            }
        }

        public static void Load(SandboxSettingsSO settings)
        {
            if (settings == null)
            {
                Debug.LogWarning("[SettingsManager] Load called with null settings!");
                return;
            }

            if (!File.Exists(SettingsPath))
            {
                Debug.Log($"[SettingsManager] No saved settings found at {SettingsPath}. Using defaults.");
                return;
            }

            try
            {
                string json = File.ReadAllText(SettingsPath);
                var data = JsonUtility.FromJson<SettingsData>(json);

                // Apply loaded values
                // VALIDATION: Fix for "Flat Blue" / Zeroed Settings Bug
                if (data.HeightScale < 0.01f || data.MaxDepth < 100f)
                {
                    Debug.LogWarning("[SettingsManager] Invalid/Zeroed Settings Detected (Min/Max/Height). Resetting to defaults.");
                    settings.MinDepth = 500f;
                    settings.MaxDepth = 1500f;
                    settings.HeightScale = 5.0f;
                    settings.FlatMode = false; // Reset flat mode too
                }
                else
                {
                    settings.MinDepth = data.MinDepth;
                    settings.MaxDepth = data.MaxDepth;
                    settings.HeightScale = data.HeightScale;
                    settings.FlatMode = data.FlatMode;
                }

                // SAFETY: Ensure Min < Max (Standard Sensor Logic: Closer is Smaller)
                if (settings.MinDepth > settings.MaxDepth)
                {
                    Debug.LogWarning($"[SettingsManager] Inverted Depth Detected (Min:{settings.MinDepth} > Max:{settings.MaxDepth}). Swapping.");
                    float temp = settings.MinDepth;
                    settings.MinDepth = settings.MaxDepth;
                    settings.MaxDepth = temp;
                }
                
                settings.MeshSize = data.MeshSize;
                settings.ProjectionMatrix = data.ProjectionMatrix.ToMatrix();
                settings.ProjectionMatrix = data.ProjectionMatrix.ToMatrix();
                
                // Validate Boundary Points (Fix for "Invisible Plain" zero-width bug)
                bool invalidBoundary = false;
                if (data.BoundaryPoints == null || data.BoundaryPoints.Length != 4)
                {
                    invalidBoundary = true;
                }
                else
                {
                    // Check for Zero Area (Collinear points)
                    float width = Mathf.Abs(data.BoundaryPoints[2].x - data.BoundaryPoints[0].x);
                    float height = Mathf.Abs(data.BoundaryPoints[2].y - data.BoundaryPoints[0].y);
                    if (width < 0.01f || height < 0.01f) invalidBoundary = true;
                }

                if (invalidBoundary)
                {
                    Debug.LogWarning("[SettingsManager] Invalid/Zero-size Boundary Points detected. Resetting to defaults.");
                    // corrected order: BL, TL, TR, BR (Matches SandboxSettingsSO defaults)
                    settings.BoundaryPoints = new Vector2[] {
                        new Vector2(0, 0), 
                        new Vector2(0, 1),
                        new Vector2(1, 1), 
                        new Vector2(1, 0)
                    };
                }
                else
                {
                    settings.BoundaryPoints = data.BoundaryPoints;
                }

                settings.TintStrength = data.TintStrength;
                settings.SandScale = data.SandScale;
                settings.ColorScheme = (GradientPreset)data.ColorScheme;
                settings.UseDiscreteBands = data.UseDiscreteBands;
                settings.ShowWalls = data.ShowWalls;
                settings.WaterLevel = data.WaterLevel;
                settings.WaterOpacity = data.WaterOpacity;
                settings.MinCutoff = data.MinCutoff;
                settings.Beta = data.Beta;
                settings.HandThreshold = data.HandThreshold;
                settings.SpatialBlur = data.SpatialBlur;

                Debug.Log($"[SettingsManager] Loaded from: {SettingsPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SettingsManager] Load failed: {e.Message}");
            }
        }
    }
}

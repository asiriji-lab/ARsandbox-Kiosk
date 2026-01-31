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
                settings.MinDepth = data.MinDepth;
                settings.MaxDepth = data.MaxDepth;
                settings.HeightScale = data.HeightScale;
                settings.MeshSize = data.MeshSize;
                settings.ProjectionMatrix = data.ProjectionMatrix.ToMatrix();
                if (data.BoundaryPoints != null && data.BoundaryPoints.Length == 4) settings.BoundaryPoints = data.BoundaryPoints;
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

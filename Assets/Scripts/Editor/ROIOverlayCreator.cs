using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using ARSandbox.UI;

public class ROIOverlayCreator 
{
    [MenuItem("AR Sandbox/Create ROI Overlay")]
    public static void CreateOverlay()
    {
        // 1. Find or Create Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 2. Create ROI Overlay Object
        GameObject roiObj = new GameObject("ROI_Overlay");
        roiObj.transform.SetParent(canvas.transform, false);

        // 3. Add RectTransform & Stretch
        RectTransform rt = roiObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; // Bottom-Left
        rt.anchorMax = Vector2.one;  // Top-Right
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 4. Add Components
        ROIEditorView view = roiObj.AddComponent<ROIEditorView>();
        RawImage rawImage = roiObj.AddComponent<RawImage>();
        
        // Settings for RawImage
        rawImage.color = new Color(1, 1, 1, 0.5f); // Semi-transparent
        rawImage.raycastTarget = true;

        // 5. Link References
        view.CameraPreviewImage = rawImage;
        
        // 6. Create Points Container
        GameObject pointsContainer = new GameObject("PointsContainer");
        pointsContainer.transform.SetParent(roiObj.transform, false);
        RectTransform pointsRT = pointsContainer.AddComponent<RectTransform>();
        pointsRT.anchorMin = Vector2.zero;
        pointsRT.anchorMax = Vector2.one;
        pointsRT.offsetMin = Vector2.zero;
        pointsRT.offsetMax = Vector2.zero;
        
        view.PointsContainer = pointsRT;

        // 7. Create Buttons (Optional, bottom right)
        CreateButton(roiObj.transform, "Confirm", new Vector2(-120, 50), view, true);
        CreateButton(roiObj.transform, "Clear", new Vector2(-240, 50), view, false);

        // 8. Disable by default
        roiObj.SetActive(false);

        Selection.activeGameObject = roiObj;
        Debug.Log("Created ROI_Overlay successfully!");
    }

    static void CreateButton(Transform parent, string name, Vector2 offset, ROIEditorView view, bool isConfirm)
    {
        GameObject btnObj = new GameObject(name + "Btn");
        btnObj.transform.SetParent(parent, false);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = Color.white;
        
        Button btn = btnObj.AddComponent<Button>();
        
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.one; // Bottom-Right relative (Wait, 1,0 is Bottom Right)
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(offset.x, 50); // Up from bottom
        rt.sizeDelta = new Vector2(100, 40);

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.text = name;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.black;
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // Link to Script
        if(isConfirm) view.ConfirmButton = btn;
        else view.ClearButton = btn;
    }
}

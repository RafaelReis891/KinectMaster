using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class JointUIBinding
{
    public string name;
    public KinectWrapper.NuiSkeletonPositionIndex joint;
    public RectTransform overlayRect;
}

public class KinectOverlayUI : MonoBehaviour
{
    public RawImage backgroundImage;

    [Header("Bindings")]
    public List<JointUIBinding> joints = new List<JointUIBinding>();

    [Header("Root Scale (fake distance)")]
    [Range(0.25f, 1f)]
    public float size = 1f;
    public RectTransform root; // objeto pai que será escalado

    public Text debugText;

    private RectTransform canvasRect;

    void Start()
    {
        if (root != null)
            canvasRect = root.parent as RectTransform;
    }

    void Update()
    {
        KinectManager manager = KinectManager.Instance;

        if (manager == null || !manager.IsInitialized())
            return;

        // textura Kinect
        if (backgroundImage && backgroundImage.texture == null)
            backgroundImage.texture = manager.GetUsersClrTex();

        if (!manager.IsUserDetected())
            return;

        uint userId = manager.GetPlayer1ID();

        // escala fake de distância
        if (root != null)
        {
            root.localScale = Vector3.one * size;
        }

        foreach (var binding in joints)
        {
            if (binding.overlayRect == null)
                continue;

            int jointIndex = (int)binding.joint;

            if (!manager.IsJointTracked(userId, jointIndex))
                continue;

            Vector3 posJoint = manager.GetRawSkeletonJointPos(userId, jointIndex);

            if (posJoint == Vector3.zero)
                continue;

            // Kinect → Depth → Color
            Vector2 posDepth = manager.GetDepthMapPosForJointPos(posJoint);
            Vector2 posColor = manager.GetColorMapPosForDepthPos(posDepth);

            float scaleX = posColor.x / KinectWrapper.Constants.ColorImageWidth;
            float scaleY = 1.0f - (posColor.y / KinectWrapper.Constants.ColorImageHeight);

            Vector2 screenPos = new Vector2(
                scaleX * Screen.width,
                scaleY * Screen.height
            );

            Vector2 localPoint;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                null,
                out localPoint
            );

            // resposta imediata (sem lerp)
            binding.overlayRect.anchoredPosition = localPoint;
        }

        if (debugText)
        {
            debugText.text = $"User: {userId}\nJoints ativos: {joints.Count}\nSize: {size:F2}";
        }
    }
}
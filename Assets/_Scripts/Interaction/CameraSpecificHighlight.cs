using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraSpecificHighlight :
    MonoBehaviour,
    ICameraHighlightable
{
    [Header("Visual")]
    [SerializeField] private Material highlightMaterial;

    [Range(1.001f, 1.1f)]
    [SerializeField] private float scaleMultiplier = 1.015f;

    private MeshFilter[] meshFilters;

    private readonly HashSet<Camera> highlightedCameras =
        new HashSet<Camera>();

    private void Awake()
    {
        meshFilters =
            GetComponentsInChildren<MeshFilter>(true);
    }

    public void SetHighlightForCamera(
        Camera camera,
        bool highlighted
    )
    {
        if (camera == null)
            return;

        if (highlighted)
        {
            highlightedCameras.Add(camera);
        }
        else
        {
            highlightedCameras.Remove(camera);
        }
    }

    private void LateUpdate()
    {
        if (highlightMaterial == null)
            return;

        if (highlightedCameras.Count == 0)
            return;

        foreach (Camera camera in highlightedCameras)
        {
            if (camera == null)
                continue;

            if (!camera.isActiveAndEnabled)
                continue;

            DrawHighlight(camera);
        }
    }

    private void DrawHighlight(Camera camera)
    {
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter == null)
                continue;

            Mesh mesh = meshFilter.sharedMesh;

            if (mesh == null)
                continue;

            MeshRenderer meshRenderer =
                meshFilter.GetComponent<MeshRenderer>();

            if (meshRenderer != null &&
                !meshRenderer.enabled)
            {
                continue;
            }

            Matrix4x4 matrix =
                meshFilter.transform.localToWorldMatrix *
                Matrix4x4.Scale(
                    Vector3.one * scaleMultiplier
                );

            RenderParams renderParams =
                new RenderParams(highlightMaterial);

            renderParams.camera = camera;
            renderParams.layer =
                meshFilter.gameObject.layer;

            renderParams.shadowCastingMode =
                ShadowCastingMode.Off;

            renderParams.receiveShadows = false;

            for (
                int subMesh = 0;
                subMesh < mesh.subMeshCount;
                subMesh++
            )
            {
            Graphics.RenderMesh(
                renderParams,
                mesh,
                subMesh,
                matrix
            );
            }
        }
    }

    private void OnDisable()
    {
        highlightedCameras.Clear();
    }
}
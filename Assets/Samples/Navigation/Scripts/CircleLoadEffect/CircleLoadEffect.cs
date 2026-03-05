using System.Collections.Generic;
using UnityEngine;

public class CircleLoadEffect : MonoBehaviour
{
    public Camera targetCamera;
    public Transform followTarget;    
    public GameObject spritePrefab;

    [Header("Layer")]
    [Tooltip("Use this component GameObject layer for container and spawned circles.")]
    [SerializeField] private bool _useOwnerLayer = false;
    [Tooltip("Layer name used when 'Use Owner Layer' is disabled.")]
    [SerializeField] private string _spawnLayerName = "LoaderFX";

    public int spriteCount = 24;
    public float spawnRadius = 5f;
    public Vector3 centerOffset = Vector3.zero;
    public float size = 1f;

    public bool randomizeY = false;
    public float minY = 0f;
    public float maxY = 0f;

    public float viewAngleThreshold = 15f;
    [Tooltip("Start angle offset (degrees) for the first spawned circle, relative to camera forward.")]
    public float startAngleDegrees = 30f;
    [Tooltip("Skip spawning circles in a frontal sector to avoid overlapping UI/onboarding modal.")]
    public bool skipForwardSector = false;
    [Tooltip("Half-angle of the forward sector (degrees) to skip.")]
    public float forwardSectorAngle = 35f;

    Transform container;               
    readonly List<GameObject> spawned = new();
    int _spawnLayer = -1;

    public Transform Container => container;

    void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (followTarget == null) followTarget = targetCamera.transform;
        ResolveSpawnLayer();

        container = new GameObject("CircleContainer").transform;
        container.SetParent(null);
        SetLayerRecursively(container, _spawnLayer);
    }

    void LateUpdate()
    {
        if (container == null || followTarget == null)
        {
            return;
        }

        container.position = followTarget.position + centerOffset;
        container.rotation = Quaternion.identity;     
    }

    public void SpawnCircles()
    {
        ClearCircles();

        if (spriteCount <= 0)
        {
            return;
        }

        float step = 360f / spriteCount;
        float forwardCosThreshold = Mathf.Cos(forwardSectorAngle * Mathf.Deg2Rad);
        Vector3 forwardFlat = GetFlatForward();
        Vector3 baseDirection = Quaternion.AngleAxis(startAngleDegrees, Vector3.up) * forwardFlat;

        for (int i = 0; i < spriteCount; i++)
        {
            Vector3 dir = Quaternion.AngleAxis(step * i, Vector3.up) * baseDirection;

            if (skipForwardSector && Vector3.Dot(dir, forwardFlat) >= forwardCosThreshold)
                continue;

            Vector3 pos = container.position + dir * spawnRadius;

            if (randomizeY) pos.y = Random.Range(minY, maxY);

            var go = Instantiate(spritePrefab, pos, Quaternion.identity, container);
            go.transform.localScale = Vector3.one * size;
            SetLayerRecursively(go.transform, _spawnLayer);

            var trig = go.AddComponent<SpriteAnimationTrigger>();
            trig.targetCamera = targetCamera;
            trig.viewAngleThreshold = viewAngleThreshold;

            spawned.Add(go);
        }
    }

    public void ClearCircles()
    {
        foreach (var g in spawned) if (g) Destroy(g);
        spawned.Clear();
    }

    private void ResolveSpawnLayer()
    {
        if (_useOwnerLayer)
        {
            _spawnLayer = gameObject.layer;
            return;
        }

        _spawnLayer = LayerMask.NameToLayer(_spawnLayerName);
        if (_spawnLayer >= 0)
        {
            return;
        }

        _spawnLayer = gameObject.layer;
        Debug.LogWarning($"{nameof(CircleLoadEffect)}: Layer '{_spawnLayerName}' not found. Fallback to owner layer '{LayerMask.LayerToName(_spawnLayer)}'.");
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null || layer < 0)
        {
            return;
        }

        root.gameObject.layer = layer;
        for (var i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }

    private Vector3 GetFlatForward()
    {
        if (followTarget == null)
        {
            return Vector3.forward;
        }

        var forwardFlat = Vector3.ProjectOnPlane(followTarget.forward, Vector3.up);
        if (forwardFlat.sqrMagnitude < 0.0001f)
        {
            return Vector3.forward;
        }

        return forwardFlat.normalized;
    }
}

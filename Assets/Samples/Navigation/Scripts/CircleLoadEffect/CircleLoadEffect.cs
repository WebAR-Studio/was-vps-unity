using System.Collections.Generic;
using UnityEngine;

public class CircleLoadEffect : MonoBehaviour
{
    public Camera targetCamera;
    public Transform followTarget;    
    public GameObject spritePrefab;

    public int spriteCount = 24;
    public float spawnRadius = 5f;
    public Vector3 centerOffset = Vector3.zero;
    public float size = 1f;

    public bool randomizeY = false;
    public float minY = 0f;
    public float maxY = 0f;

    public float viewAngleThreshold = 15f;
    [Tooltip("Skip spawning circles in a frontal sector to avoid overlapping UI/onboarding modal.")]
    public bool skipForwardSector = true;
    [Tooltip("Half-angle of the forward sector (degrees) to skip.")]
    public float forwardSectorAngle = 35f;

    Transform container;               
    readonly List<GameObject> spawned = new();

    public Transform Container => container;

    void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (followTarget == null) followTarget = targetCamera.transform;

        container = new GameObject("CircleContainer").transform;
        container.SetParent(null);       
    }

    void LateUpdate()
    {
        container.position = followTarget.position + centerOffset;
        container.rotation = Quaternion.identity;     
    }

    public void SpawnCircles()
    {
        ClearCircles();
        float step = 360f / spriteCount;
        float forwardCosThreshold = Mathf.Cos(forwardSectorAngle * Mathf.Deg2Rad);
        Vector3 forwardFlat = followTarget == null ? Vector3.forward : Vector3.ProjectOnPlane(followTarget.forward, Vector3.up).normalized;

        for (int i = 0; i < spriteCount; i++)
        {
            float rad = Mathf.Deg2Rad * step * i;
            Vector3 dir = new(Mathf.Cos(rad), 0f, Mathf.Sin(rad));

            if (skipForwardSector && Vector3.Dot(dir, forwardFlat) >= forwardCosThreshold)
                continue;

            Vector3 pos = container.position + dir * spawnRadius;

            if (randomizeY) pos.y = Random.Range(minY, maxY);

            var go = Instantiate(spritePrefab, pos, Quaternion.identity, container);
            go.transform.localScale = Vector3.one * size;

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
}

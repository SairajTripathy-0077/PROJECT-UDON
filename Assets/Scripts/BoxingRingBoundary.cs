using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class BoxingRingBoundary : MonoBehaviour
{
    private const string BoundaryRootName = "Generated Boxing Ring Boundaries";
    private static BoxingRingBoundary active;

    [SerializeField] private Transform ringModel;
    [SerializeField, Min(0.01f)] private float wallThickness = 0.2f;
    [SerializeField, Min(0.1f)] private float wallHeight = 2.5f;
    [SerializeField, Min(0f)] private float playerPadding = 0.35f;

    private Bounds ringBounds;

    public static BoxingRingBoundary Active => active;

    private void Awake()
    {
        if (active != null && active != this)
        {
            enabled = false;
            return;
        }

        active = this;
        BuildBoundary();
    }

    private void OnDestroy()
    {
        if (active == this)
            active = null;
    }

    public bool TryClampPosition(ref Vector3 position)
    {
        if (!TryGetBounds(out Bounds bounds))
            return false;

        position.x = Mathf.Clamp(
            position.x,
            bounds.min.x + playerPadding,
            bounds.max.x - playerPadding);
        position.z = Mathf.Clamp(
            position.z,
            bounds.min.z + playerPadding,
            bounds.max.z - playerPadding);
        return true;
    }

    private void BuildBoundary()
    {
        if (ringModel == null)
            ringModel = FindRingModel();

        if (ringModel == null || !TryCalculateBounds(ringModel, out ringBounds))
        {
            Debug.LogWarning("BoxingRingBoundary could not find a Boxing/Ring model with renderers.", this);
            return;
        }

        Transform existing = transform.Find(BoundaryRootName);
        if (existing != null)
            Destroy(existing.gameObject);

        Transform boundaryRoot = new GameObject(BoundaryRootName).transform;
        boundaryRoot.SetParent(transform, false);
        CreateWall(boundaryRoot, "North", new Vector3(ringBounds.center.x, ringBounds.center.y, ringBounds.max.z),
            new Vector3(ringBounds.size.x, wallHeight, wallThickness));
        CreateWall(boundaryRoot, "South", new Vector3(ringBounds.center.x, ringBounds.center.y, ringBounds.min.z),
            new Vector3(ringBounds.size.x, wallHeight, wallThickness));
        CreateWall(boundaryRoot, "East", new Vector3(ringBounds.max.x, ringBounds.center.y, ringBounds.center.z),
            new Vector3(wallThickness, wallHeight, ringBounds.size.z));
        CreateWall(boundaryRoot, "West", new Vector3(ringBounds.min.x, ringBounds.center.y, ringBounds.center.z),
            new Vector3(wallThickness, wallHeight, ringBounds.size.z));
    }

    private void CreateWall(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(name, typeof(BoxCollider));
        wall.transform.SetParent(parent, true);
        wall.transform.position = position;
        BoxCollider collider = wall.GetComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = false;
    }

    private bool TryGetBounds(out Bounds bounds)
    {
        if (ringModel == null && active != null)
            ringModel = FindRingModel();

        if (ringModel != null && TryCalculateBounds(ringModel, out bounds))
        {
            ringBounds = bounds;
            return true;
        }

        bounds = default;
        return false;
    }

    private static bool TryCalculateBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return false;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds.size.x > 0.01f && bounds.size.z > 0.01f;
    }

    private static Transform FindRingModel()
    {
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            string name = root.name.ToLowerInvariant();
            if (name.Contains("boxing") || name == "ring" || name.Contains("boxing ring"))
                return root.transform;
        }

        return null;
    }
}

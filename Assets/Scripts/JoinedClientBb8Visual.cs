using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public sealed class JoinedClientBb8Visual : NetworkBehaviour
{
    private const string VisualRootName = "Joined Client BB8 Visual";

    [Header("Model Resource")]
    [Tooltip("Name of the model file inside Assets/Resources/ (without extension).")]
    [SerializeField] private string bb8ResourceName = "bb-8";

    [Header("Model Transform & Sizing")]
    [Tooltip("Offset relative to the player avatar root (X = right, Y = up, Z = forward).")]
    [SerializeField] private Vector3 modelOffset = Vector3.zero;

    [Tooltip("Rotation of the BB-8 model in Euler angles.")]
    [SerializeField] private Vector3 modelEulerAngles = new Vector3(-90f, 0f, 0f);

    [Tooltip("Total height of the BB-8 model in world units.")]
    [SerializeField, Min(0.1f)] private float targetHeight = 1.8f;

    [Tooltip("Additional scale multiplier.")]
    [SerializeField, Min(0.01f)] private float scaleMultiplier = 1.0f;

    [Header("Hand Spheres (Tracked Hands)")]
    [Tooltip("Whether to display glowing hand spheres tracking the VR controllers.")]
    [SerializeField] private bool showHandSpheres = true;

    [Tooltip("Size/diameter scale of the hand spheres.")]
    [SerializeField, Min(0.01f)] private float handSphereSize = 0.18f;

    [Tooltip("Color of the BB-8 hand spheres.")]
    [SerializeField] private Color handColor = new Color(0.95f, 0.35f, 0.04f);

    [SerializeField, Range(0f, 1f)] private float handMetallic = 0.15f;
    [SerializeField, Range(0f, 1f)] private float handSmoothness = 0.65f;

    [Header("Collider Tuning (Hitbox)")]
    [Tooltip("Whether to automatically adjust the avatar's hitbox collider for BB-8.")]
    [SerializeField] private bool overrideCollider = true;

    [Tooltip("Radius of the BB-8 collider (width/thickness of the hitbox).")]
    [SerializeField, Min(0.05f)] private float colliderRadius = 0.55f;

    [Tooltip("Total height of the BB-8 collider.")]
    [SerializeField, Min(0.1f)] private float colliderHeight = 1.2f;

    [Tooltip("Center offset of the collider relative to avatar root.")]
    [SerializeField] private Vector3 colliderCenter = new Vector3(0f, 0.6f, 0f);

    public float ColliderRadius
    {
        get => colliderRadius;
        set
        {
            colliderRadius = Mathf.Max(0.05f, value);
            ApplyColliderSettings();
        }
    }

    public float ColliderHeight
    {
        get => colliderHeight;
        set
        {
            colliderHeight = Mathf.Max(0.1f, value);
            ApplyColliderSettings();
        }
    }

    public Vector3 ColliderCenter
    {
        get => colliderCenter;
        set
        {
            colliderCenter = value;
            ApplyColliderSettings();
        }
    }

    [Header("VR First-Person Culling")]
    [Tooltip("Whether to cull the BB-8 model body from the local VR player's camera so it does not obstruct their view.")]
    [SerializeField] private bool cullForLocalVrPlayer = true;

    [Tooltip("Layer name used to cull local first-person visuals (default: 'Mirror', which is excluded from the VR camera culling mask).")]
    [SerializeField] private string culledLayerName = "Mirror";

    [Tooltip("If true, also sets local renderers to ShadowsOnly so the body casts shadows on the ring floor without rendering into the headset.")]
    [SerializeField] private bool castShadowsOnlyForLocalOwner = true;

    public bool CullForLocalVrPlayer
    {
        get => cullForLocalVrPlayer;
        set
        {
            cullForLocalVrPlayer = value;
            ApplyCullingSettings();
        }
    }

    private Transform visualRoot;
    private GameObject modelInstance;
    private Transform leftHandSphere;
    private Transform rightHandSphere;
    private Transform leftHandTarget;
    private Transform rightHandTarget;

    public bool IsJoinedClientAvatar =>
        NetworkManager.Singleton != null && OwnerClientId != NetworkManager.ServerClientId;

    public override void OnNetworkSpawn()
    {
        if (IsJoinedClientAvatar)
            CreateVisual();
    }

    private void CreateVisual()
    {
        if (modelInstance != null)
            return;

        HideMidasRenderers();

        if (visualRoot == null)
        {
            visualRoot = new GameObject(VisualRootName).transform;
            visualRoot.SetParent(transform, false);
        }

        GameObject bb8ModelPrefab = Resources.Load<GameObject>(bb8ResourceName);
        if (bb8ModelPrefab == null)
        {
            Debug.LogError($"JoinedClientBb8Visual could not load Resources/{bb8ResourceName}.glb.", this);
            return;
        }

        modelInstance = Instantiate(bb8ModelPrefab, visualRoot);
        modelInstance.name = "Imported BB-8 Model";

        ApplyTransformSettings();
        ApplyColliderSettings();
        ApplyCullingSettings();
        DisableModelColliders(modelInstance);

        if (showHandSpheres)
            CreateHandSpheres();
    }

    public void ApplyTransformSettings()
    {
        if (modelInstance == null) return;

        modelInstance.transform.localRotation = Quaternion.Euler(modelEulerAngles);

        Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            if (bounds.size.y > 0.001f)
            {
                float baseScale = (targetHeight / bounds.size.y) * scaleMultiplier;
                modelInstance.transform.localScale = Vector3.one * baseScale;

                Vector3 localMin = visualRoot.InverseTransformPoint(bounds.min);
                modelInstance.transform.localPosition = new Vector3(
                    modelOffset.x,
                    -localMin.y * baseScale + modelOffset.y,
                    modelOffset.z
                );
            }
        }

        ApplyColliderSettings();
        ApplyCullingSettings();
    }

    public void ApplyColliderSettings()
    {
        if (!overrideCollider) return;

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.radius = colliderRadius;
            capsule.height = colliderHeight;
            capsule.center = colliderCenter + new Vector3(modelOffset.x, 0f, modelOffset.z);
        }
    }

    public void ApplyCullingSettings()
    {
        if (modelInstance == null) return;

        bool isLocalOwner = IsOwner;
        int targetLayer;
        UnityEngine.Rendering.ShadowCastingMode shadowMode;

        if (isLocalOwner && cullForLocalVrPlayer)
        {
            targetLayer = LayerMask.NameToLayer(culledLayerName);
            if (targetLayer < 0) targetLayer = 7; // "Mirror" layer fallback

            EnsureCameraCullsLayer(targetLayer);
            shadowMode = castShadowsOnlyForLocalOwner
                ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
                : UnityEngine.Rendering.ShadowCastingMode.On;
        }
        else
        {
            targetLayer = LayerMask.NameToLayer("Player");
            if (targetLayer < 0) targetLayer = 0;
            shadowMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        SetLayerRecursively(modelInstance.transform, targetLayer);

        foreach (Renderer r in modelInstance.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = shadowMode;
        }
    }

    private static void EnsureCameraCullsLayer(int layer)
    {
        if (layer < 0 || layer > 31) return;

        int layerMaskToExclude = ~(1 << layer);

        if (Camera.main != null && Camera.main.targetTexture == null)
        {
            Camera.main.cullingMask &= layerMaskToExclude;
        }

        Unity.XR.CoreUtils.XROrigin xrOrigin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null && xrOrigin.Camera.targetTexture == null)
        {
            xrOrigin.Camera.cullingMask &= layerMaskToExclude;
        }

        foreach (Camera cam in Camera.allCameras)
        {
            if (cam != null && cam.targetTexture == null)
            {
                cam.cullingMask &= layerMaskToExclude;
            }
        }
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null) return;
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (modelInstance != null)
        {
            ApplyTransformSettings();
        }
        ApplyColliderSettings();
        ApplyCullingSettings();
    }
#endif

    private int cullingCheckFrame;

    private void LateUpdate()
    {
        if (IsJoinedClientAvatar && modelInstance == null)
            CreateVisual();

        if (modelInstance == null)
            return;

        modelInstance.SetActive(true);
        foreach (Renderer renderer in modelInstance.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = true;

        if (IsOwner && cullForLocalVrPlayer && Time.frameCount - cullingCheckFrame > 60)
        {
            cullingCheckFrame = Time.frameCount;
            int targetLayer = LayerMask.NameToLayer(culledLayerName);
            if (targetLayer < 0) targetLayer = 7;
            EnsureCameraCullsLayer(targetLayer);
        }

        if (showHandSpheres)
        {
            UpdateHandSphere(leftHandSphere, leftHandTarget);
            UpdateHandSphere(rightHandSphere, rightHandTarget);
        }
    }

    private void HideMidasRenderers()
    {
        Transform midas = transform.Find("Midans mdol");
        if (midas == null)
            return;

        foreach (Renderer renderer in midas.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
    }

    private static void DisableModelColliders(GameObject model)
    {
        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
            Object.Destroy(collider);
    }

    private void CreateHandSpheres()
    {
        leftHandTarget = FindChildNamed(transform, "Left Hand");
        rightHandTarget = FindChildNamed(transform, "Right Hand");

        Material material = CreateHandMaterial();
        leftHandSphere = CreateHandSphere("BB8 Left Hand", material);
        rightHandSphere = CreateHandSphere("BB8 Right Hand", material);
    }

    private Transform CreateHandSphere(string name, Material material)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(visualRoot, false);
        sphere.transform.localScale = Vector3.one * handSphereSize;
        sphere.GetComponent<Renderer>().sharedMaterial = material;

        int visibleLayer = LayerMask.NameToLayer("Player");
        if (visibleLayer < 0) visibleLayer = 0;
        sphere.layer = visibleLayer;

        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        return sphere.transform;
    }

    private static void UpdateHandSphere(Transform sphere, Transform target)
    {
        if (sphere == null)
            return;

        sphere.gameObject.SetActive(target != null);
        if (target != null)
            sphere.SetPositionAndRotation(target.position, target.rotation);
    }

    private Material CreateHandMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = "BB8 Hand Sphere Material",
            color = handColor
        };

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", handMetallic);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", handSmoothness);

        return material;
    }

    private static Transform FindChildNamed(Transform root, string childName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }
}

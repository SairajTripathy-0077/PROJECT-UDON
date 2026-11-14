using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public sealed class JoinedClientBb8Visual : NetworkBehaviour
{
    private const string Bb8ResourceName = "bb-8";

    private const string VisualRootName = "Joined Client BB8 Visual";
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

        GameObject bb8ModelPrefab = Resources.Load<GameObject>(Bb8ResourceName);
        if (bb8ModelPrefab == null)
        {
            Debug.LogError($"JoinedClientBb8Visual could not load Resources/{Bb8ResourceName}.glb.", this);
            return;
        }

        modelInstance = Instantiate(bb8ModelPrefab, visualRoot);
        modelInstance.name = "Imported BB-8 Model";
        modelInstance.transform.localPosition = new Vector3(0f, 0f, 2.2f );
        modelInstance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        modelInstance.transform.localScale = Vector3.one;

        FitModelToAvatar(modelInstance.transform);
        DisableModelColliders(modelInstance);
        CreateHandSpheres();
    }

    private void LateUpdate()
    {
        if (IsJoinedClientAvatar && modelInstance == null)
            CreateVisual();

        if (modelInstance == null)
            return;

        modelInstance.SetActive(true);
        foreach (Renderer renderer in modelInstance.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = true;

        UpdateHandSphere(leftHandSphere, leftHandTarget);
        UpdateHandSphere(rightHandSphere, rightHandTarget);
    }

    private void HideMidasRenderers()
    {
        Transform midas = transform.Find("Midans mdol");
        if (midas == null)
            return;

        foreach (Renderer renderer in midas.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
    }

    private static void FitModelToAvatar(Transform model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        if (bounds.size.y <= 0.001f)
            return;

        float scale = 1.8f / bounds.size.y;
        model.localScale = Vector3.one * scale;
        Vector3 localMin = model.parent.InverseTransformPoint(bounds.min);
        model.localPosition = new Vector3(0f, -localMin.y * scale, 0f);
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
        sphere.transform.localScale = Vector3.one * 0.18f;
        sphere.GetComponent<Renderer>().sharedMaterial = material;

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

    private static Material CreateHandMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = "BB8 Hand Sphere Material",
            color = new Color(0.95f, 0.35f, 0.04f)
        };

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0.15f);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.65f);

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

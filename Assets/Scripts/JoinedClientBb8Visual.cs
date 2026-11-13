using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public sealed class JoinedClientBb8Visual : NetworkBehaviour
{
    private const string VisualRootName = "Joined Client BB8 Visual";
    private Transform visualRoot;
    private Transform headPose;
    private Transform headVisual;

    public bool IsJoinedClientAvatar =>
        NetworkManager.Singleton != null && OwnerClientId != NetworkManager.ServerClientId;

    public override void OnNetworkSpawn()
    {
        if (!IsJoinedClientAvatar)
            return;

        CreateVisual();
    }

    private void LateUpdate()
    {
        if (headVisual == null || headPose == null)
            return;

        headVisual.SetPositionAndRotation(
            headPose.position - headPose.up * 0.14f,
            headPose.rotation);
    }

    private void CreateVisual()
    {
        if (visualRoot != null)
            return;

        Transform existingRoot = transform.Find(VisualRootName);
        if (existingRoot != null)
        {
            visualRoot = existingRoot;
            headVisual = visualRoot.Find("BB8 Head");
            headPose = FindChildNamed(transform, "Head");
            return;
        }

        HideMidasRenderers();

        visualRoot = new GameObject(VisualRootName).transform;
        visualRoot.SetParent(transform, false);

        Material white = CreateMaterial(new Color(0.92f, 0.92f, 0.86f), 0.05f, 0.72f);
        Material orange = CreateMaterial(new Color(0.95f, 0.33f, 0.05f), 0.05f, 0.65f);
        Material black = CreateMaterial(new Color(0.015f, 0.015f, 0.012f), 0.1f, 0.55f);
        Material metal = CreateMaterial(new Color(0.34f, 0.36f, 0.38f), 0.75f, 0.8f);

        GameObject body = CreatePrimitive("BB8 Body", PrimitiveType.Sphere, visualRoot, white);
        body.transform.localPosition = new Vector3(0f, 0.78f, 0f);
        body.transform.localScale = Vector3.one * 0.72f;

        CreatePanel("BB8 Front Panel", new Vector3(0f, 0.82f, 0.57f),
            Quaternion.Euler(90f, 0f, 0f), new Vector3(0.24f, 0.045f, 0.24f), orange);
        CreatePanel("BB8 Side Panel", new Vector3(0.57f, 0.82f, 0f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(0.24f, 0.045f, 0.24f), orange);

        GameObject neck = CreatePrimitive("BB8 Neck", PrimitiveType.Cylinder, visualRoot, metal);
        neck.transform.localPosition = new Vector3(0f, 1.18f, 0f);
        neck.transform.localScale = new Vector3(0.2f, 0.08f, 0.2f);

        headVisual = CreatePrimitive("BB8 Head", PrimitiveType.Sphere, visualRoot, white).transform;
        headVisual.localScale = new Vector3(0.58f, 0.35f, 0.58f);

        GameObject eye = CreatePrimitive("BB8 Eye", PrimitiveType.Sphere, headVisual, black);
        eye.transform.localPosition = new Vector3(0f, 0.02f, 0.93f);
        eye.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);

        GameObject antenna = CreatePrimitive("BB8 Antenna", PrimitiveType.Cylinder, headVisual, metal);
        antenna.transform.localPosition = new Vector3(0.18f, 0.82f, 0f);
        antenna.transform.localScale = new Vector3(0.035f, 0.22f, 0.035f);

        GameObject antennaTip = CreatePrimitive("BB8 Antenna Tip", PrimitiveType.Sphere, headVisual, orange);
        antennaTip.transform.localPosition = new Vector3(0.18f, 1.27f, 0f);
        antennaTip.transform.localScale = Vector3.one * 0.09f;

        headPose = FindChildNamed(transform, "Head");
        if (headPose == null)
            headVisual.localPosition = new Vector3(0f, 1.42f, 0f);
    }

    private void HideMidasRenderers()
    {
        Transform midas = transform.Find("Midans mdol");
        if (midas == null)
            return;

        foreach (Renderer renderer in midas.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
    }

    private void CreatePanel(
        string name,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Material material)
    {
        GameObject panel = CreatePrimitive(name, PrimitiveType.Cylinder, visualRoot, material);
        panel.transform.localPosition = position;
        panel.transform.localRotation = rotation;
        panel.transform.localScale = scale;
    }

    private static GameObject CreatePrimitive(
        string name,
        PrimitiveType type,
        Transform parent,
        Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = name;
        primitive.transform.SetParent(parent, false);
        primitive.GetComponent<Renderer>().sharedMaterial = material;

        Collider collider = primitive.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        return primitive;
    }

    private static Material CreateMaterial(Color color, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader)
        {
            color = color,
            name = "BB8 Runtime Material"
        };

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);

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

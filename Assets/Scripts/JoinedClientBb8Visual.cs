using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public sealed class JoinedClientBb8Visual : NetworkBehaviour
{
    [SerializeField] private GameObject bb8ModelPrefab;

    private const string VisualRootName = "Joined Client BB8 Visual";
    private Transform visualRoot;
    private GameObject modelInstance;

    public bool IsJoinedClientAvatar =>
        NetworkManager.Singleton != null && OwnerClientId != NetworkManager.ServerClientId;

    public override void OnNetworkSpawn()
    {
        if (IsJoinedClientAvatar)
            CreateVisual();
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

        if (bb8ModelPrefab == null)
        {
            Debug.LogError("JoinedClientBb8Visual requires an imported BB-8 model prefab.", this);
            return;
        }

        modelInstance = Instantiate(bb8ModelPrefab, visualRoot);
        modelInstance.name = "Imported BB-8 Model";
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        FitModelToAvatar(modelInstance.transform);
        DisableModelColliders(modelInstance);
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
}

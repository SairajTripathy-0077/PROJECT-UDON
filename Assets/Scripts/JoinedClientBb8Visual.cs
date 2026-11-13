using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public sealed class JoinedClientBb8Visual : NetworkBehaviour
{
    [SerializeField] private GameObject bb8ModelPrefab;

    private const string VisualRootName = "Joined Client BB8 Visual";
    private Transform visualRoot;

    public bool IsJoinedClientAvatar =>
        NetworkManager.Singleton != null && OwnerClientId != NetworkManager.ServerClientId;

    public override void OnNetworkSpawn()
    {
        if (IsJoinedClientAvatar)
            CreateVisual();
    }

    private void CreateVisual()
    {
        if (visualRoot != null)
            return;

        HideMidasRenderers();

        visualRoot = new GameObject(VisualRootName).transform;
        visualRoot.SetParent(transform, false);

        if (bb8ModelPrefab == null)
        {
            Debug.LogError("JoinedClientBb8Visual requires an imported BB-8 model prefab.", this);
            return;
        }

        GameObject model = Instantiate(bb8ModelPrefab, visualRoot);
        model.name = "Imported BB-8 Model";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        FitModelToAvatar(model.transform);
        DisableModelColliders(model);
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

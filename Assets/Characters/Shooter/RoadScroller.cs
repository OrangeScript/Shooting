
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class RoadScroller : MonoBehaviour
{
    [Header("道路滚动速度")]
    [SerializeField]
    private Vector2 scrollSpeed = new Vector2(0f, -0.5f);

    [Header("是否滚动")]
    [SerializeField]
    private bool isScrolling = true;

    private Material roadMaterial;
    private Vector2 offset;
    private int texturePropertyID;

    private void Awake()
    {
        Renderer roadRenderer = GetComponent<Renderer>();

        // 获取运行时独立材质，不修改原始材质资源
        roadMaterial = roadRenderer.material;

        // URP/Lit 使用 _BaseMap
        texturePropertyID = Shader.PropertyToID("_BaseMap");

        if (!roadMaterial.HasProperty(texturePropertyID))
        {
            Debug.LogError("道路材质不支持 _BaseMap！");
            enabled = false;
            return;
        }

        offset = roadMaterial.GetTextureOffset(texturePropertyID);
    }

    private void Update()
    {
        if (!isScrolling)
            return;

        offset += scrollSpeed * Time.deltaTime;

        // 防止偏移量持续无限增大
        offset.x = Mathf.Repeat(offset.x, 1f);
        offset.y = Mathf.Repeat(offset.y, 1f);

        roadMaterial.SetTextureOffset(
            texturePropertyID,
            offset
        );
    }

    public void SetScrollSpeed(Vector2 speed)
    {
        scrollSpeed = speed;
    }

    public void SetScrolling(bool scrolling)
    {
        isScrolling = scrolling;
    }

    private void OnDestroy()
    {
        if (roadMaterial != null)
            Destroy(roadMaterial);
    }
}

using UnityEngine;
using UnityEngine.UI;

public class AutoScroll : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float scrollSpeed = 50f;
    public bool scrollRightToLeft = true;

    private RectTransform content;
    private RectTransform viewport;
    private HorizontalLayoutGroup layoutGroup;

    void Start()
    {
        if (!scrollRect) scrollRect = GetComponent<ScrollRect>();
        content = scrollRect.content;
        viewport = scrollRect.viewport;
        layoutGroup = content.GetComponent<HorizontalLayoutGroup>();

        scrollRect.horizontal = false;
    }

    void Update()
    {
        if (content.childCount < 2) return;

        float direction = scrollRightToLeft ? 1f : -1f;
        float moveX = scrollSpeed * Time.deltaTime * direction;
        content.anchoredPosition += new Vector2(-moveX, 0);

        for (int i = 0; i < content.childCount; i++)
        {
            RectTransform child = content.GetChild(i) as RectTransform;

            if (scrollRightToLeft && IsFullyLeft(child))
            {
                float width = GetItemWidth(child);
                child.SetAsLastSibling();
                content.anchoredPosition += new Vector2(width, 0);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                break;
            }
            else if (!scrollRightToLeft && IsFullyRight(child))
            {
                float width = GetItemWidth(child);
                child.SetAsFirstSibling();
                content.anchoredPosition -= new Vector2(width, 0);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                break;
            }
        }
    }

    bool IsFullyLeft(RectTransform item)
    {
        Vector3[] corners = new Vector3[4];
        item.GetWorldCorners(corners);
        return corners[3].x < viewport.position.x;
    }

    bool IsFullyRight(RectTransform item)
    {
        Vector3[] corners = new Vector3[4];
        item.GetWorldCorners(corners);
        return corners[0].x > viewport.position.x + viewport.rect.width;
    }

    float GetItemWidth(RectTransform item)
    {
        float spacing = layoutGroup.spacing;
        return item.rect.width + spacing;
    }
}

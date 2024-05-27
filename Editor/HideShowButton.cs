using UnityEngine;
using UnityEngine.UI;
public class HideShowButton : MonoBehaviour
{
    // ��Ŀ �������� ĵ������ �پ� �־����.
    void Start()
    {
        RectTransform rectTrans = transform.parent.GetComponent<RectTransform>();
        Button myBtn = transform.GetComponent<Button>();
        myBtn.onClick.AddListener(() => HideShow(rectTrans));
    }
    void HideShow(RectTransform rectTrans)
    {
        float width = rectTrans.rect.width; float height = rectTrans.rect.height;
        Vector2 min = rectTrans.anchorMin; Vector2 max = rectTrans.anchorMax;

        if (min == new Vector2(1, 0) && max == new Vector2(1, 1))//���� streach
        {
            if (rectTrans.anchoredPosition.x > 1f) rectTrans.anchoredPosition = new Vector2(0, 0);
            else rectTrans.anchoredPosition = new Vector2(width, 0);
        }
        else if (min == new Vector2(0, 0) && max == new Vector2(0, 1))//���� streach
        {
            if (rectTrans.anchoredPosition.x < -1f) rectTrans.anchoredPosition = new Vector2(0, 0);
            else rectTrans.anchoredPosition = new Vector2((-1) * width, 0);
        }
        else if (min == new Vector2(0, 0) && max == new Vector2(1, 0))//�ϴ� streach
        {
            if (rectTrans.anchoredPosition.y < -1f) rectTrans.anchoredPosition = new Vector2(0, 0);
            else rectTrans.anchoredPosition = new Vector2(0, (-1) * height);
        }
        else if (min == new Vector2(0, 1) && max == new Vector2(1, 1))//��� streach
        {
            if (rectTrans.anchoredPosition.y > 1f) rectTrans.anchoredPosition = new Vector2(0, 0);
            else rectTrans.anchoredPosition = new Vector2(0, height);
        }
        transform.eulerAngles += new Vector3(0, 0, 180f);//FlipImage
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Widget_ChatBox : MonoBehaviour
{
    public   Vector4  textContentSize;
    private  Image imgChatBox;
    private  Text  textContent;
    private Vector2 defaultSize;
	// Use this for initialization
	void Awake ()
    {
        Init();
        ResizeBoxSize();
	}
    public void Init()
    {
        imgChatBox = this.GetComponent<Image>();
        textContent = this.transform.GetChild(0).GetComponent<Text>();
        defaultSize = imgChatBox.rectTransform.sizeDelta;
    }

    public void ResizeBoxSize()
    {      
        //左上角位置,x - z
        textContent.rectTransform.anchoredPosition = new Vector2(textContentSize.x, textContentSize.z * -1);
        //左右宽
        var textWidth =  imgChatBox.rectTransform.rect.width - textContentSize.x - textContentSize.y;
        textContent.rectTransform.sizeDelta = new Vector2(textWidth, textContent.rectTransform.sizeDelta.y);

        //调整box高度
        var textHeight = textContent.rectTransform.sizeDelta.y;
        var boxDeltaSize  = imgChatBox.rectTransform.sizeDelta;
        //上下
        var minHeight  = textHeight + textContentSize.z + textContentSize.w;
        if (boxDeltaSize.y < minHeight)
        {
            imgChatBox.rectTransform.sizeDelta = new Vector2(boxDeltaSize.x, minHeight);
        }
        else
        {
            imgChatBox.rectTransform.sizeDelta = defaultSize;
        }
        
    }
     
    public void SetContent(string text)
    {
        textContent.text = text;
        // ResizeBoxSize();
        //需要在这帧结束改变 textbox 的大小
        IEnumeratorTool.StartCoroutine(IE_ChangeBoxSize());
    }

    IEnumerator IE_ChangeBoxSize()
    {
        yield return new WaitForEndOfFrame();
        ResizeBoxSize();
    }
}

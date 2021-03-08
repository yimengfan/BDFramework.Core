using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class StartPageTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
#if UNITY_EDITOR
       GameViewUtils.Set1080p();
#endif
        //
        var simple = this.transform.Find("Tween/text_simple");
        var easy = this.transform.Find("Tween/text_easy");
        var powerful = this.transform.Find("Tween/text_powerful");
        //

        (simple as RectTransform).DOAnchorPosY(-40, 0.5f).SetEase(Ease.Flash);
        (easy as RectTransform).DOAnchorPosY(-180, 0.5f).SetEase(Ease.Flash).SetDelay(1f);
        (powerful as RectTransform).DOAnchorPosY(-320, 0.6f).SetEase(Ease.Flash).SetDelay(1.5f);
        this.StartCoroutine(this.WattingForClose());
    }

    IEnumerator WattingForClose()
    {
        yield return new WaitForSeconds(3f);
        this.transform.gameObject.SetActive(false);
        
    }
}

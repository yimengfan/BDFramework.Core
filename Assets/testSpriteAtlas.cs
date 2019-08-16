using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class testSpriteAtlas : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        this.StartCoroutine(LoadTex());
    }


    IEnumerator LoadTex()
    {

        var path = "E:/BDFramework.Core/Assets/test/11111";
        var asset = AssetBundle.LoadFromFileAsync(path);
        yield return asset;

        if (asset.isDone && asset.assetBundle != null)
        {
            
          //  SpriteAtlasManager.atlasRequested
            
          //  var sas = asset.assetBundle.LoadAllAssets();
            var sa = asset.assetBundle.LoadAsset<SpriteAtlas>("New Sprite Atlas");

            var sp1 = sa.GetSprite("1");
            var sp2 = sa.GetSprite("2");

            this.transform.GetChild(0).GetComponent<Image>().overrideSprite = sp1;
            this.transform.GetChild(1).GetComponent<Image>().overrideSprite = sp2;
        }
    }
}

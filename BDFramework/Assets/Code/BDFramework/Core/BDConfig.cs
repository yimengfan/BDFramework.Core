using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BDFramework
{
    
    public class BDConfig : MonoBehaviour
    {

        //CND访问的url
        public string CDNConfigUrl = "";
        //用来上传到cnd的账号密码
        public string UploadCDNAccount = "";
        public string UploadCDNPassword ="";
        // Start is called before the first frame update
        void Start()
        {
        
        }
    }
}

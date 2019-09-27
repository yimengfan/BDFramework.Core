using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace BDFramework.Http
{
    public class HttpClient : WebClient
    {
        public HttpClient() 
        {
        }


        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = WebRequest.Create(address);
            request.Timeout = 8000;
            return request;
        
        }
    }
}

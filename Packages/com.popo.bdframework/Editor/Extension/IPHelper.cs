using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

namespace BDFramework.Editor.Unity3dEx
{
    public class IPHelper
    {
        public enum ADDRESSFAM
        {
            IPv4,
            IPv6
        }

        /// <summary>
        /// 获取本机IP
        /// </summary>
        /// <param name="Addfam">要获取的IP类型</param>
        /// <returns></returns>
        //获取本地ip
        static public string GetLocalIP()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adater in adapters)
            {
                if (adater.Supports((NetworkInterfaceComponent.IPv4)))
                {
                    UnicastIPAddressInformationCollection uniCast = adater.GetIPProperties().UnicastAddresses;
                    if (uniCast.Count > 0)
                    {
                        foreach (UnicastIPAddressInformation uni in uniCast)
                        {
                            if (uni.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                var ip = uni.Address.ToString();
                                if (ip.StartsWith("192"))
                                {
                                    return ip;
                                }
                            }
                        }
                    }
                }
            }

            return "127.0.0.1";
        }
    }
}

using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace BDFramework.Core.Tools
{    
    public enum AddressFam
    {
        IPv4,
        IPv6
    }
    public class DebuggerNetworkTools
    {
        /// <summary>
        /// 获取IP
        /// </summary>
        /// <param name="Addfam"></param>
        /// <returns></returns>
        public static string GetIP(AddressFam Addfam)
        {
            //Return null if ADDRESSFAM is Ipv6 but Os does not support it
            if (Addfam == AddressFam.IPv6 && !Socket.OSSupportsIPv6)
            {
                return null;
            }

            string output = "";

            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                NetworkInterfaceType _type1 = NetworkInterfaceType.Wireless80211;
                NetworkInterfaceType _type2 = NetworkInterfaceType.Ethernet;

                if ((item.NetworkInterfaceType == _type1 || item.NetworkInterfaceType == _type2) &&
                    item.OperationalStatus == OperationalStatus.Up)
#endif
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        //IPv4
                        if (Addfam == AddressFam.IPv4)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                output = ip.Address.ToString();
                            }
                        }

                        //IPv6
                        else if (Addfam == AddressFam.IPv6)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                output = ip.Address.ToString();
                            }
                        }
                    }
                }
            }

            return output;
        }
        
        
        /// <summary>
        /// 二进制方式将object对象序列化到字节数组中
        /// </summary>
        /// <param   name= "obj "> </param>
        /// <returns> </returns>
        public static byte[] SerializeByBinary(object obj)
        {
            byte[] retBuffer;
            try
            {
                var memoryStream = new MemoryStream();
                var formatter1 = new BinaryFormatter();
                formatter1.Serialize(memoryStream, obj);
                byte[] buffer1 = memoryStream.ToArray();
                memoryStream.Close();
                retBuffer = buffer1;
            }
            catch
            {
                retBuffer = null;
            }
            return retBuffer;

        }
        
        /// <summary>
        /// 二进制方式字节数组中数据还原为对象
        /// </summary>
        /// <param   name= "obj "> </param>
        /// <returns> </returns>
        public static T DeSerializeByBinary<T>(byte[] bytes) where T :class
        {
            object retObj;
            try
            {
                var memoryStream = new MemoryStream(bytes);
                var formatter = new BinaryFormatter();
                object obj1 = formatter.Deserialize(memoryStream);
                memoryStream.Close();
                retObj = obj1;
            }
            catch
            {
                retObj = null;
            }
            return retObj as T;
        }
        
    }


}
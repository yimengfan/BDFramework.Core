using System.Collections.Generic;

//服务器返回json数据结构
public  class ServerResponsData
{
    public string http_code;
    public string name;
    public string version_latest;
    public string ver_res_latest;
    public string server_resource_online;
    public string server_resource_online_ip;
    public string server_resource_local_ip;
    public List<ResData> resource = new List<ResData>();
}
public class ResData
{
    public string version_res_current;
    public string version_res_next;
    public string path_resource;
    public string force_update;
    public string status;
}



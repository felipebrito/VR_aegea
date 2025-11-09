using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

[System.Serializable]
public class IPEntry
{
    public string ip;
}

[System.Serializable]
public class IPListWrapper
{
    public List<IPEntry> ips;
}

public class RemoteConfigFromSheet : MonoBehaviour
{
    public string ip;

    private string url = "https://opensheet.vercel.app/1zowPHxi4lshY42Xf0VpP7txQ7t3VyIx9C81HrQK2L4k/page1";


    void Start()
    {
        StartCoroutine(LoadIP());
    }

    IEnumerator LoadIP()
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.LogWarning("Erro ao buscar IP: " + www.error);
        }
        else
        {
            // JSON retornado é um array, então encapsulamos em um objeto
            string rawJson = www.downloadHandler.text;
            string wrappedJson = "{\"ips\":" + rawJson + "}";

            IPListWrapper data = JsonUtility.FromJson<IPListWrapper>(wrappedJson);

            if (data.ips != null && data.ips.Count > 0)
            {
                ip = data.ips[0].ip;
                UnityEngine.Debug.LogWarning("IP recebido: " + ip);
            }
            else
            {
                UnityEngine.Debug.LogWarning("Nenhum IP encontrado.");
            }
        }
    }
}

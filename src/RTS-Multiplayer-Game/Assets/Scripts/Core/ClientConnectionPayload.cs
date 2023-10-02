using System;
using System.Text;
using UnityEngine;

[Serializable]
public class ClientConnectionPayload
{
    public string username;
    public string playerName;
    public string serverPassword;

    public byte[] ToBytesArray()
    {   
        string payloadJson  = JsonUtility.ToJson(this);
        return Encoding.UTF8.GetBytes(payloadJson);
    }

    public static ClientConnectionPayload FromBytesArray(byte[] payloadBytes)
    {
        string payloadJson = Encoding.UTF8.GetString(payloadBytes);
        return JsonUtility.FromJson<ClientConnectionPayload>(payloadJson);
    }
}

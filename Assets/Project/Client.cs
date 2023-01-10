using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;

public class Client : MonoBehaviour
{
    private WebSocket m_WebSocket;
    public string fromunity = "1";
    public string fromweb = "2";
    //public static string fromrobot = "";
    static Dictionary<string, string> message = new Dictionary<string, string>() {
        {"event", "message" } , {"data", ""}
    };
    string json = JsonConvert.SerializeObject(message, Formatting.None);

    void Start()
    {
        //m_WebSocket = new WebSocket("ws://localhost:8000");
        m_WebSocket = new WebSocket("ws://ec2-13-209-7-136.ap-northeast-2.compute.amazonaws.com:8000");
        m_WebSocket.Connect();

        m_WebSocket.OnMessage += (sender, e) =>
        {
            fromweb = e.Data;
            Debug.Log($"{((WebSocket)sender).Url}에서 + 데이터 : {e.Data}가 옴.");
        };
    }

    void Update()
    {
        if (m_WebSocket == null)
        {
            return;
        }

        //메시지 갱신
        if (Input.GetKeyDown(KeyCode.Space))
        {
            message["data"] = fromunity;
            //Debug.Log("message:" + message["message"]);
            json = JsonConvert.SerializeObject(message, Formatting.None);
            Debug.Log(json);
            m_WebSocket.Send(
                json
            );
        }
    }
}
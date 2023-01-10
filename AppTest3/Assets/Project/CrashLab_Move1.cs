using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class CrashLab_Move1 : UnityPublisher<MessageTypes.My.SensorData>
    {
        //name data index
        //키를 눌렀을때 수직, 수평 이동을 각각 123 456에 대응.
        public string FrameId = "Unity";
        public int input_ad; //수평, 좌우
        public int input_ws; //수직, 앞뒤

        private MessageTypes.My.SensorData message;

        protected override void Start()
        {
            base.Start();
            InitializeMessage();
        }

        private void FixedUpdate()
        {
            UpdateMessage();
        }

        private void InitializeMessage()
        {
            message = new MessageTypes.My.SensorData { };
            input_ad = 0;
            input_ws = 0;
        }
 
        private void UpdateMessage()
        {
            message.name = "robot dir";
            //앞뒤
            if (Input.GetKey(KeyCode.W)) input_ws = 0;
            else if (Input.GetKey(KeyCode.S)) input_ws = 1;
            else input_ws = 2;
            //좌우
            if (Input.GetKey(KeyCode.A)) input_ad = 0;
            else if (Input.GetKey(KeyCode.D)) input_ad = 1;
            else input_ad = 2;

            /*
            3*3으로 총 9개. 
            직좌(0) 직우(1) 직가(2)
            후좌(3) 후우(4) 후가(5)
            정좌(6) 정우(7) 정가(8)
            */
            message.data = input_ws * 3 + input_ad;

            //9,10
            if (Input.GetKey(KeyCode.Q)) message.data = 9;
            else if (Input.GetKey(KeyCode.E)) message.data = 10;

            //속도 변속
            else if (Input.GetKey(KeyCode.T)) message.data = 11;
            else if (Input.GetKey(KeyCode.Y)) message.data = 12;

            else if (Input.GetKey(KeyCode.U)) message.data = 13;
            else if (Input.GetKey(KeyCode.I)) message.data = 14;

            else if (Input.GetKey(KeyCode.O)) message.data = 15;
            else if (Input.GetKey(KeyCode.P)) message.data = 16;

            //Debug.Log("message.name: "+ message.name + " data:" + message.data);
            Publish(message);
        }

    }
}


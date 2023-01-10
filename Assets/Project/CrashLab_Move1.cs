using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace RosSharp.RosBridgeClient
{
    public class CrashLab_Move1 : UnityPublisher<MessageTypes.My.SensorData>
    {
        //라이다쪽 스크립트
        LaserScanWriter lsw_code;
        Client client_code;

        //name data index
        //키를 눌렀을때 수직, 수평 이동을 각각 123 456에 대응.
        public string FrameId = "Unity";
        public int input_ad; //수평, 좌우
        public int input_ws; //수직, 앞뒤
        public bool isauto;
        public bool isReceived;
        public bool isObstacle;
        public bool isFast;
        public string nextPlace; // 다음 위치의 랜드마크
        public string nowPlace; //현재 위치의 랜드마크
        public string goalPlace; //목표 위치의 랜드마크
        public int moveDir; //이동 방향
        public int nowState; //현재 상태

        public float xx;

        public float[] ydLidar; //0~15
        public float[] frontLidar; // 0~4 총 다섯개
        public GameObject[] spots; //랜드마크들

        public float robot_y;

        private MessageTypes.My.SensorData message;

        public float threshold_1, threshold_2, threshold_3, threshold_4, threshold_5, threshold_6_WallNav; //정지임계치, 직진 임계치

        //UI
        public TMP_Text FromWeb;
        public TMP_Text ToWeb;
        public TMP_Text Where;
        public TMP_Text isAuto_text;
        public TMP_Text isReceived_text;

        protected override void Start()
        {
            base.Start();
            isauto = false;
            isReceived = false;
            InitializeMessage();
            goalPlace = "112";
            nowState = 0;
        }

        void Update()
        {
            //수동, 자동 변경
            if (Input.GetKeyDown(KeyCode.C) && isauto == true) //수동으로 변경
            {
                isauto = false;
            }
            else if (Input.GetKeyDown(KeyCode.C) && isauto == false) //자동으로 변경
            {
                isauto = true;
            }

            Get_lidar();

            //UpdateMessage(); // 시연전
            if (isauto == true)
            {
                TestingManager();
            }
            else
            {
                PlayerControll();
            }

            //유니티 재시작
            if (Input.GetKeyDown(KeyCode.Z))
            {
                SceneManager.LoadScene(0);
            }

            //유니티 종료
            if (Input.GetKeyDown(KeyCode.X))
            {
                Application.Quit();
            }

            robot_y = this.transform.rotation.eulerAngles.y;


            //UI
            UIManager();

            Find_Xx();
        }

        void InitializeMessage()
        {
            message = new MessageTypes.My.SensorData { };
            input_ad = 0;
            input_ws = 0;
        }
        
        void PlayerControll()
        {
            //앞뒤
            if (Input.GetKey(KeyCode.W)) input_ws = 0;
            else if (Input.GetKey(KeyCode.S)) input_ws = 1;
            else input_ws = 2;
            //좌우
            if (Input.GetKey(KeyCode.A)) input_ad = 0;
            else if (Input.GetKey(KeyCode.D)) input_ad = 1;
            else input_ad = 2;

            //=========변경
            message.data = input_ws * 3 + input_ad;

            //속도 변속
            if (Input.GetKey(KeyCode.T)) message.data = 11;
            else if (Input.GetKey(KeyCode.Y)) message.data = 12;
            else if (Input.GetKey(KeyCode.U)) message.data = 13;
            else if (Input.GetKey(KeyCode.I)) message.data = 14;
            else if (Input.GetKey(KeyCode.O)) message.data = 15;
            else if (Input.GetKey(KeyCode.P)) message.data = 16;

            // 박스 밀고 당기기
            if (Input.GetKey(KeyCode.J)) message.data = 20; // motor a red
            else if (Input.GetKey(KeyCode.K)) message.data = 21; // motor a green

            // 문 여닫기
            if (Input.GetKey(KeyCode.L)) message.data = 22; // motor b green
            else if (Input.GetKey(KeyCode.Semicolon)) message.data = 23; // motor b red
            
            Publish(message);
        }
 
        void UpdateMessage()
        {
            isObstacle = false;
            message.name = "robot dir";
            //수동
            if (isauto == false) 
            {
                PlayerControll();
            }
            //현재 위치에 따라 자동주행할지 반자동 주행할지
            else
            {
                //장애물 정지
                for(int i = 0; i<12; i++)
                {
                    if ((i == 0 || i==1 || i==2 || i==3)&& frontLidar[i] < threshold_5-1)
                    {
                        isObstacle = true;
                    }
                    else if ((i == 4 || i == 5 || i == 6 || i == 7) && frontLidar[i] < threshold_5)
                    {
                        isObstacle = true;
                    }
                    else if ((i == 8 || i == 9 || i == 10 || i == 11) && frontLidar[i] < threshold_5-1)
                    {
                        isObstacle = true;
                    }
                }

                //다음 목표지점
                PathPlanning();

                //많이 안틀어졌으면 벽타고 
                if (Mathf.Abs(xx) < threshold_6_WallNav)
                {
                    Debug.Log("벽타기중");
                    Auto_Move_1();
                }
                //틀어졌다면 naviagtion
                else
                {
                    Debug.Log("네비게이션모드");
                    Nevigation();
                }
            }

            //3*3으로 총 9개.
            //직좌(0) 직우(1) 직가(2)
            //후좌(3) 후우(4) 후가(5)
            //정좌(6) 정우(7) 정가(8)
            

            //장애물 있으면 속도 감속
            if (isObstacle == true && isauto==true /*&& isFast == true*/)
            {
                //Debug.Log("속도 늦춤 threshold5");
                input_ws = 2;
                input_ad = 2;
                //message.data = 30;
                //isFast = false;
            }
            else if(isObstacle == false && isauto == true /*&& isFast == false*/)
            {
                //Debug.Log("속도 정상화 threshold5");
                //message.data = 31;
                //isFast = true;
            }
            message.data = input_ws * 3 + input_ad;

            //속도 변속
            if (Input.GetKey(KeyCode.T)) message.data = 11;
            else if (Input.GetKey(KeyCode.Y)) message.data = 12;
            else if (Input.GetKey(KeyCode.U)) message.data = 13;
            else if (Input.GetKey(KeyCode.I)) message.data = 14;
            else if (Input.GetKey(KeyCode.O)) message.data = 15;
            else if (Input.GetKey(KeyCode.P)) message.data = 16;

            // 박스 밀고 당기기
            if (Input.GetKey(KeyCode.J)) message.data = 20; // motor a red
            else if (Input.GetKey(KeyCode.K)) message.data = 21; // motor a green

            // 문 여닫기
            if (Input.GetKey(KeyCode.L)) message.data = 22; // motor b green
            else if (Input.GetKey(KeyCode.Semicolon)) message.data = 23; // motor b red

            //Debug.Log("message.name: "+ message.name + " data:" + message.data);
            Publish(message);
        }

        //path planning 방법: 모든 위치를 연결해서 저장하고, 끝부분에서부터 훑으며 goal지점을 찾아 순서 저장. 
        void PathPlanning()
        {
            //string nextPlace = "";

            //현재 위치 번호 now_num에 저장
            int now_num = -1;
            for(int i = 0; i<spots.Length; i++)
            {
                if(spots[i].name== nowPlace) now_num = i;
            }

            //목표 위치 번호 goal_num에 저장
            int goal_num = -1;
            for (int i = 0; i < spots.Length; i++)
            {
                if (spots[i].name == goalPlace) goal_num = i;
            }

            // goal-now 번호가 음수인지 양수인지 파악.moveDir true가 양수
            if (goal_num - now_num > 0)
            {
                moveDir = 1;
            }
            else if(goal_num - now_num < 0)
            {
                moveDir = -1;
            }
            else
            {
                moveDir = 0;
            }

            // moveDir에 따라서 다음 위치 찾아 return
            nextPlace = spots[now_num + moveDir].name;
            //return nextPlace;
        }

        public static float GetAngle(Vector3 vStart, Vector3 vEnd)
        {
            Vector3 v = vEnd - vStart;

            return Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
        }

        void Find_Xx()
        {
            PathPlanning();
            //xx 구하기=========================================
            //현재 바라보는 방향 robot_y
            //바라봐야 하는 방향
            //Vector3 l_vector = GameObject.Find(nextPlace).transform.position - GameObject.Find(nowPlace).transform.position;

            float test_angle = GetAngle(GameObject.Find(nextPlace).transform.position, transform.position);
            test_angle -= 180;
            //Debug.Log("test_angle: " + test_angle);
            //시계방향
            if (robot_y > 180)
            {
                robot_y -= 360;
            }
            //Debug.Log("l_vector: " + l_vector + " 바라보는방향: " + robot_y + " 바라봐야하는 방향: " + needSee_y);
            //Debug.Log("test_angle - robot_y:" + (test_angle - robot_y));
            xx = test_angle - robot_y;
            //Debug.Log("xx:" + xx);
            //=================================================
        }

        //Navigation 방법 : 현재위치에서 이동해야 하는 위치를 바라볼 수 있도록 y값을 조정한다. 이때 시계방향으로 돌지 반시례로 돌지도 판단한다.
        void Nevigation()
        {

            //=========변경
            //장애물 정지
            isObstacle = false;
            for (int i = 0; i < 12; i++)
            {
                if ((i == 0 || i == 1 || i == 2 || i == 3) && frontLidar[i] < threshold_5 - 1)
                {
                    isObstacle = true;
                }
                else if ((i == 4 || i == 5 || i == 6 || i == 7) && frontLidar[i] < threshold_5)
                {
                    isObstacle = true;
                }
                else if ((i == 8 || i == 9 || i == 10 || i == 11) && frontLidar[i] < threshold_5 - 1)
                {
                    isObstacle = true;
                }
            }

            
            //=============
            if (xx < -180)
            {
                xx += 360;
            }
            if (xx < -13)
            {
                if (xx < -45)
                {
                    //Debug.Log("좌4");
                    input_ws = 2;
                    input_ad = 0;
                }
                else
                {
                    //Debug.Log("직좌4");
                    input_ws = 0;
                    input_ad = 0;
                }
            }
            //반시계방향
            else if(xx > 13)
            {
                if (xx > 45)
                {
                    //Debug.Log("우4");
                    input_ws = 2;
                    input_ad = 1;
                }
                else
                {
                    //Debug.Log("직우4");
                    input_ws = 0;
                    input_ad = 1;
                }
            }
            //직진
            else
            {
                //Debug.Log("직진4");
                input_ws = 0;
                input_ad = 2;
            }
            //Debug.Log("xx:" + xx);
            if (isObstacle == true && isauto == true)
            {
                input_ws = 2;
                input_ad = 2;
            }
        }

        //자동 조종 코드
        void Auto_Move_1()
        {
            //벽타며 전진
            float leftSum, rightSum;
            leftSum = ydLidar[9] + ydLidar[10]+ ydLidar[11];
            rightSum = ydLidar[4] + ydLidar[5]+ ydLidar[6];
            //정면 벽이면 정지
            if(ydLidar[7]+ydLidar[8]< threshold_1)
            {
                //Debug.Log("정지");
                input_ws = 2;
                input_ad = 2;
            }
            //좌우 차이 얼마 안나면 직진
            else if (Mathf.Abs(leftSum - rightSum) < threshold_2)
            {
                //Debug.Log("직진");
                input_ws = 0;
                input_ad = 2;
            }
            //좌회전
            else if (leftSum > rightSum)
            {
                //Debug.Log("직좌");
                input_ws = 0;
                input_ad = 0;
            }
            //우회전
            else
            {
                //Debug.Log("직우");
                input_ws = 0;
                input_ad = 1;
            }
        }


        //라이다값 받아오기
        void Get_lidar()
        {
            //코드 받아와봐서 레이저
            lsw_code = GameObject.Find("Robot").GetComponent<LaserScanWriter>();
            if (lsw_code.directions.Length != 720)
            {
                //Debug.Log("아직 라이다값 못받음: " + lsw_code.directions.Length);
                return;
            }

            //8등분후 평균 구해서 ydLidar에 넣기
            float ydSum; //0~44까지 라이다값 합
            float ydMean; //0~44까지 라이다값 평균
            int cnt = -1; //몇번째 라이다값?
            int cnt_not0;
            for(int i = 0; i<16; i++)
            {
                ydSum = 0;
                cnt_not0 = 1;
                for (int j = 0; j<45; j++)
                {
                    cnt++;
                    if (lsw_code.ranges[cnt] == 0)
                    {
                        //continue;
                        cnt_not0++;
                        ydSum += 10f;
                    }
                    else
                    {
                        cnt_not0++;
                        ydSum += lsw_code.ranges[cnt];
                    }
                }
                ydMean = ydSum / cnt_not0;
                //Debug.Log(i + " ydSum: " + ydSum + " cnt_not0:" + cnt_not0);
                ydLidar[i] = ydMean;
            }

            //전방 15개씩 총 12세트 frontLidar
            cnt = 269;
            for (int i = 0; i<12; i++)
            {
                ydSum = 0;
                cnt_not0 = 1;
                for (int j = 0; j<15; j++)
                {
                    cnt++;
                    if (lsw_code.ranges[cnt] == 0)
                    {
                        //continue;
                        cnt_not0++;
                        ydSum += 10f;
                    }
                    else
                    {
                        cnt_not0++;
                        ydSum += lsw_code.ranges[cnt];
                    }
                }
                ydMean = ydSum / cnt_not0;
                //Debug.Log(i + " ydSum: " + ydSum + " cnt_not0:" + cnt_not0);
                frontLidar[i] = ydMean;
            }
        }
        private void OnTriggerEnter(Collider collision)
        {
            //Debug.Log(collision.gameObject.name);
            nowPlace = collision.gameObject.name;
        }

        private void OnTriggerStay(Collider collision)
        {
            //Debug.Log(collision.gameObject.name);
            
        }

        private void OnTriggerExit(Collider collision)
        {
            //Debug.Log(collision.gameObject.name);
            //nowPlace = "Default";
        }

        //UI쪽 코드
        void UIManager()
        {
            /*
            FromWeb
            ToWeb
            Where
            isAuto 버튼 - isReceived_text
            isReceived 버튼 - isAuto_text
            */
            client_code = GameObject.Find("WebSocket").GetComponent<Client>();
            ToWeb.text = client_code.fromunity;
            FromWeb.text = client_code.fromweb;
            goalPlace = client_code.fromweb;

            Where.text = nowPlace;

            if (isauto == true)
            {
                isAuto_text.text = "Auto";
            }
            else
            {
                isAuto_text.text = "manual";
            }

            if (isReceived == true)
            {
                isReceived_text.text = "Received: O";
            }
            else{
                isReceived_text.text = "Received: X";
            }
        }

        public void Click_isAuto()
        {
            print("isAuto 클릭");
            if (isauto == true) isauto = false;
            else isauto = true;
        }

        public void Click_isReceived()
        {
            print("isReceived 클릭");
            if (isReceived == true) isReceived = false;
            else isReceived = true;
        }

        /*
        - 시작지점에 로봇 위치
        - start 누르면 바로 112호로 간다.
        - 112호 도착하면 112호를 바라본다
        - 문을 연다 (a초)
        - 박스 넣었다는 버튼 누른다
        - 문을 닫는다 (a초)
        - 웹에서 목적지 입력받는다
        - 이동버튼 클릭하면 목적지로 이동한다.
        - 목적지 도착하면 벽 바라본다
        - 문을 연다 (a초)
        - 물건 내린다 (b초)
        - 앞으로 1초 이동
        - 뒤로 1초 이동
        - 문을 닫는다 (a초)
        - start지점으로 자동 입력된다
        - 이동버튼 입력받으면 start로 간다
        - 멈춘다.
        */
        void TestingManager()
        {
            //정지
            if(nowState == 0)
            {
                //정지해 있는다.
                input_ws = 2;
                input_ad = 2;
                //만약 isReceived 누르면 다음 상태로
                if (isReceived == true)
                {
                    isReceived = false;
                    nowState++;
                    client_code = GameObject.Find("WebSocket").GetComponent<Client>();
                    client_code.fromweb = "112"; //isReceived 누르면 바로 112호로 간다.
                    isauto = true;
                }
            }
            else if (nowState == 1)
            {
                //112호 도착하면 112호를 바라본다
                PathPlanning();
                if(nowPlace != goalPlace)
                {
                    Nevigation();
                    message.data = input_ws * 3 + input_ad;
                    Publish(message);
                }
                if (nowPlace == goalPlace && nowState==1)
                {
                    if(nowState == 1)
                    {
                        input_ws = 2;
                        input_ad = 0;
                        if (robot_y>-95 && robot_y < -75)
                        {
                            input_ws = 2;
                            input_ad = 2;
                            nowState++;
                        }
                        message.data = input_ws * 3 + input_ad;
                        Publish(message);
                    }
                }
            }
            else if (nowState == 2)
            {
                //문을 연다 (a초)
                message.data = 22;
                Publish(message);
                Invoke("ToState3", 9f);
            }
            //버튼 누를때까지 대기
            else if (nowState == 3)
            {
                if (isReceived == true)
                {
                    Invoke("ToState4", 12f);
                    //문을 닫는다 (a초)
                    message.data = 23;
                    Publish(message);
                }
            }
            //목적지 이동하다가 도착하면 벽바라본다
            else if ( nowState == 4)
            {
                client_code = GameObject.Find("WebSocket").GetComponent<Client>();
                client_code.fromweb = "122"; //isReceived 누르면 바로 112호로 간다.
                isReceived = false;
                PathPlanning();
                if (nowPlace != goalPlace)
                {
                    Nevigation();
                    message.data = input_ws * 3 + input_ad;
                    Publish(message);
                }
                if (nowPlace == goalPlace && nowState == 4)
                {
                    if (nowState == 4)
                    {
                        input_ws = 2;
                        input_ad = 1;
                        if (robot_y > 75 && robot_y < 95)
                        {
                            input_ws = 2;
                            input_ad = 2;
                            nowState++;
                        }
                        message.data = input_ws * 3 + input_ad;
                        Publish(message);
                    }
                }
            }
            //문을 연다(a초)
            else if (nowState==5)
            {
                message.data = 22;
                Publish(message);
                Invoke("ToState6", 9f);
            }
            //- 물건 내린다(b초)
            else if (nowState == 6)
            {
                message.data = 20;
                Publish(message);
                Invoke("ToState7", 25f);
            }
            //- 앞으로 1초 이동
            else if (nowState == 7)
            {
                input_ws = 0;
                input_ad = 2;
                message.data = input_ws * 3 + input_ad;
                Publish(message);
                Invoke("ToState8", 1f);
            }
            //-뒤로 1초 이동
            else if (nowState == 8)
            {
                input_ws = 1;
                input_ad = 2;
                message.data = input_ws * 3 + input_ad;
                Publish(message);
                Invoke("ToState9", 2f);
            }
            //-문을 닫는다(a초)
            else if (nowState == 9)
            {
                message.data = 23;
                Publish(message);
                Invoke("ToState10", 12f);
            }
            //- start지점으로 자동 입력된다
            else if (nowState ==10)
            {
                client_code = GameObject.Find("WebSocket").GetComponent<Client>();
                client_code.fromweb = "start";

                PathPlanning();
                if (nowPlace != goalPlace)
                {
                    Nevigation();
                    message.data = input_ws * 3 + input_ad;
                    Publish(message);
                }
                if (nowPlace == goalPlace && nowState == 10)
                {
                    if (nowState == 10)
                    {
                        input_ws = 2;
                        input_ad = 1;
                        if (robot_y > 165)
                        {
                            nowState++;
                            isauto = false;
                        }
                        message.data = input_ws * 3 + input_ad;
                        Publish(message);
                    }
                }
            }
            //- 멈춘다.
            else if(nowState == 11)
            {
                isauto = false;
            }
        }

        void ToState3()
        { 
            if (nowState != 2) return;
            Debug.Log("state3");
            input_ws = 2;
            input_ad = 2;
            message.data = input_ws * 3 + input_ad;
            Publish(message);
            nowState = 3;
        }
        void ToState4()
        {
            if (nowState != 3) return;
            Debug.Log("state4");
            nowState = 4;
        }

        void ToState6()
        {
            if (nowState != 5) return;
            Debug.Log("state6");
            nowState = 6;
        }

        void ToState7()
        {
            if (nowState != 6) return;
            Debug.Log("state7");
            nowState = 7;
        }
        void ToState8()
        {
            if (nowState != 7) return;
            Debug.Log("state8");
            nowState = 8;
        }
        void ToState9()
        {
            if (nowState != 8) return;
            Debug.Log("state9");
            nowState = 9;
        }

        void ToState10()
        {
            if (nowState != 9) return;
            Debug.Log("state10");
            nowState = 10;
        }

    }
}


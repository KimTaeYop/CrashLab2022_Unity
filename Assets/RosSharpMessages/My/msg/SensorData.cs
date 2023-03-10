/* 
 * This message is auto generated by ROS#. Please DO NOT modify.
 * Note:
 * - Comments from the original code will be written in their own line 
 * - Variable sized arrays will be initialized to array of size 0 
 * Please report any issues at 
 * <https://github.com/siemens/ros-sharp> 
 */



namespace RosSharp.RosBridgeClient.MessageTypes.My
{
    public class SensorData : Message
    {
        public const string RosMessageName = "my_msgs/SensorData";

        public string name { get; set; }
        public int data { get; set; }
        public ulong index { get; set; }

        public SensorData()
        {
            this.name = "";
            this.data = 0;
            this.index = 0;
        }

        public SensorData(string name, int data, ulong index)
        {
            this.name = name;
            this.data = data;
            this.index = index;
        }
    }
}

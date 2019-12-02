using System;

namespace SentProt.ClientSocket.AppBase
{


    /// <summary>
    /// STENT信号
    /// </summary>
    public enum StentSignalEnum 
    {
        [Description("登陆")]
        Login = 1,
        [Description("请求数据头")]
        RequestHead = 0Xff,
        [Description("请求发送数据")]
        RequestData = 0Xaa01,
        [Description("请求停止发送数据")]
        StopData = 0Xaa00
    }

    public class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string des)
        {
            Description = des;
        }
        public string Description { get; set; }
    }

    public static class EnumUtil
    {
        public static string GetDescription(this Enum value, bool nameInstead = true)
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            if (name == null)
                return null;

            var field = type.GetField(name);
            var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;

            if (attribute == null && nameInstead)
                return name;
            return attribute?.Description;
        }
    }
}

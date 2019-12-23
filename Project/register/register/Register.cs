using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace register
{
    class Register
    {
        public void probation()
        {
            /***************设置软件试用期*****************/
            //可免费试用45天
            RegistryKey mainkey = Registry.LocalMachine;
            RegistryKey subkey = mainkey.OpenSubKey("SOFTWARE\\PosRegister\\time", true);
            if (subkey == null)
            {
                object usetime = System.DateTime.Now.AddDays(15).ToLongDateString();
                subkey = mainkey.CreateSubKey("SOFTWARE\\PosRegister\\time");
                subkey.SetValue("Position", usetime);
                MessageBox.Show("您可以免费试用软件15天！", "感谢您首次使用");
            }
            try
            {
                DateTime usetime = Convert.ToDateTime(subkey.GetValue("Position"));
                DateTime daytime = DateTime.Parse(System.DateTime.Now.ToLongDateString());
                TimeSpan ts = usetime - daytime;
                int day = ts.Days;
                if (day <= 0)
                {
                    if (MessageBox.Show("软件试用期已到，请注册后再使用！", "提示", MessageBoxButtons.OK) == DialogResult.OK)
                    {
                        Application.Exit();
                    }
                }
                //else 
                //{
                //    MessageBox.Show("本软件的试用期还有" + day.ToString() + "天！", "提示");
                //}
            }
            catch { }
            subkey.Close();
        }

        #region 软件试用期////不用注册表
        public void probation2()
        {
            /***************设置软件试用期*****************/
            //可免费试用到20140501
            try
            {
                DateTime daytime = DateTime.Parse(System.DateTime.Now.ToLongDateString());
                DateTime usetime = daytime.AddDays(30);
                TimeSpan ts = usetime - daytime;
                int day = ts.Days;
                if (day <= 0)
                {
                    if (MessageBox.Show("软件试用期已到，请注册后再使用！", "提示", MessageBoxButtons.OK) == DialogResult.OK)
                    {
                        Application.Exit();
                    }
                }
                else
                {
                    MessageBox.Show("本软件的试用期还有" + day.ToString() + "天！", "提示");
                }
            }
            catch 
            {
            }
        }
        #endregion
    }
}

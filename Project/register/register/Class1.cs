using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Globalization;

namespace register
{
    class Class1
    {
        public void sss()
        {
            string filePath = @"123.txt";
            string s = File.ReadAllText(filePath, Encoding.GetEncoding("GB2312"));
            Base64Crypt encode = new Base64Crypt();
            string smg = encode.Decode(s);
            string msg = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(smg));
           // MessageBox.Show(msg);

           
           
           
            string year = smg.Substring(5, 4);
            string month = smg.Substring(16, 2);
          //  MessageBox.Show(year);
         //   MessageBox.Show(month);
            string day = smg.Substring(24, 2);
          //  MessageBox.Show(day);
            string date1 = year + month + day;

            CultureInfo provider = CultureInfo.InvariantCulture;
            DateTime b = DateTime.ParseExact(date1, "yyyyMMdd", provider);

            string  date2 = DateTime.Now.ToString("yyyyMMdd");
            DateTime c = DateTime.ParseExact(date2, "yyyyMMdd", provider);
            if (b < c)
            {
                MessageBox.Show("软件试用已结束");
                System.Environment.Exit(0);
            }
            else
            {
                MessageBox.Show("软件试用未结束");
            }
        }
    }
}

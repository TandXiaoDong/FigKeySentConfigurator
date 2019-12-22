using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace register
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
          string  time = this.dateTimePicker1.Value.ToString("yyyyMMdd");
           string  timeYear = time.Substring(0, 4);
            string timeMonth = time.Substring(4, 2);
           string timeDay = time.Substring(6, 2);

           // MessageBox.Show(timeDay);

            string registercode = "nihao" + timeYear + "zhucema" + timeMonth + "123456" + timeDay;
           Base64Crypt encode = new Base64Crypt();
           string x = encode.Encode(registercode);

            string Path = @"123.txt";
            FileStream errStr = null;
            StreamWriter errWri = null;
            try
            {
                if(!File.Exists(Path))
                {
                    errWri = File.AppendText(Path);
                    errWri.Write(x);
                    errWri.Flush();
                    errWri.Close();
                }
                else
                {
                    FileInfo fi = new FileInfo(Path);
                    if (fi.Length > 0)
                    {
                        errStr = new FileStream(Path, FileMode.Open, FileAccess.Write);
                        errStr.SetLength(0);
                        errStr.Close();
                    }
                    StreamWriter sw = new StreamWriter(Path, true);
                    sw.WriteLine(x);
                    sw.Close();

                }
            }catch (UnauthorizedAccessException err)
            {
                System.Windows.Forms.MessageBox.Show(err.Message);
            }
            catch (IOException err)
            {
                System.Windows.Forms.MessageBox.Show(err.Message);
            }
            if (errWri != null)
            {
                errWri.Close();
            }
            if (errStr != null)
            {
                errStr.Close();
            }
            this.Close();
        }
    }
}

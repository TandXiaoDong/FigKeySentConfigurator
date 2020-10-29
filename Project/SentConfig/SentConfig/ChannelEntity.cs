using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SentConfig
{
    public class ChannelEntity
    {
        /// <summary>
        /// 0-1-2
        /// </summary>
        public int ChannelIndex { get; set; }

        public int DataSource { get; set; }

        public int CANEnable { get; set; }

        public int DAEnable { get; set; }

        public int SendEnable { get; set; }

        public int SyncErr { get; set; }

        public int CrcErr { get; set; }

        public int UsTick { get; set; }

        public int IdleSt { get; set; }

        public int ChangeF2 { get; set; }

        /// <summary>
        /// 2byte hex, 将输入端取反
        /// </summary>
        public string UsartF1 { get; set; }

        /// <summary>
        /// 2byte hex，将输入端取反
        /// </summary>
        public string UsartF2 { get; set; }
    }
}

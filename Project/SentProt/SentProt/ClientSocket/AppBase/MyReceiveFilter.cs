using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.ProtoBase;

namespace SentProt.ClientSocket.AppBase
{
   public  class MyReceiveFilter:FixedHeaderReceiveFilter<MyPackageInfo>
    {

        /// +-------+---+-------------------------------+
        /// |request| l |                               |
        /// | name  | e |    request body               |
        /// |  (2)  | n |                               |
        /// |       |(2)|                               |
        /// +-------+---+-------------------------------+
        public MyReceiveFilter() : base(4)//2-header + 2-dataLen
        {
            
        }

        protected override int GetBodyLengthFromHeader(IBufferStream bufferStream, int length)
        {
            ArraySegment<byte> buffers = bufferStream.Buffers[0];
            byte[] array = buffers.ToArray();
            //int len = array[length - 2] * 256 + array[length - 1];//高位在前
            int len = array[length - 1] * 256 + array[length - 2];//长度-低位在前
            //int len = (int)array[buffers.Offset + 2] * 256 + (int)array[buffers.Offset + 3];
            return len;//+ 2
        }

        public override MyPackageInfo ResolvePackage(IBufferStream bufferStream)
        {
            //第三个参数用0,1都可以
            byte[] header =bufferStream.Buffers[0].ToArray();
            byte[] bodyBuffer = bufferStream.Buffers[1].ToArray();
            

            //byte[] allBuffer = bufferStream.Buffers[0].Array.CloneRange(0, (int)bufferStream.Length);
            //合并所有buffer
            //byte[] allBuffer
            return new MyPackageInfo(header, bodyBuffer);
        }
    }
}

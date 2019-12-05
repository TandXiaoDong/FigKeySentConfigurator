﻿using System;
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
        public MyReceiveFilter() : base(2)//2-header + 2-dataLen
        {
        }

        protected override int GetBodyLengthFromHeader(IBufferStream bufferStream, int length)
        {
            ArraySegment<byte> buffers = bufferStream.Buffers[0];
            byte[] array = buffers.Reverse().ToArray();
            int len = array[length - 2] * 256 + array[length - 1];//高位在前
            //int len = (int)array[buffers.Offset + 2] * 256 + (int)array[buffers.Offset + 3];
            return len;
        }

        public override MyPackageInfo ResolvePackage(IBufferStream bufferStream)
        {

            //获取接收到的完整数据，包括头和尾
            var body = bufferStream.ReadByte();
            //掐头去尾，只返回中间的数据
            //body = body.Remove(body.Length - end.Length, end.Length);
            //body = body.Remove(0, begin.Length);


            //第三个参数用0,1都可以
            byte[] header =bufferStream.Buffers[0].Reverse().ToArray();
            byte[] bodyBuffer = bufferStream.Buffers[1].ToArray();
            //byte[] allBuffer = bufferStream.Buffers[0].Array.CloneRange(0, (int)bufferStream.Length);
            //合并所有buffer
            //byte[] allBuffer
            return new MyPackageInfo(header, bodyBuffer);
        }
    }
}

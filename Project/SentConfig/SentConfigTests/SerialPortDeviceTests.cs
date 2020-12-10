using Microsoft.VisualStudio.TestTools.UnitTesting;
using SentConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SentConfig.Tests
{
    [TestClass()]
    public class SerialPortDeviceTests
    {
        [TestMethod()]
        public void RevSerialDataTest()
        {
            SerialPortDevice serialPort = new SerialPortDevice();
            byte[] buffer = new byte[] { 0xaa, 0x55, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09};
            serialPort.ReceiveDataUnitTest(buffer);
            //Assert.Fail();
        }
    }
}
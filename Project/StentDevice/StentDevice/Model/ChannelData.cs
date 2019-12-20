using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StentDevice.ClientSocket.AppBase;

namespace StentDevice.Model
{
    public class ChannelData
    {
        private int revCount;
        private int slowSignalCount = 0;
        private List<string> cacheDataPerFrame;
        private List<int> cacheFirstBitValue;
        private int autoSendTimeInternal;
        //private bool isAutoSend = true;
        private bool isAnalysisComplete;
        private bool isFirstReceive = true;
        private Queue<MyPackageInfo> receivePackageInfoQueue;
        private Queue<MyPackageInfo> packageInfoQueueTemp;

        public enum ChannelTypeEnum
        {
            Channel1,
            Channel2
        }

        public enum FrameTypeEnum
        {
            StandardFrame,
            ExtendFrame
        }

        public ChannelTypeEnum ChannelType { get; set; }

        public FrameTypeEnum FrameType { get; set; }

        public Queue<MyPackageInfo> ReceivePackageInfoQueue
        {
            get { return this.receivePackageInfoQueue; }
            set { this.receivePackageInfoQueue = value; }
        }

        public Queue<MyPackageInfo> PackageInfoQueueTemp
        {
            get { return this.packageInfoQueueTemp; }
            set { this.packageInfoQueueTemp = value; }
        }
        public int RevCount
        {
            get { return this.revCount; }
            set { this.revCount = value; }
        }

        public int SlowSignalCount
        {
            get { return this.slowSignalCount; }
            set { this.slowSignalCount = value; }
        }

        public List<string> CacheDataPerFrame
        {
            get { return this.cacheDataPerFrame; }
            set { this.cacheDataPerFrame = value; }
        }

        public List<int> CacheFirstBitValue
        {
            get { return this.cacheFirstBitValue; }
            set { this.cacheFirstBitValue = value; }
        }

        public int AutoSendTimeInternal
        {
            get { return this.autoSendTimeInternal; }
            set { this.autoSendTimeInternal = value; }
        }

        //public bool IsAutoSend
        //{
        //    get { return this.isAutoSend; }
        //    set { this.isAutoSend = value; }
        //}

        public bool IsAnalysisComplete
        {
            get { return this.isAnalysisComplete; }
            set { this.isAnalysisComplete = value; }
        }

        public bool IsFirstReceive
        {
            get { return this.isFirstReceive; }
            set { this.isFirstReceive = value; }
        }
    }
}

#include "sent.h"
#include "pwm.h"

#define ENHANCED_SERIAL_MSG_NUM 18                      //串行数据 发送18帧
#define SHORT_SERIAL_MSG_NUM    16                      //串行数据 发送18帧

#define MIN_IDLE_TICKS  5                             //最小起始空闲时间
#define SYS_PULSE_TICKS 56                            //同步脉冲
#define MIN_DATA_TICKS  12                            //最短数据时间节拍（数据0）
#define ONE_SENTFRAME_NIBBLES 8                       //一帧SENT数据 有8个nibbles

#define SERIAL_MSG_TICKS 294                          //串行数据时间（294*3 = 882us）

#define SERIALMSG_NUM 24                              //发送ID 个数
const uint8_t shortIDSequence[] = {
//  0x01,0x06,
//  0x01,0x05,
//  0x01,0x03,
//  0x01,0x07,
//  0x01,0x08,
//  0x01,0x09,
//  0x01,0x0A,
//  0x01,0x23,
//  0x01,0x29,
//  0x01,0x2A,
//  0X01,0X2B,
//  0X01,0X2C,
	0x01,0x01,
  0x01,0x01,
  0x01,0x01,
  0x01,0x01,
  0x01,0x01,
  0x01,0x01,
  0x01,0x01,
  0x01,0x01,
  0x01,0x01,
  0x01,0x01,
  0X01,0x01,
  0X01,0x01,
};

uint16_t shortMsgSequence[] = {
//  0x0,0x4,
//  0x0,0x6,
//  0x0,0x50,
//  0x0,0x0,
//  0x0,0x0,
//  0x0,0x0,
//  0x0,0x0,
//  0x0,0x37E,
//  0x0,0x829,
//  0x0,0xC80,
//  0x0,0x874,
//  0x0,0x212,
	0x45,0x45,
  0x45,0x45,
  0x45,0x45,
  0x45,0x45,
  0x45,0x45,
  0x45,0x45,
  0x45,0x45,
  0x45,0x45,
  0x45,0x45,
  0x45,0x45,
  0x45,0x45,
  0x45,0x45,
};

static uint16_t shortSequenceNum = sizeof(shortIDSequence)/(sizeof(uint8_t));

const uint8_t externIDSequence[] = {
  0x01,0x90,
  0x01,0x91,
  0x01,0x92,
  0x01,0x93,
  0x01,0x94,
  0x01,0x95,
  0x01,0x96,
  0x01,0x97,
};

const uint16_t externMsgSequence[] = {
  0x123,0x124,
  0x345,0x355,
  0x345,0x345,
  0x345,0x345,
  0x345,0x345,
  0x345,0x345,
  0x345,0x345,
  0x345,0x345,
};

int16_t pwm_sentD1Tab[][4] =
{
  0,      0xFFF, 1, 0,
  1000,   1,     49, -489,
  2000,   0x1EB, 53, -567,
  3000,   0x3fc, 51, -510,
  4000,   0x5fb, 51, -510,
  5000,   0x7f8, 52, -560,
  6000,   0x9fb, 52, -560,
  7000,   0xc03, 53, -630,
  8000,   0xe15, 48, -230,
  9000,   0xff8, 1, 0,
  10000,  0xFFF, 1, 0,
};

static uint16_t externSequenceNum = sizeof(externIDSequence)/(sizeof(uint8_t));

FrameDataType SerialMsgBuf[SERIALMSG_NUM][ENHANCED_SERIAL_MSG_NUM]; //串行数据buf
static uint8_t sequenceid;
sentStr sent;//sent 结构体数据

//static void sent_isr(void);

void sentIdleTicks(uint16_t tickCnt);
void sentIdleTicks(uint16_t tickCnt)
{
  if(tickCnt < MIN_IDLE_TICKS)
  {
    sentLv = sent.idleLv;
  }
  else
  {
    sentLv = sent.activeLv;
  }
}

void sent_Init(void)
{
  GPIO_InitTypeDef  GPIO_InitStructure;

  RCC_APB2PeriphClockCmd(RCC_APB2Periph_GPIOA, ENABLE);  //使能PB,PE端口时钟

  GPIO_InitStructure.GPIO_Pin = GPIO_Pin_12|GPIO_Pin_9; //LED0和LED1对应IO口
  GPIO_InitStructure.GPIO_Mode = GPIO_Mode_Out_PP;     //推挽输出
  GPIO_InitStructure.GPIO_Speed = GPIO_Speed_50MHz;    //IO口速度为50MHz
  GPIO_Init(GPIOA, &GPIO_InitStructure);              //初始化GPIO

  TIM3_Int_Init((216-1), (1-1));
}

void TIM3_Int_Init(u16 arr,u16 psc)
{
  TIM_TimeBaseInitTypeDef TIM_TimeBaseInitStructure;
  NVIC_InitTypeDef NVIC_InitStructure;

  RCC_APB1PeriphClockCmd(RCC_APB1Periph_TIM3,ENABLE);  ///使能TIM3时钟

  TIM_TimeBaseInitStructure.TIM_Period = arr;   //自动重装载值
  TIM_TimeBaseInitStructure.TIM_Prescaler = psc;  //定时器分频
  TIM_TimeBaseInitStructure.TIM_CounterMode = TIM_CounterMode_Up; //向上计数模式
  TIM_TimeBaseInitStructure.TIM_ClockDivision = TIM_CKD_DIV1;

  TIM_TimeBaseInit(TIM3,&TIM_TimeBaseInitStructure);//初始化TIM3

  TIM_ITConfig(TIM3,TIM_IT_Update,ENABLE); //允许定时器3更新中断
  TIM_Cmd(TIM3,ENABLE); //使能定时器3

  NVIC_InitStructure.NVIC_IRQChannel=TIM3_IRQn; //定时器3中断
  NVIC_InitStructure.NVIC_IRQChannelPreemptionPriority=0x00; //抢占优先级1
  NVIC_InitStructure.NVIC_IRQChannelSubPriority=0x00; //子优先级3
  NVIC_InitStructure.NVIC_IRQChannelCmd=ENABLE;
  NVIC_Init(&NVIC_InitStructure);

}

volatile static uint16_t lvCnt = 0;                 //电平发送计数器
volatile static uint8_t nibbleCnt = 0;                //半字节发送计数器
volatile static uint16_t frameTicks = 0;              //一帧时钟节拍数
volatile static uint8_t sentFrameLen = 0;           //发送帧数据计数
volatile static emFrameSts Frame_STA = eFrame_Sys;    //一帧SENT信号发送状态

volatile static uint8_t RollingCnt = 0;

volatile static uint8_t handlercrc4 = 0;
//定时器3中断服务函数
void TIM3_IRQHandler(void)
{
//  if(TIM_GetITStatus(TIM3,TIM_IT_Update)==SET) //溢出中断
//  {
    switch(sent.SENT_STA)
    {
      case eSENT_IDLE:
        lvCnt = 0;
        nibbleCnt = 0;
        sentFrameLen = 0;
        frameTicks = 0;
        sentLv = sent.idleLv;
        Frame_STA = eFrame_Sys;
        break;
      case eSENT_TRANSING:
        if(sentFrameLen < sent.serialMsgLen)
        {//发送多少帧
          switch(Frame_STA)
          {
            case eFrame_Sys:
              if(lvCnt < MIN_IDLE_TICKS)
              {
                sentLv = sent.idleLv;
                lvCnt+=1;
              }
              else
              {
                sentLv = sent.activeLv;
                if(lvCnt >= (SYS_PULSE_TICKS-1))
                {
                  frameTicks = SYS_PULSE_TICKS;
                  lvCnt = 0;
                  nibbleCnt = 0;
                  Frame_STA = eNibble;    //测试
                }
                else
                {
                  lvCnt+=1;
                }
              }
              break;
            case eNibble:

              if(lvCnt < MIN_IDLE_TICKS)
              {
                sentLv = sent.idleLv;
                lvCnt+=1;
              }
              else
              {
                sentLv = sent.activeLv;
                if(lvCnt >= (SerialMsgBuf[sequenceid][sentFrameLen].nibbleBuf[nibbleCnt]+(MIN_DATA_TICKS-2)))//判断半字节后续高电平是否发送完毕
                {
                  frameTicks += (SerialMsgBuf[sequenceid][sentFrameLen].nibbleBuf[nibbleCnt]+MIN_DATA_TICKS);
                  lvCnt = 0;
                  nibbleCnt+=1;
                  if(nibbleCnt > (ONE_SENTFRAME_NIBBLES-1))
                  {
                    lvCnt = 0;
                    nibbleCnt = 0;
                    Frame_STA = ePause;
                  }
                }
                else
                {
                  lvCnt+=1;
                }
              }
              break;
            case ePause:
              if(lvCnt < MIN_IDLE_TICKS)
              {
                sentLv = sent.idleLv;
                lvCnt+=1;
              }
              else
              {
                sentLv = sent.activeLv;
                if(lvCnt >= (SERIAL_MSG_TICKS-frameTicks-2))
                {
                  frameTicks = 0;
                  lvCnt = 0;
                  nibbleCnt = 0;
                  sentFrameLen+=1;
                  RollingCnt+=1;

                  Frame_STA = eFrame_Sys;
                  if(sentFrameLen >= sent.serialMsgLen)
                  {
                    sentFrameLen = 0;     //循环发送
                    sequenceid += 1;
                    if(sequenceid >= SERIALMSG_NUM)
                    {
                      sequenceid = 0;
                    }
                  }
                  SerialMsgBuf[sequenceid][sentFrameLen].Msg.D2[0] = (RollingCnt>>4)&0x0F;
                  SerialMsgBuf[sequenceid][sentFrameLen].Msg.D2[1] = (RollingCnt>>0)&0x0F;

                  handlercrc4 = crc4_cal(&SerialMsgBuf[sequenceid][sentFrameLen].nibbleBuf[1], 6);
                  SerialMsgBuf[sequenceid][sentFrameLen].Msg.Crc4 = handlercrc4;

                }
                else
                {
                  lvCnt+=1;
                }
              }
              break;
            default:
              Frame_STA = eFrame_Sys;
              break;
          }
        }
        else
        {
          sentFrameLen = 0;
          sentLv = sent.idleLv;
          sent.SENT_STA = eSENT_IDLE;//发生一次
        }
        break;
      default:
        sent.SENT_STA = eSENT_IDLE;
        break;
    }
//  }
//  TIM3->SR = ~TIM_IT_Update;
  TIM_ClearITPendingBit(TIM3,TIM_IT_Update);  //清除中断标志位
}

/*多项式为：x4+x3+x2+1;这里主要对4位nibble进行校验，不会大于0x0F，因此crc4表就只有16个值*/
static uint8_t CRC4_Table[16]= {0,13,7,10,14,3,9,4,1,12,6,11,15,2,8,5};
uint8_t crc4_cal(uint8_t *data,uint8_t len)/*data位4位nibble块的值，len为nibble块的数量*/
{
  uint8_t result = 0x03;
  uint8_t tableNo = 0;
  int i = 0;
  for( ;i < len; i++)
  {
    tableNo = result ^ data[i];
    result = CRC4_Table[tableNo];
  }
  return result;
}



/*多项式为：x6+x4+x3+1;这里主要对6位数据块进行校验，不会大于63，因此crc6表就只有64个值*/
static uint8_t CRC6_Table[64]= { 0, 25, 50, 43, 61, 36, 15, 22, 35, 58, 17, 8, 30, 7, 44 ,53,
             31, 6, 45, 52, 34, 59, 16, 9, 60, 37, 14, 23, 1, 24, 51, 42,
             62, 39, 12, 21, 3, 26, 49, 40, 29, 4, 47, 54, 32, 57, 18, 11,
             33, 56, 19, 10, 28, 5, 46, 55, 2, 27, 48, 41, 63, 38, 13, 20};
uint8_t crc6_cal(uint8_t *data,uint8_t len)/*data位6位nibble块的值，len为数据块的数量*/
{
  /*crc初始值*/
  uint8_t result = 0x15;
  /*查表地址*/
  uint8_t tableNo = 0;
  int i = 0;

 /*对额外添加的6个0进行查表计算crc*/
  tableNo = result ^ 0;
  result = CRC6_Table[tableNo];

 /*对数组数据查表计算crc*/
  for(i = 0; i < len; i++)
  {
    tableNo = result ^ data[i];
    result = CRC6_Table[tableNo];
  }

  /*返回最终的crc值*/
  return result;
}

/*计算出当前占空比应当发送多少DATA1*/
uint16_t GetSentD1fromPwmDuty(uint16_t duty)
{
  uint16_t sentD1;
  int16_t k,b;
  uint32_t temp;
  int i;

  if(duty < pwm_sentD1Tab[1][0] || duty >= pwm_sentD1Tab[9][0])
  {
		for(i = 0; i < SERIALMSG_NUM; i += 2)
		{
//			shortMsgSequence[i] = 0x803;
		}
		
    if(duty == pwm_sentD1Tab[9][0])
      sentD1 = pwm_sentD1Tab[9][1];
    else
      sentD1 = pwm_sentD1Tab[0][1];
  }
  else
  {
		for(i = 0; i < SERIALMSG_NUM; i += 2)
		{
//			shortMsgSequence[i] = 0x0;
		}
		
    for(i = 1; i < 9; i++)
    {
      if(duty >= pwm_sentD1Tab[i][0])
      {
        if(duty < pwm_sentD1Tab[i+1][0])
        {
          k = pwm_sentD1Tab[i][2];
          b = pwm_sentD1Tab[i][3];
          break;
        }
      }
    }

    temp = (uint32_t)(duty*k)/100 + b;

    sentD1 = temp&0xfff;
  }

  return sentD1;
}

void sentDispose(uint8_t *data, uint16_t len)
{
  int i;
  int j;
  int offset;
  uint8_t   serialMsgType;      //短/扩展串行消息
  uint8_t   SerialMsgCmd;       //扩展串行消息类型

  uint8_t   SerialMsgId;        //扩展串行ID
  uint16_t  SerialMsgData;      //扩展串行数据
  uint8_t   SerialCh1Ind;       //通道1指示灯

  uint16_t sentD1;

  uint8_t crc4;
  uint8_t serialMsgCrc[SERIALMSG_NUM];          //串行消息 CRC

  uint32_t mCrcDat;
  uint8_t serialMsgCrcBuf[4];   //组合待校验的数据

  //pwm信息
  uint8_t vaildPulseLv;         //有效脉冲宽度
  uint16_t pwmDuty;


  //数据获取
  offset = 0;
  sent.idleLv = data[offset]&0x01;          //空闲电平
  sent.activeLv = (!sent.idleLv) & 0x01;    //激活电平
  serialMsgType = (data[offset]>>1)&0x01;   //短/扩展串行消息类型
  SerialMsgCmd = (data[offset]>>2)&0x01;    //类型 扩展型串行数据长度
  SerialCh1Ind = (data[offset]>>3)&0x01;    //通道指示灯
  vaildPulseLv = (data[offset]>>4)&0x01;    //PWM脉宽有效电平
  offset++;

  pwmDuty = data[offset++];
  pwmDuty <<= 8;
  pwmDuty |= data[offset++];

  if(!vaildPulseLv)
  {
    pwmDuty = 10000 - pwmDuty;
  }

  sentD1 = GetSentD1fromPwmDuty(pwmDuty);

  pwmDispose(pwmDuty);

  for(j = 0; j < SERIALMSG_NUM; j++)
  {
    if(j < shortSequenceNum)
    {
      SerialMsgId = shortIDSequence[j];       //ID
      SerialMsgData = shortMsgSequence[j];
    }
    else if(j < (shortSequenceNum+externSequenceNum))
    {
      SerialMsgId = externIDSequence[j-shortSequenceNum];       //ID
      SerialMsgData = externMsgSequence[j-shortSequenceNum];
    }

    if(serialMsgType)
    {//扩展型 CRC6求解
      sent.serialMsgLen = ENHANCED_SERIAL_MSG_NUM;
      mCrcDat = 0;
      for(i = 0; i< 12; i++)
      {//12位数据
        mCrcDat |= ((SerialMsgData>>(11-i))&0x01)<<(31-2*i);
      }

      if(SerialMsgCmd&0x01)
      {//4bit id 16bit data
        for(i = 0; i < 4; i++)
        {
          mCrcDat |= ((SerialMsgId>>(7-i))&0x01)<<(26-2*i);
        }

        mCrcDat |= (SerialMsgCmd&0x01)<<28;
        for(i = 0; i < 4; i++)
        {
          mCrcDat |= ((SerialMsgData>>(15-i))&0x01)<<(16-2*i);
        }
      }
      else
      {//id
        for(i = 0; i < 4; i++)
        {
          mCrcDat |= ((SerialMsgId>>(7-i))&0x01)<<(26-2*i);
        }

        for(i = 0; i < 4; i++)
        {
          mCrcDat |= ((SerialMsgId>>(3-i))&0x01)<<(16-2*i);
        }
      }

      serialMsgCrcBuf[0] = (mCrcDat>>26)&0x3F;
      serialMsgCrcBuf[1] = (mCrcDat>>20)&0x3F;
      serialMsgCrcBuf[2] = (mCrcDat>>14)&0x3F;
      serialMsgCrcBuf[3] = (mCrcDat>>8)&0x3F;
  //    serialMsgCrcBuf[4] = (mCrcDat>>2)&0x3F;
      //CRC6计算
      serialMsgCrc[j] = crc6_cal(serialMsgCrcBuf, 4);
    }
    else
    {//短串行消息解析
      sent.serialMsgLen = SHORT_SERIAL_MSG_NUM;

      serialMsgCrcBuf[0] = SerialMsgId&0xF;
      serialMsgCrcBuf[1] = (SerialMsgData>>4)&0xF;
      serialMsgCrcBuf[2] = SerialMsgData&0xF;

      serialMsgCrc[j] = crc4_cal(serialMsgCrcBuf, 3);
    }

    /* *******************************装载发送数据********************************** */
    for(i = 0; i < sent.serialMsgLen; i++)
    {
      SerialMsgBuf[j][i].Msg.status.bits.status0 = SerialCh1Ind;
      SerialMsgBuf[j][i].Msg.status.bits.status1 = 0;
      switch(i)
      {
        case 0:
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (serialMsgCrc[j]>>5)&0x01;//CRC6
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 1;
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgId>>3)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 1;
          }
          break;
        case 1:
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (serialMsgCrc[j]>>4)&0x01;//CRC6
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 1;
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgId>>2)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 2:
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (serialMsgCrc[j]>>3)&0x01;//CRC6
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 1;
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgId>>1)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 3:
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (serialMsgCrc[j]>>2)&0x01;//CRC6
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 1;
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgId>>0)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 4:
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (serialMsgCrc[j]>>1)&0x01;//CRC6
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 1;
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>7)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 5:
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (serialMsgCrc[j]>>0)&0x01;//CRC6
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 1;
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>6)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 6:
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>11)&0x01;//DATA
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;//固定0
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>5)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 7://控制码
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>10)&0x01;//DATA
            SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgCmd&0x01);
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>4)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 8: //9-12BIT 4bit ID_H
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>9)&0x01;//DATA
            if(SerialMsgCmd&0x01)
            {//data_16 ID_4
              SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgId>>3)&0x01;
            }
            else
            {//data_12 ID_8
              SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgId>>7)&0x01;
            }
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>3)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 9:
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>8)&0x01;//DATA
            if(SerialMsgCmd&0x01)
            {//data_16 ID_4
              SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgId>>2)&0x01;
            }
            else
            {//data_12 ID_8
              SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgId>>6)&0x01;
            }
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>2)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 10:
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>7)&0x01;//DATA
            if(SerialMsgCmd&0x01)
            {//data_16 ID_4
              SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgId>>1)&0x01;
            }
            else
            {//data_12 ID_8
              SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgId>>5)&0x01;
            }
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>1)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 11:
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>6)&0x01;//DATA
            if(SerialMsgCmd&0x01)
            {//data_16 ID_4
              SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgId>>0)&0x01;
            }
            else
            {//data_12 ID_8
              SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgId>>4)&0x01;
            }
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>0)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 12:
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>5)&0x01;//DATA
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;//固定0
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (serialMsgCrc[j]>>3)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 13:  //14-17 4bit data_H or ID_L
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>4)&0x01;//DATA
            if(SerialMsgCmd&0x01)
            {//data_16 ID_4
              SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgData>>15)&0x01;
            }
            else
            {//data_12 ID_8
              SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgId>>3)&0x01;
            }
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (serialMsgCrc[j]>>2)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 14:
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>3)&0x01;//DATA
            if(SerialMsgCmd&0x01)
            {//data_16 ID_4
              SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgData>>14)&0x01;
            }
            else
            {//data_12 ID_8
              SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgId>>2)&0x01;
            }
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (serialMsgCrc[j]>>1)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 15:
          if(serialMsgType)
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>2)&0x01;//DATA
            if(SerialMsgCmd&0x01)
            {//data_16 ID_4
              SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgData>>13)&0x01;
            }
            else
            {//data_12 ID_8
              SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgId>>1)&0x01;
            }
          }
          else
          {
            SerialMsgBuf[j][i].Msg.status.bits.status2 = (serialMsgCrc[j]>>0)&0x01;
            SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;
          }
          break;
        case 16:
          SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>1)&0x01;//DATA
          if(SerialMsgCmd&0x01)
          {//data_16 ID_4
            SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgData>>12)&0x01;
          }
          else
          {//data_12 ID_8
            SerialMsgBuf[j][i].Msg.status.bits.status3 = (SerialMsgId>>0)&0x01;
          }
          break;
        case 17:
          SerialMsgBuf[j][i].Msg.status.bits.status2 = (SerialMsgData>>0)&0x01;//DATA
          SerialMsgBuf[j][i].Msg.status.bits.status3 = 0;   //固定0
          break;
      }

      SerialMsgBuf[j][i].Msg.D1[0] = (sentD1>>8)&0x0F;
      SerialMsgBuf[j][i].Msg.D1[1] = (sentD1>>4)&0x0F;
      SerialMsgBuf[j][i].Msg.D1[2] = (sentD1>>0)&0x0F;

      SerialMsgBuf[j][i].Msg.D2[0] = (RollingCnt>>4)&0x0F;
      SerialMsgBuf[j][i].Msg.D2[1] = (RollingCnt>>0)&0x0F;
//      SerialMsgBuf[j][i].Msg.D2[2] = (sentD2>>0)&0x0F;
      SerialMsgBuf[j][i].Msg.D2[2] = (~(sentD1>>8))&0x0F;
      //crc4 计算
      crc4 = crc4_cal(&SerialMsgBuf[j][i].nibbleBuf[1], 6);
      SerialMsgBuf[j][i].Msg.Crc4 = crc4;
    }
  }

  sent.SENT_STA = eSENT_TRANSING;
}


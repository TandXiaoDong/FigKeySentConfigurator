#include "SentCal.h"
#include "SentHardConf.h"


void SentCapInit(void)
{
		TIM3_Init(0xffff,1);//4M
		TIM4_Init(0xffff,1);//4M
}


extern uint32_t Tim4CapTab[256];
extern uint32_t Tim3CapTab[256];


uint8_t Tim3RdCounter = 0;
uint8_t Tim4RdCounter = 0;
typedef enum SentStType
{
		S_IDLE=0,
		S_SYNC,
		S_SER,
	  S_D1,
		S_D2,
		S_D3,
		S_D4,
		S_D5,
		S_D6,
		S_CS	  
}SentStType;



typedef union SentSerDataType
{
		uint32_t Data;
		struct 
		{
			uint8_t rev :2;
			uint8_t serbit2 :1;
			uint8_t serbit3 :1;
			uint32_t rev1:   28;
		}BitVal;
}SentSerDataType;


typedef union SentSerCrcDataType
{
		uint32_t Data;
		struct 
		{			
			uint8_t serd1 :6;
			uint8_t serd2 :6;
			uint8_t serd3 :6;
			uint8_t serd4 :6;
			uint8_t rev :8;
		}BitVal;
}SentSerCrcDataType;


typedef struct SentSlowDataType
{
		uint8_t  id;
		uint16_t data;
}SentSlowDataType;

uint8_t CRC4_Table[16]= {0,13,7,10,14,3,9,4,1,12,6,11,15,2,8,5};
uint8_t Crc4CalProg(uint8_t crc,uint8_t data)
{
		uint8_t tableNo = 0;
		tableNo = crc ^ data;
		return (CRC4_Table[tableNo]);
}
uint8_t CRC6_Table[64]= { 0, 25, 50, 43, 61, 36, 15, 22, 35, 58, 17, 8, 30, 7, 44 ,53,
													31, 6, 45, 52, 34, 59, 16, 9, 60, 37, 14, 23, 1, 24, 51, 42,
													62, 39, 12, 21, 3, 26, 49, 40, 29, 4, 47, 54, 32, 57, 18, 11,
													33, 56, 19, 10, 28, 5, 46, 55, 2, 27, 48, 41, 63, 38, 13, 20};

uint8_t Crc6CalProg(uint32_t serdata)
{
			uint32_t temp;
			uint8_t data;
			uint8_t crc = 0;
			uint8_t tableNo = 0;
//	    tableNo = 0x15^0;//0x15 is seed 
//			crc = CRC6_Table[tableNo];	
			data = (serdata>>18)&0x3f;
			tableNo = 59^data;
			crc = CRC6_Table[tableNo];
			data = (serdata>>12)&0x3f;
			tableNo = crc^data;
			crc = CRC6_Table[tableNo];
			data = (serdata>>6)&0x3f;
			tableNo = crc^data;
			crc = CRC6_Table[tableNo];
			data = serdata&0x3f;
			tableNo = crc^data;
			crc = CRC6_Table[tableNo];
	    return crc;
}

SentStType  SentASt = S_IDLE;
uint32_t    ClokRef = 126;
uint32_t    SentCalTicVal = 0;
uint32_t    SerDataP1 = 0;
uint32_t    SerDataP2 = 0;
uint8_t SentDataTemp[6];
uint8_t CRC4FastVal;
uint8_t CRC6SerVal;




uint32_t SentSucCounter = 0;

uint32_t SentErrCounter = 0;

uint32_t SentIdleCounter = 0;
SentSerCrcDataType SentASerCrcTemp;

SentSlowDataType SlowDataTab[256];
uint8_t  SentSerDataCounter=0;

void SentACal(void)
{
		SentSerDataType temp;
		if(Tim3CapTab[Tim3RdCounter] != 0)//缓存有数据
		{
//				if(Tim3CapTab[Tim3RdCounter] > 5645 && Tim3CapTab[Tim3RdCounter] < 8467)//56 tic  sync
				if(Tim3CapTab[Tim3RdCounter] > 6200 && Tim3CapTab[Tim3RdCounter] < 7400)//56 tic  sync
				{
						ClokRef = Tim3CapTab[Tim3RdCounter]/56;
						SentASt = S_SER;
				}
				else
				{
						SentCalTicVal = (Tim3CapTab[Tim3RdCounter]+ClokRef/2)/ClokRef;
						if(SentCalTicVal <12 || SentCalTicVal >27)
						{
								SentIdleCounter++;
								SentASt = S_IDLE;
						}
						else//数据满足格式要求
						{
								SentCalTicVal = SentCalTicVal -12;
								if(SentASt == S_SER)//慢信号
								{
										temp.Data = SentCalTicVal;
									  SerDataP1 = 	SerDataP1<<1 | temp.BitVal.serbit2;//bit2
										SerDataP2 = 	SerDataP2<<1 | temp.BitVal.serbit3;//bit3
										SentASerCrcTemp.Data = SentASerCrcTemp.Data<<1|temp.BitVal.serbit2;
										SentASerCrcTemp.Data = SentASerCrcTemp.Data<<1|temp.BitVal.serbit3;
										if((SerDataP2&0x3f821) == 0x3f000)//
										{
										    CRC6SerVal = Crc6CalProg(SentASerCrcTemp.Data);	
												if(CRC6SerVal == ((SerDataP1>>12)&0x3f))
												{
														if((SerDataP2>>10 &0x01)==0)//8bit id
														{
																SlowDataTab[SentSerDataCounter].id = (uint8_t)((SerDataP2>>2&0xf0)  | (SerDataP2>>1&0xf));
																SlowDataTab[SentSerDataCounter].data =(uint16_t)(SerDataP1 & 0xfff);
														}
														else//4bit id
														{
																SlowDataTab[SentSerDataCounter].id = (uint8_t)((SerDataP2>>6)&0xf);
																SlowDataTab[SentSerDataCounter].data =(uint16_t)((SerDataP2<<11 & 0xf000)|(SerDataP1 & 0xfff));
														}
														SentSerDataCounter++;
												}
										}
										SentASt = S_D1;
								}
								else if(SentASt == S_D1)
								{
										SentDataTemp[0] = SentCalTicVal;
										CRC4FastVal = Crc4CalProg(0x3,SentCalTicVal);
										SentASt = S_D2;
								}
								else if(SentASt == S_D2)
								{
										SentDataTemp[1] = SentCalTicVal;
										CRC4FastVal =  Crc4CalProg(CRC4FastVal,SentCalTicVal);
										SentASt = S_D3;
								}
								else if(SentASt == S_D3)
								{
										SentDataTemp[2] = SentCalTicVal;
										CRC4FastVal =  Crc4CalProg(CRC4FastVal,SentCalTicVal);
										SentASt = S_D4;
								}
								else if(SentASt == S_D4)
								{
										SentDataTemp[3] = SentCalTicVal;
										CRC4FastVal =  Crc4CalProg(CRC4FastVal,SentCalTicVal);
										SentASt = S_D5;
								}
								else if(SentASt == S_D5)
								{
										SentDataTemp[4] = SentCalTicVal;
										CRC4FastVal =  Crc4CalProg(CRC4FastVal,SentCalTicVal);
										SentASt = S_D6;
								}
								else if(SentASt == S_D6)
								{
										SentDataTemp[5] = SentCalTicVal;
										CRC4FastVal =  Crc4CalProg(CRC4FastVal,SentCalTicVal);
										SentASt = S_CS;
								}
								else if(SentASt == S_CS)
								{
										if(CRC4FastVal == SentCalTicVal)
										{
												SentSucCounter++;
										}
										SentASt = S_IDLE;
								}
								else
								{
										SentErrCounter++;
								}
						}						
				}
				Tim3CapTab[Tim3RdCounter] = 0;
				Tim3RdCounter++;
		}
}

SentStType  SentBSt = S_IDLE;
uint32_t    ClokRefB = 126;
uint32_t    SentCalTicValB = 0;
uint32_t    SerDataP1B = 0;
uint32_t    SerDataP2B = 0;
uint8_t SentDataTempB[6];
uint8_t CRC4FastValB;
uint32_t SentSucCounterB = 0;

void SentBCal(void)
{
		SentSerDataType temp;
		if(Tim4CapTab[Tim4RdCounter] != 0)//缓存有数据
		{
				if(Tim4CapTab[Tim4RdCounter] > 5645 && Tim4CapTab[Tim4RdCounter] < 8467)//56 tic  sync
				{
						ClokRefB = Tim4CapTab[Tim4RdCounter]/56;
						SentBSt = S_SER;
				}
				else
				{
						SentCalTicValB = (Tim4CapTab[Tim4RdCounter]+ClokRefB/2)/ClokRefB;
						if(SentCalTicValB <12 || SentCalTicValB >27)
						{
								SentBSt = S_IDLE;
						}
						else//数据满足格式要求
						{
								SentCalTicValB = SentCalTicValB -12;
								if(SentBSt == S_SER)//慢信号
								{
										temp.Data = SentCalTicValB;
									  SerDataP1B = 	SerDataP1B<<1 | temp.BitVal.serbit2;
										SerDataP2B = 	SerDataP2B<<1 | temp.BitVal.serbit3;
										SentBSt = S_D1;
								}
								else if(SentBSt == S_D1)
								{
										SentDataTempB[0] = SentCalTicValB;
										CRC4FastValB = Crc4CalProg(0x3,SentCalTicValB);
										SentBSt = S_D2;
								}
								else if(SentBSt == S_D2)
								{
										SentDataTempB[1] = SentCalTicValB;
										CRC4FastValB =  Crc4CalProg(CRC4FastValB,SentCalTicValB);
										SentBSt = S_D3;
								}
								else if(SentBSt == S_D3)
								{
										SentDataTempB[2] = SentCalTicValB;
										CRC4FastValB =  Crc4CalProg(CRC4FastValB,SentCalTicValB);
										SentBSt = S_D4;
								}
								else if(SentBSt == S_D4)
								{
										SentDataTempB[3] = SentCalTicValB;
										CRC4FastValB =  Crc4CalProg(CRC4FastValB,SentCalTicValB);
										SentBSt = S_D5;
								}
								else if(SentBSt == S_D5)
								{
										SentDataTempB[4] = SentCalTicValB;
										CRC4FastValB =  Crc4CalProg(CRC4FastValB,SentCalTicValB);
										SentBSt = S_D6;
								}
								else if(SentBSt == S_D6)
								{
										SentDataTempB[5] = SentCalTicValB;
										CRC4FastValB =  Crc4CalProg(CRC4FastValB,SentCalTicValB);
										SentBSt = S_CS;
								}
								else if(SentBSt == S_CS)
								{
										if(CRC4FastValB == SentCalTicValB)
										{
												SentSucCounterB++;
										}
										SentBSt = S_IDLE;
								}
								else
								{
								
								}
						}						
				}
				Tim4CapTab[Tim4RdCounter] = 0;
				Tim4RdCounter++;
		}
}
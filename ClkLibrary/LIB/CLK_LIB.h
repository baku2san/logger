//----------------------------------------------------------
// Copyright(C) 2000 by OMRON Corporation
// All Rights Reserved.
//----------------------------------------------------------
//------------------------------------------------
// ファイル名：CLK_LIB.h
//
// ファイルの概要：CLKボード用CLKライブラリヘッダ
//
// 作成者：貫名，石黒
//
// 更新履歴：
//------------------------------------------------
// 更新番号 年月日 更新者 更新内容
//------------------------------------------------
// 1.00/ 9/04 貫名，石黒 新規作成
// CLK_LIB.h: 
//
//////////////////////////////////////////////////////////////////////

#ifndef _CLK_LIB_HEAD_
#define _CLK_LIB_HEAD_


#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

#include <windows.h>

//=====================================================================
// E R R O R   D E F I N I T I O N S
//=====================================================================

#define ERR_UNIT_ADDREES			1	// 号機アドレス範囲エラー
#define ERR_NO_CLKUNIT				2	// CLKユニット無し
#define ERR_NO_MEMORY				3	// メモリ確保エラー
#define ERR_MSG_SIZE_OVER			4	// 受信メッセージサイズオーバー
#define ERR_NO_MEM_TYPE             5   // メモリ種別が存在しない
#define ERR_RESPONSE				6	// レスポンスコードエラー
#define ERR_SEND_BUFFER_SIZE		7	// 送信メッセージサイズエラー
#define ERR_RECV_BUFFER_SIZE		8	// 受信メッセージサイズエラー
#define ERR_WRITE_BUFFER_SIZE		9	// 書込メッセージサイズエラー
#define ERR_READ_BUFFER_SIZE        10	// 読出メッセージサイズエラー
#define ERR_WINDOWHANDLE_PARA		11	// ウィンドウハンドルエラー
#define ERR_MESSAGE_PARA			12  // メッセージ種別エラー
#define ERR_MEMORY_AREA				13	// メモリ種別指定エラー
#define ERR_INTERNAL_FAILURE		14	// 予期しない異常ー
#define ERR_NOT_RING_MODE			15  // 光トークンリングモードではない
#define	ERR_NETWORK_ADDRESS			16  // ネットワークアドレス指定エラー

// リターンコード
const INT CLK_SUCCESS	= 0;	// 成功
const INT CLK_ERROR		= -1;	// 失敗

//=====================================================================
// B A S I C   T Y P E   D E F I N I T I O N S
//=====================================================================

typedef struct {
	HANDLE hNet;							// ネットワークハンドル
	char aEvMemName[2][16];					// メモリ名称
	HANDLE hMem[2];							// メモリハンドル
	int iErrCode;							// 異常コード
} CLKHANDLE,*PCLKHANDLE;

typedef struct {
	BYTE byIcfBits;			// ICF
	BYTE byNetAddr;			// Net
	BYTE byNodeAddr;		// Node
	BYTE byUnitAddr;		// Unit
	int  nSid;				// SID
} CLKHEADER, *PCLKHEADER;

typedef struct {
	LPCTSTR	lpszMemName;	// メモリ名称
	DWORD dwWordOffset;		// CHオフセット
}EMCLKADDRESS, *PEMCLKADDRESS;

typedef struct {
	BYTE byConnectionMethod;	// ワイヤ/光タイプ識別、伝送路形式他
	BYTE byMyNodeAddr;			// 自ノードアドレス
	BYTE byMyUnitAddr;			// 自号機アドレス
	BYTE byMyNetAddr;			// 自ネットワークアドレス
	BYTE abyNodeList[32];		// ネットワーク加入ステータス
	WORD wComunicationCycleTime;// 通信サイクルタイム
	BYTE byPollingNodeAddr;		// 管理局ノードアドレス
	BYTE byStatus1;				// 予約エリア
	BYTE byStatus2;				// 伝送速度、給電状態/終端抵抗設定状態
	BYTE byStatus3;				// 異常情報１
	BYTE byStatus4;				// 予約エリア
	BYTE byStatus5;				// 異常情報２
	BYTE byStatus6;				// 予約エリア
	BYTE byStatusFlag;			// データリンク稼動状態、データリンクモード
	BYTE abyDataLinkStatus[62];	// データリンクステータス
} NSTBUFFER, *PNSTBUFFER;

typedef struct {
	WORD wDisConnectionFlag;				    // 断線検知フラグ
	WORD wDisConnectionNodeInfo1;			    // 断線検知 ノード情報１
	WORD wDisConnectionNodeInfo2;			    // 断線検知 ノード情報２
	BYTE abyDisConnectionInfoRecordTime[6];	    // 断線情報記録開始時刻
	DWORD dwNetworkSeparationCount;				// ネットワーク 離脱回数
	DWORD dwNetworkDisConnectionCount;			// ネットワーク断線状態発生回数
	DWORD dwLocalNodeDisConnectionCount;	    // 自ノード 断線検知回数
	DWORD dwNetwaorkDisConnectMaxCycleCount;    // 断線継続 最大サイクル数
	DWORD dwFrameDropOutsCountSL1;				// フレーム欠落検出回数（SL1側）
	DWORD dwFrameDropOutsCountSL2;				// フレーム欠落検出回数（SL2側）
	DWORD dwFrameBrakesCountSL1;				// フレーム破損検出回数（SL1側）
	DWORD dwFrameBrakesCountSL2;				// フレーム破損検出回数（SL2側）
	DWORD dwCrcErrorCountSL1;					// CRCエラー検出 回数（SL1側）
	DWORD dwCrcErrorCountSL2;					// CRCエラー検出 回数（SL2側）
} RINGBUFFER, *PRINGBUFFER;

#endif


//******************************************************************************
// 	P R O T O T Y P E S
//******************************************************************************


PCLKHANDLE ClkOpen(BYTE byAppUnitAddr,int *piReterr);
int ClkClose(PCLKHANDLE hClk);
int ClkSendFins(PCLKHANDLE hClk, PCLKHEADER pHeader, LPVOID lpMessage, DWORD dwSize);
int ClkRecvFins(PCLKHANDLE hClk, PCLKHEADER pHeader, LPVOID lpMessage, DWORD dwSize, DWORD dwTimeLimit);
int ClkWriteDatalink(PCLKHANDLE hClk, PEMCLKADDRESS pStartAddr, LPWORD pwWriteData, DWORD dwSize);
int ClkReadDatalink(PCLKHANDLE hClk, PEMCLKADDRESS pStartAddr, LPWORD pwReadData, DWORD dwSize);
int ClkGetNetWorkStatus(PCLKHANDLE hClk, BYTE byNet, PNSTBUFFER pBuffer);
int ClkGetRingStatus(PCLKHANDLE hClk, BYTE byNet, PRINGBUFFER pBuffer);
int ClkSetMessageOnArrival(PCLKHANDLE hClk, HWND hWnd, UINT uMsg);
int ClkSetThreadMessageOnArrival(PCLKHANDLE hClk, DWORD dwThreadId, UINT uMsg);
int ClkClearMessageOnArrival(PCLKHANDLE hClk);
int ClkGetLastError(PCLKHANDLE hClk);

#ifdef __cplusplus
}
#endif /*__cplusplus*/

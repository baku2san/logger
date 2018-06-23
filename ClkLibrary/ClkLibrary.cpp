// ClkLibrary.cpp :
// C++ Library においては、PLCに送り込む文字コードがASCIIである必要が有る為
// ・マルチバイト文字セット　でBuild
// ・C#の利用を想定した外部IFへは、Unicodeとする　　→文字列コピーするのは止めて値指定とする

// ❏DllImportで呼ばれたDLLのメモリの保持期間
// https://social.msdn.microsoft.com/Forums/ja-JP/ddd6f440-93e4-482a-9c1a-6377729f81c7/dllimportdll?forum=vcgeneralja
// DllImportは呼ばれる側のDLLをメモリにロードしたら、
// そのままアプリが動作している間中、確保し続ける

// ❏C++/CLIかどうか？
// ref class ReferenceType{}　が反応したので Yes と判断してる

// ❏文字列変換時の注意
// CString は、ThreadSafeじゃないので使うとメモリ破壊起きるらしい

// ❏include vector を"ClkLibrary.h" の後にしたらDLL自体生成できるけど、実行時にDll無いってなる。前にしたらそもそもエラー
// 純粋に配列にしたけど・・

#include "stdafx.h"
#include "ClkLibrary.h"

using namespace System;
using namespace System::Reflection;

[assembly: AssemblyVersionAttribute("1.0.0.0")];
namespace ClkLibrary
{
#define PCLKHANDLE_MAXIMUM_SIZE (7)		// PCLKHANDLE の最大格納数：４本並行動作の為。同時アクセス数を増やすならこれも増やす必要あり。最大どれくらいかは未調査

	static PCLKHANDLE handles[PCLKHANDLE_MAXIMUM_SIZE];
	static BYTE sendBuffer[PCLKHANDLE_MAXIMUM_SIZE][SendCommandIndex::BUFFER_SIZE];
	static CLKHEADER	sendHeader[PCLKHANDLE_MAXIMUM_SIZE];
	static CLKHEADER	receiveHeader[PCLKHANDLE_MAXIMUM_SIZE];

	/// <param name="unitAdr">0 - ( PCLKHANDLE_MAXIMUM_SIZE - 1)</param>
	int Open(int unitAdr, BYTE networkNo, BYTE nodeNo, BYTE unitNo)
	{
		int			iRetErr;		// Error Code
		PCLKHANDLE	handle;

		if (unitAdr >= PCLKHANDLE_MAXIMUM_SIZE) {
			return ErrorCode::UnitAddressOver;	
		}
		handle = ClkOpen(0, &iRetErr);		// 0で自動取得にして、unitAdrはDLL管理に利用するのみとする

		if (handle != (PCLKHANDLE)-1) {
			handles[unitAdr] = handle;
			// OK
		}
		else {
			// ERROR
			return iRetErr;
		}

		// 現状固定値としているものは、Open時に設定
		int sid = (unitAdr + 1);	// SIDに－１～２５５以外を使用した場合はエラー終了します。（詳細エラー情報は　39　：　SID設定エラー）
		sendHeader[unitAdr].byIcfBits = 0x80;
		sendHeader[unitAdr].byNetAddr = networkNo;
		sendHeader[unitAdr].byNodeAddr = nodeNo;
		sendHeader[unitAdr].byUnitAddr = unitNo;
		sendHeader[unitAdr].nSid = sid;
		receiveHeader[unitAdr].byIcfBits = 0xC1;
		receiveHeader[unitAdr].byNetAddr = networkNo;
		receiveHeader[unitAdr].byNodeAddr = nodeNo;
		receiveHeader[unitAdr].byUnitAddr = unitNo;
		receiveHeader[unitAdr].nSid = sid;
		sendBuffer[unitAdr][SendCommandIndex::Command1] = 0x01;
		sendBuffer[unitAdr][SendCommandIndex::Command2] = 0x01;
		sendBuffer[unitAdr][SendCommandIndex::MemoryType] = 0x82;		// DM : 通信コマンド <sbca-304s-1_cs1_cj1_cp1_com_cmd.pdf>	5-19
		sendBuffer[unitAdr][SendCommandIndex::ReadBit] = 0x00;			// 現状Bitは未対応
		return CLK_SUCCESS;
	}

	int Close(int unitAdr) 
	{
		PCLKHANDLE	handle;

		if (unitAdr >= PCLKHANDLE_MAXIMUM_SIZE) {
			return ErrorCode::UnitAddressOver;
		}
		handle = handles[unitAdr];
		if (handle == NULL) {											////
																	// CLK Handle is not opened.
			return ErrorCode::HandleUnexisted;													////
		}															////
		handles[unitAdr] = NULL;
		return ClkClose(handle);
	}

	int ReadAsWord(int unitAdr, EventMemoryCPP acMem, DWORD readOffset, WORD *result, DWORD wordLength)
	{
		PCLKHANDLE	handle;
		EMCLKADDRESS	EmStartAddr;	// Data Link Structure

		if (unitAdr >= PCLKHANDLE_MAXIMUM_SIZE) {
			return ErrorCode::UnitAddressOver;
		}
		handle = handles[unitAdr];
		if (handle == NULL)
		{											////																// CLK Handle is not opened
			return ErrorCode::HandleUnexisted;													////
		}															////

		EmStartAddr.dwWordOffset = readOffset;
		switch (acMem)
		{
		case EventMemoryCPP::CIO:
			EmStartAddr.lpszMemName = "CIO";
			break;
		case EventMemoryCPP::DM:
			EmStartAddr.lpszMemName = "DM";
			break;
		case EventMemoryCPP::EM:
			EmStartAddr.lpszMemName = "EM";
			break;
		default:
			return ErrorCode::MemoryTypeUnexpected;
			break;
		}
		return ClkReadDatalink(handle, &EmStartAddr, result, wordLength);
	}
	int GetLastError(int unitAdr) {
		PCLKHANDLE	handle;

		handle = handles[unitAdr];
		if (handle == NULL) {
			return ErrorCode::HandleUnexisted;
		}
		return ClkGetLastError(handle);
	}
	/// <summary>
	/// BitRead は現状不要なんで未対応	CIO : sendBuf[SendCommandIndex::MemoryType] = 0xB0;		// 通信コマンド <sbca-304s-1_cs1_cj1_cp1_com_cmd.pdf>	5-18
	/// </summary>
	/// <returns>received length</returns>
	int ReadAsWordByFinsAtDM(int unitAdr, DWORD readOffset, WORD *result, DWORD wordLength, DWORD waitMs) {
		PCLKHANDLE	handle;
		int commandResult;
		int receivedLength;
		DWORD byteLength = wordLength * 2 + 4;	// 注意！！！4: header 分は、Call元でBuffer提供してくれる際に確保させる。


		if (unitAdr >= PCLKHANDLE_MAXIMUM_SIZE) {
			return ErrorCode::UnitAddressOver;
		}
		handle = handles[unitAdr];
		if (handle == NULL)
		{											////																// CLK Handle is not opened
			return ErrorCode::HandleUnexisted;													////
		}

		sendBuffer[unitAdr][SendCommandIndex::ReadAddress1] = (BYTE)(readOffset >> 8);	// 1000 : [3] 0x03, [4] 0xE8
		sendBuffer[unitAdr][SendCommandIndex::ReadAddress2] = (BYTE)(readOffset);
		sendBuffer[unitAdr][SendCommandIndex::Length1] = (BYTE)(wordLength >> 8);
		sendBuffer[unitAdr][SendCommandIndex::Length2] = (BYTE)(wordLength);

		commandResult = ClkSendFins(handle, &sendHeader[unitAdr], &sendBuffer[unitAdr][0], SendCommandIndex::BUFFER_SIZE);
		if (commandResult != CLK_SUCCESS) {
			return commandResult;
		}

		receivedLength = ClkRecvFins(handle, &receiveHeader[unitAdr], (BYTE *)result, byteLength, waitMs);
		return receivedLength;	// return 
	}
}

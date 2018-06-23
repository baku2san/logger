#pragma

#ifndef CLKLIBRARY_HEADER_
#define CLKLIBRARY_HEADER_

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

#ifdef CLKLIBRARY_EXPORTS  
#define CLKLIBRARY_WRAPPER __declspec(dllexport)   
#else  
#define CLKLIBRARY_WRAPPER __declspec(dllimport)   
#endif // CLKLIBRARY_EXPORTS


#include "CLK_LIB.h"

namespace ClkLibrary
{
	// TODO: 定義が増えるようなら、専用にHeader分割も検討しようそうしよう
	// #include 使えば、実定義部分（CIO = 0, ... )を共通ファイル化も可能だが、今回プロジェクト分けてるしC#との多重定義とした。
	public enum EventMemoryCPP {    // scoped enum にすると公開が難しくなるので注意。出来ないかも？
		CIO = 0,
		DM,
		EM        // Extended DM 対応しているか不明だが定義しておく
	};
	public enum ErrorCode {
		UnitAddressOver = 100,
		HandleUnexisted,
		MemoryTypeUnexpected,
	};
	private enum SendCommandIndex {
		Command1 = 0,
		Command2,
		MemoryType,
		ReadAddress1,
		ReadAddress2,
		ReadBit,
		Length1,
		Length2,
		BUFFER_SIZE
	};

	CLKLIBRARY_WRAPPER int Open(int unitAdr, BYTE networkNo, BYTE nodeNo, BYTE unitNo);

	CLKLIBRARY_WRAPPER int Close(int unitAdr);

	CLKLIBRARY_WRAPPER int ReadAsWord(int unitAdr, EventMemoryCPP acMem, DWORD readOffset, WORD *result, DWORD wordLength);

	CLKLIBRARY_WRAPPER int GetLastError(int unitAdr);

	CLKLIBRARY_WRAPPER int ReadAsWordByFinsAtDM(int unitAdr, DWORD readOffset, WORD *result, DWORD wordLength, DWORD waitMs);
}

#ifdef __cplusplus
}
#endif /*__cplusplus*/

#endif /* CLKLIBRARY_HEADER_ */
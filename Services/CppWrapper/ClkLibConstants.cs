using System;

namespace loggerApp.CppWrapper
{
    /// <summary>
    /// CliLib利用時の汎用的なConstants
    /// C++側で、ClkLibHelper内にとどまるものは、内部定義としてある
    /// </summary>
    public class ClkLibConstants
    {
        public enum AccessSize
        {
            BIT = 0,
            BYTE = 1,               // Word Access Address しか無い場合、Byte指定はあり得ない。上位/下位のどちらかが取得不能なので
            WORD = 2,
            DWORD = 4,
            WORD2SmallInt= 12       // Word情報を処理してSmallIntにしたもの
        }
        public enum EventMemory
        {
            CIO = 0,
            DM,
            EM      // Extended DM 対応しているか不明だが定義しておく
        };
        public enum CLK_ReturnCode
        {
            // CLK_LIB.h より
            CLK_ERROR = -1,             	    // 失敗
            CLK_SUCCESS = 0,
            // CLK_LIB API関数リファレンスより
            ERR_UNIT_ADDREES,                   // 号機アドレス範囲エラー
            ERR_NO_CLKUNIT,     	        	// CLKユニットなし
            ERR_NO_MEMORY,     		            // メモリ確保エラー
            ERR_MSG_SIZE_OVER,     	        	// 受信メッセージサイズオーバー
            ERR_NO_MEM_TYPE,     	        	// メモリ種別が存在しない
            ERR_RESPONSE,     		            // レスポンスコードエラー
            ERR_SEND_BUFFER_SIZE,         		// 送信メッセージサイズエラー
            ERR_RECV_BUFFER_SIZE,         		// 受信メッセージサイズエラー
            ERR_WRITE_BUFFER_SIZE,         		// 書込データサイズエラー
            ERR_READ_BUFFER_SIZE,         		// 読出データサイズエラー
            ERR_WINDOWHANDLE_PARA,         		// ウィンドウハンドルエラー
            ERR_MESSAGE_PARA,     	    	    // メッセージ種別エラー
            ERR_MEMORY_AREA,     	    	    // メモリ種別指定エラー
            ERR_INTERNAL_FAILURE,         		// 予期しない異常
            ERR_NOT_RING_MODE,     	    	    // 光トークンリングモードではない
            ERR_NETWORK_ADDRESS,         		// ネットワークアドレス指定エラー
            ERR_NO_SUPPORT = 30,                // サービス未サポート
            ERR_LACK_OF_MEMORY = 32,            // メモリ不足のため実行不可
            ERR_DUPLICATE_UNITADR,              // 同一号機アドレス加入エラー
            ERR_NETWORK_BUSY,                   // ネットワークビジーのため実行不可
            ERR_BUFFER_OVERFLOW,                // 受信バッファオーバーフロー
            ERR_DATA_SIZE,                      // データサイズエラー
            ERR_FINS_HEADER,                    // FINSヘッダ異常
            ERR_RECIEVE_TIMEOUT,                // 受信タイムアウト（受信データなし）
            ERR_SID_SETTING,                    // SID設定エラー
            ERR_NO_ADDRESS_IN_ROOTING = 41,     // 宛先アドレスがルーチングテーブルに設定されていないため実行不可
            ERR_ROOTING_TABLE,                  // ルーチングテーブル設定エラーのため実行不可
            ERR_GATEWAY_OVER,                   // ゲートウェイ回数オーバー
            ERR_UNKNOWN_61 = 61,                // なんか発生したけど不明・・一応ここに記録。TODO:調査したいところ発生要因不明。号機アドレス異常の最終形態？
            ERR_DUPLICATE_MESSAGE = 69,         // メッセージ２重定義エラー
            // 以降は、独自定義
		    UnitAddressOver = 100,
		    HandleUnexisted,
		    MemoryTypeUnexpected,
        }
        public const int MutipleAccess = 7; //  PCLKHANDLE_MAXIMUM_SIZE
        public const int FinsReceiveHeaderSizeAsWord = 4 / 2; //  FINS 受信コマンドのHeader size 4bytes
        public const UInt32 FinsReceiveCommandWaitTime = 1000; //  FINS 受信コマンドの応答待ち時間
        public const UInt32 DataLinkStartAddressDM = 16000; //  DataLink の先頭Address
        public const UInt32 DataLinkEndAddressDM = 17599; //  DataLink の終端Address

        [Flags]
        public enum BitPlace : UInt16
        {
            NOBIT = 0x0000,
            BIT00 = 0x0001,
            BIT01 = 0x0002,
            BIT02 = 0x0004,
            BIT03 = 0x0008,
            BIT04 = 0x0010,
            BIT05 = 0x0020,
            BIT06 = 0x0040,
            BIT07 = 0x0080,
            BIT08 = 0x0100,
            BIT09 = 0x0200,
            BIT10 = 0x0400,
            BIT11 = 0x0800,
            BIT12 = 0x1000,
            BIT13 = 0x2000,
            BIT14 = 0x4000,
            BIT15 = 0x8000
        }
    }
}

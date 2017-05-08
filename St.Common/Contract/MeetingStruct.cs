using System;
using System.Runtime.InteropServices;

namespace St.Common
{
    //delegates
    public delegate void ViewChange(SpeakerView speakerView);

    public delegate void GetMeetingListCallback(GetMeetingListResult getMeetingListResult);

    public delegate void AttendeeChange(ContactInfo contactInfo);

    public delegate void ReceiveUIMessage(UIMessage message);

    public delegate void ReceiveMsg(AsynCallResult Msg);

    public delegate void ReceiveInvitation(MeetingInvitation invitation);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PFunc_CallBack(int cmdId, IntPtr pData, int dataLen, long ctx);

    #region  结构体

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ContactInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)] public string m_szPhoneId; /*视讯号*/
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string m_szDisplayName; /*名称*/
    }

    //窗口位置信息
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ViewRect
    {
        public int m_left;
        public int m_right;
        public int m_top;
        public int m_bottom;
    }

    //设备信息
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DeviceInfo
    {
        public int m_isDefault; /*是否是默认设备*/
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string m_szDevName; /*设备名称*/
    }

    //参会者信息结构体定义
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ParticipantInfo
    {
        public ContactInfo m_contactInfo; /*参会者视讯号 和 名称*/
        public int m_bIsSpeaking; /*是否正在发言 0 否  非0 是*/
    }

    //当前会议信息结构体定义
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CurMeetingInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)] public string m_szTeacherPhoneId; /*老师的视讯号*/
        //ParticipantInfo*	m_pParticipantsInfo;			/*参会人列表指针*/
        public IntPtr m_pParticipantsInfo; /*参会人列表指针*/
        public int m_iParticipantsCount; /*参会人列表中成员的个数*/
    }

    //异步调用结果
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class AsynCallResult
    {
        public int m_rc; /*错误码 0：成功  非0：失败*/
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string m_message; /*错误描述信息*/
    }

    //启动结果
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class StartResult
    {
        public AsynCallResult m_result; /*异步调用结果*/
        public ContactInfo m_selfInfo; /*自己的视讯号和名称，异步调用成功时有效*/
    }

    //创建会议结果
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CreateMeetingResult
    {
        public AsynCallResult m_result; /*异步调用结果*/
        public int m_meetingId; /*创建的会议Id*/
    }

    //加入会议结果
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class JoinMeetingResult
    {
        public AsynCallResult m_result; /*异步调用结果*/
        public CurMeetingInfo m_meetingInfo; /*会议信息，调用成功时有效*/
    }

    //邀请参会通知
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct MeetingInvitation
    {
        public ContactInfo m_invitor; /*邀请者信息*/
        public int m_meetingId; /*邀请参加的会议Id*/
    }

    //发言者视频窗口信息
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class SpeakerView
    {
        public ContactInfo m_speaker; /*发言者信息*/
        public ViewRect m_viewPosition; /*窗口默认位置*/
        //void *				m_viewHwnd;		/*窗口句柄*/
        public IntPtr m_viewHwnd; /*窗口句柄*/
        public int m_viewType; /*视图类型 1：摄像头  2：课件 */
        public int m_visible; /*是否可见 0：不可见  非0：可见*/
    }

    //参会者间的透传消息
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct UIMessage
    {
        public ContactInfo m_sender; /*消息发送者*/
        public int m_messageId; /*消息Id*/
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 160)] public string m_szData; /*消息携带数据*/

        private readonly int m_DataLen; /*携带数据长度*/
    }

    //当前可参加的会议列表
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct MeetingInfo
    {
        public int m_meetingId; /*会议Id*/
        public int m_meetingType; /*会议类型 1即时会议 2 预约会议*/
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)] public string m_szCreatorId; /*创建者视讯号*/
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string m_szCreatorName; /*创建者名称*/
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)] public string m_szStartTime; /*会议开始时间*/

    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct MeetingList
    {
        //MeetingInfo *			m_pMeetings;		/*会议信息指针*/
        public IntPtr m_pMeetings; /*会议信息指针*/
        public int m_count; /*会议数*/
    }

    #endregion

    /*取得会议列表结果*/

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct GetMeetingListResult
    {
        public AsynCallResult m_result; /*异步调用结果*/
        public MeetingList m_meetingList; /*会议列表*/
    };

    /*查询名称结果*/

    public struct QueryNameResult
    {
        public AsynCallResult m_result; /*异步调用结果*/
        public ContactInfo m_contactInfo; /*视讯号 和 名称*/
    };

    /// <summary>
    /// 推流参数
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct LiveParam
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string m_url1; //直播流地址1,直播流地址2

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string m_url2; //直播流地址1,直播流地址2

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string m_sRecordFilePath; //录制文件路径

        public int m_nWidth; //直播画面宽度
        public int m_nHeight; //直播画面高度
        public int m_nVideoBitrate; //直播视频码率(单位Kbps)	
        public int m_nSampleRate; //采样率
        public int m_nChannels; //声道数
        public int m_nBitsPerSample; //采样精度
        public int m_nAudioBitrate; //直播音频码率(单位Kbps)
        public int m_nIsLive; //是否直播
        public int m_nIsRecord; //是否录制
    }

    /// <summary>
    /// 开始直播返回结果
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class StartLiveStreamResult
    {
        /// <summary>
        /// 异步调用结果
        /// </summary>
        public AsynCallResult m_result;

        /// <summary>
        /// 创建的直播Id
        /// </summary>
        public int m_liveId;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class LocalRecordResult
    {
        /// <summary>
        /// 异步调用结果
        /// </summary>
        public AsynCallResult m_result;

        /// <summary>
        /// 创建的直播Id
        /// </summary>
        public int m_liveId;
    }

    /*监控推流录制*/

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class MonitorBroadcastResult
    {
        public AsynCallResult m_result; /*异步调用结果*/
        public int m_streamid; /*流id*/
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct VideoDeviceInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Name;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public VideoFormat[] Formats;
        public int FormatCount;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct VideoFormat
    {
        public VideoColorSpace ColorSpace;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public VideoSize[] VideoSizes;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public int[] Fps;
        public int sizeCount;
        public int fpsCount;
    }

    public enum VideoColorSpace
    {
        COLORSPACE_I420,
        COLORSPACE_YV12,
        COLORSPACE_NV12,
        COLORSPACE_YUY2,
        COLORSPACE_YUYV,
        COLORSPACE_RGB,
        COLORSPACE_MJPG
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct VideoSize
    {
        public int Width;
        public int Height;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct LiveVideoStreamInfo
    {
        public int XLocation;
        public int YLocation;
        public int Width;
        public int Height;
        public uint Handle;
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct RecordParam
    {
        public int Width; //直播画面宽度     
        public int Height; //直播画面高度
        public int VideoBitrate; //直播视频码率(单位Kbps)
        public int SampleRate; //采样率    
        public int Channels; //声道数
        public int BitsPerSample; //采样精度
        public int AudioBitrate; //直播音频码率(单位Kbps)
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct VideoStreamParam
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)] public string m_sPhoneId; //视讯号
        public int m_nMediaType;
        public int m_nVideoWidth;
        public int m_nVideoHeight;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class GetvideoParamResult
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public VideoStreamParam[] m_VideoParams;
        public int m_count; //实际取到的数目
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DeviceUpdateInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string m_szDevName; /*设备名称*/
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DeviceUpdateResult
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public DeviceUpdateInfo[] m_Devices;
        public int m_count; //实际取到的数目
        public int m_nDeviceType; //0:视频设备1：音频设备2：音频播放设备

    }
}
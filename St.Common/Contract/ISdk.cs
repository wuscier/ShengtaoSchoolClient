using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace St.Common
{
    public interface ISdk
    {
        //events
        //event Action<string> InternalMessagePassThroughEvent;
        event ViewChange ViewCreateEvent;
        event ViewChange ViewCloseEvent;
        event Action StartSpeakEvent;
        event Action StopSpeakEvent;
        event Action ExitMeetingEvent;
        event GetMeetingListCallback GetMeetingListEvent;
        event AttendeeChange OtherJoinMeetingEvent;
        event AttendeeChange OtherExitMeetingEvent;
        event ReceiveUIMessage UIMessageReceivedEvent;
        event ReceiveMsg ErrorMsgReceivedEvent;
        event ReceiveInvitation InvitationReceivedEvent;
        event ReceiveMsg KickedByHostEvent;
        event ReceiveMsg DiskSpaceNotEnough;

        int MeetingId { get; }

        string SelfName { get; }

        string SelfPhoneId { get; }

        string TeacherPhoneId { get; }

        bool IsSpeaker { get; }

        bool MeetingAgentStarted { get; }

        void SetSelfInfo(string phoneId, string name);

        void SetTeacherPhoneId(string phoneId);
        
        void SetMeetingId(int meetingId);

        void SetMeetingAgentStatus(bool started);

        //void OnInternalMessagePassThrough(string internalMsg);

        int SetWorkingDirectory(string directory);

        Task<StartResult> Start(UserInfo userInfo);

        int Stop();

        Task<CreateMeetingResult> CreateMeeting(ContactInfo[] contactInfos, int count);

        Task<StartLiveStreamResult> StartLiveStream(LiveParam liveParam, LiveVideoStreamInfo[] streamsInfos, int count);

        Task<AsynCallResult> StopLiveStream(int liveId);

        Task<AsynCallResult> UpdateLiveVideoStreams(int liveId, LiveVideoStreamInfo[] streamsInfos, int count);

        AsynCallResult GetMeetingList();

        Task<AsynCallResult> InviteParticipants(int meetingId, ContactInfo[] contactInfos, int count);

        Task<JoinMeetingResult> JoinMeeting(int meetingId, uint[] hwnds, int count, uint[] docHwnds, int docCount);

        Task<AsynCallResult> QueryMeetingExist(int meetingId);

        Task<int> GetParticipantsCount();

        List<ParticipantInfo> GetParticipants();

        Task<AsynCallResult> ApplyToSpeak();

        Task<bool> StopSpeak();

        Task<AsynCallResult> SendUIMessage(int messageId, string pData, int dataLength, string targetPhoneId);

        Task<AsynCallResult> StartShareDoc();

        Task<AsynCallResult> StopShareDoc();

        bool IsShareDocOpened();

        void ShowQosView();

        void CloseQosView();

        Task<AsynCallResult> ExitMeeting();

        DeviceInfo[] GetDeviceList(int devType);


        //void SetVideoInfo(int mainCamera, int secondaryCamera, string mainCameraName, int mainWidth, int mainHeight, int mainBitRate, string secondaryCameraName, int secondaryWidth, int secondaryHeight, int secondaryBitRate);

        //void SetAudioInfo(int mainMic, int secondaryMic, int speaker, string mainMicName, string secondaryMicName, string speakerName, int sampleRate, int bitRate);

        void SetDefaultDevice(int deviceType, string deviceName);

        void SetVideoResolution(int videoType, int width, int height);

        void SetVideoBitRate(int videoType, int bitRate);

        void SetAudioSampleRate(int sampleRate);

        void SetAudioBitRate(int bitRate);

        //void SetDefaultCamera(string cameraName);

        string GetSerialNo();

        void SetViewDisplayName(IntPtr viewHwnd, string name);

        void SetViewPosition(IntPtr viewHwnd, ViewRect viewRect);

        Task SetViewVisible(IntPtr viewHwnd, int visible);

        Task BringViewPanelFront(IntPtr viewHwnd);

        Task<AsynCallResult> SetDoulbeScreenRender(string phoneId, int mediaType, int doubleScreenRenderTrigger, IntPtr displayWindowIntPtr);

        Task AdjustVideoLayoutEnd();

        Task<AsynCallResult> HostKickoutUser(string userPhoneId);

        Task<AsynCallResult> OpenCamera(string cameraName);
        Task<AsynCallResult> CloseCamera();
        Task<AsynCallResult> OpenSharedCamera(string cameraName);
        Task<AsynCallResult> CloseSharedCamera();
        Task<AsynCallResult> SetDefaultCamera(int type, string cameraName);

        int ShowCameraProtityPage(string cameraName);

        VideoDeviceInfo GetVideoDeviceInfos(string cameraName);

        Task<AsynCallResult> SetMicMuteState(int muteState);
        Task<AsynCallResult> StartScreenSharing();
        Task<AsynCallResult> StopScreenSharing();

        Task<AsynCallResult> RequireUserSpeak(string phoneId);
        Task<AsynCallResult> RequireUserStopSpeak(string phoneId);

        Task BanToSpeak(string[] phoneIds);
        Task AllowToSpeak(string[] phoneIds);

        Task<LocalRecordResult> StartRecord(string fileName, LiveVideoStreamInfo[] streamsInfos, int count);

        Task<AsynCallResult> StopRecord();

        Task<int> SetRecordDirectory(string recordDir);
        Task<AsynCallResult> SetRecordParam(RecordParam recordParam);

        Task<MonitorBroadcastResult> StartMonitorBroadcast(LiveParam liveParam);

        Task<AsynCallResult> StopMonitorBroadcast(int streamId);

        Task<GetvideoParamResult> GetVideoStreamsParam();

        /// <summary>
        /// 设置视频窗口绘制填充模式（启动sdk成功后调用）
        /// </summary>
        /// <param name="fillMode">0 保持原始图片直接显示模式, 1：保持原始图片裁剪之后拉伸模式, 4：原始图片无条件拉伸模式</param>
        /// <returns></returns>
        int SetFillMode(int fillMode);
    }
}

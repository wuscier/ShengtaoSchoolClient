using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Serilog;
using St.Common;

namespace St.Core
{
    public class SdkService : ISdk
    {
        public enum ENUM_CALLBACK_CMD_TYPE
        {
            /*异步调用结果*/
            enum_startup_result = 100, /*启动结果					回调参数 StartResult				*/
            enum_createMeeting_result, /*创建会议结果				回调参数 CreateMeetingResult		*/
            enum_invite_result, /*邀请参会结果				回调参数 AsynCallResult				*/
            enum_joinmeeting_result, /*进入会议结果				回调参数 JoinMeetingResult			*/
            enum_applyspeak_result, /*申请发言结果				回调参数 AsynCallResult				*/
            enum_exitmeeting_result, /*退出会议结果				回调参数 AsynCallResult				*/
            enum_getmeetinglist_result, /*取得会议列表结果			回调参数 GetMeetingListResult		*/
            enum_queryname_result, /*查询名称结果				回调参数 QueryNameResult			*/
            enum_modify_name_result, /*修改显示名称结果			回调参数 AsynCallResult				*/
            enum_senduimessage_result, /*发送消息结果				回调参数 AsynCallResult				*/
            enum_verifymeetexist_result, /*查询会议是否存在结果		回调参数 AsynCallResult				*/
            enum_open_doc_result, /*打开课件结果				回调参数 AsynCallResult				*/
            enum_close_doc_result, /*关闭课件结果				回调参数 AsynCallResult				*/
            enum_start_broadcast_result, /*开始推流结果				回调参数 AsynCallResult				*/
            enum_stop_broadcast_result, /*停止推流结果				回调参数 AsynCallResult				*/
            enum_create_datemeeting_result, /*创建预约会议结果			回调参数 CreateMeetingResult		*/
            enum_req_user_speak_result, /*请求用户发言结果			回调参数 AsynCallResult				*/
            enum_req_user_stopspeak_result, /*请用户停止发言结果		回调参数 AsynCallResult				*/
            enum_openuserstream_result, /*打开用户流结果			回调参数 AsynCallResult				*/
            enum_closeuserstream_result, /*关闭用户流结果			回调参数 AsynCallResult				*/
            enum_start_record_result, /*开始录制结果				回调参数 AsynCallResult				*/
            enum_stop_record_result, /*停止录制结果				回调参数 AsynCallResult				*/
            enum_set_mute_state_result, /*设置麦克风静音结果		回调参数 AsynCallResult				*/
            enum_host_kick_user_result, /*设置麦克风静音结果		回调参数 AsynCallResult				*/
            enum_start_screensharing_result, /*开始屏幕分享结果			回调参数 AsynCallResult				*/
            enum_stop_screensharing_result, /*停止桌面分享结果			回调参数 AsynCallResult				*/
            enum_open_camera_result, /*打开摄像头				回调参数 AsynCallResult				*/
            enum_close_camera_result, /*关闭摄像头				回调参数 AsynCallResult				*/
            enum_open_data_camera_result, /*打开数据分享				回调参数 AsynCallResult				*/
            enum_close_data_camera_result, /*关闭数据分享				回调参数 AsynCallResult				*/
            enum_set_default_camera_result, /*设置默认摄像头			回调参数 AsynCallResult				*/
            enum_update_live_layout_result, /*更新直播视频流布局		回调参数 AsynCallResult				*/
            enum_set_record_param_result, /*设置录制参数				回调参数 AsynCallResult				*/
            enum_stop_monitor_result, /*停止监控推流				回调参数 AsynCallResult				*/
            enum_start_monitor_result, /*开始监控推流				回调参数 MonoitorBroadcastResult	*/
            enum_get_video_param_result, /*取得视频分辨率			回调参数 GetvideoParamResult		*/
            enum_device_update_result, /*设备更新					回调参数 GetvideoParamResult		*/
            enum_check_disk_space,			/*检查磁盘空间				回调参数 AsynCallResult		*/

            /*通知消息*/
            enum_recive_invitation = 200, /*收到参会邀请通知			回调参数 MeetingInvitation			*/
            enum_view_created, /*新视频窗口创建通知		回调参数 SpeakerView				*/
            enum_view_closed, /*视频窗口关闭通知			回调参数 SpeakerView				*/
            enum_startspeak, /*开始发言通知				无回调参数							*/
            enum_stopspeak, /*停止发言通知				无回调参数							*/
            enum_other_startspeak, /*其他人开始发言通知		回调参数 ContactInfo（发言人信息）	*/
            enum_other_stopspeak, /*其他人停止发言通知		回调参数 ContactInfo（发言人信息）	*/
            enum_other_joinmeeting, /*其他人进入会议通知		回调参数 ContactInfo（进入者信息）	*/
            enum_other_exitmeeting, /*其他人退出会议通知		回调参数 ContactInfo（退出者信息）	*/
            enum_other_message, /*收到来自其他参会者的消息	回调参数 UIMessage					*/
            enum_device_lost, /*设备丢失通知				回调参数 DeviceLost					*/
            enum_set_double_screen_render, /*设置双屏渲染模式			回调参数							*/

            enum_kicked_by_host,


            /*通用的底层回调的错误提示信息*/
            enum_error_message = 300 /*底层错误提示信息	回调参数 AsynCallResult						*/
        }

        //private fields
        private static readonly object _syncRoot = new object();
        private static readonly Dictionary<string, ITaskCallback> _hash = new Dictionary<string, ITaskCallback>();
        private static PFunc_CallBack _callbackFunc;

        //events
        public event ViewChange ViewCreateEvent;
        public event ViewChange ViewCloseEvent;
        public event Action StartSpeakEvent;
        public event Action StopSpeakEvent;
        public event Action ExitMeetingEvent;
        //public event Action<string> InternalMessagePassThroughEvent;
        public event GetMeetingListCallback GetMeetingListEvent;
        public event AttendeeChange OtherJoinMeetingEvent;
        public event AttendeeChange OtherExitMeetingEvent;
        public event ReceiveUIMessage UIMessageReceivedEvent;
        public event ReceiveMsg ErrorMsgReceivedEvent;
        public event ReceiveInvitation InvitationReceivedEvent;
        public event ReceiveMsg KickedByHostEvent;
        public event ReceiveMsg DiskSpaceNotEnough;


        //properties
        public bool MeetingAgentStarted { get; private set; }

        public string SelfName { get; private set; }

        public string TeacherPhoneId { get; private set; }

        public string SelfPhoneId { get; private set; }

        public bool IsSpeaker => SelfPhoneId == TeacherPhoneId;

        public int MeetingId { get; private set; }

        public void SetMeetingId(int meetingId)
        {
            MeetingId = meetingId;
        }

        public void SetTeacherPhoneId(string phoneId)
        {
            TeacherPhoneId = phoneId;
        }

        public void SetSelfInfo(string phoneId, string name)
        {
            SelfName = name;
            SelfPhoneId = phoneId;
        }

        public void SetMeetingAgentStatus(bool started)
        {
            MeetingAgentStarted = started;
        }


        //methods
        public int SetWorkingDirectory(string directory)
        {
            int result = MeetingAgent.Init(directory);

            return result;
        }

        public Task AdjustVideoLayoutEnd()
        {
            throw new NotImplementedException();
        }

        public Task<AsynCallResult> ApplyToSpeak()
        {
            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            var tcs = new TaskCallback<AsynCallResult>("ApplyToSpeak");

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.ApplyToSpeak();
            if (result != 0)
                SetResult(tcs.Name, new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "申请发言失败！"
                });

            return tcs.Task;
        }

        public Task BringViewPanelFront(IntPtr viewHwnd)
        {
            throw new NotImplementedException();
        }

        public void CloseQosView()
        {
            throw new NotImplementedException();
        }

        public Task<CreateMeetingResult> CreateMeeting(ContactInfo[] contactInfos, int count)
        {
            var tcs = new TaskCallback<CreateMeetingResult>("CreateMeeting");

            if (!MeetingAgentStarted)
                return Task.FromResult(new CreateMeetingResult());

            if (_hash.ContainsKey(tcs.Name))
                return Task.FromResult(new CreateMeetingResult());

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.CreateMeeting(contactInfos, count);

            if (result != 0)
                SetResult(tcs.Name, new CreateMeetingResult
                {
                    m_result = new AsynCallResult
                    {
                        m_rc = result,
                        m_message = "创建会议失败！"
                    }
                });

            return tcs.Task;
        }

        public Task<AsynCallResult> ExitMeeting()
        {
            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            var tcs = new TaskCallback<AsynCallResult>("ExitMeeting");

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.ExitMeeting();

            if (result != 0)
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "退出会议失败！"
                });

            return tcs.Task;
        }

        public DeviceInfo[] GetDeviceList(int deviceType)
        {
            lock (_syncRoot)
            {
                if (!MeetingAgentStarted)
                    return new DeviceInfo[0];

                var deviceListPointer = IntPtr.Zero;

                try
                {
                    int maxDeviceCount = 10, getDeviceCount = 0;
                    var deviceInfoByte = Marshal.SizeOf(typeof(DeviceInfo));

                    var maxDeviceBytes = deviceInfoByte*maxDeviceCount;
                    deviceListPointer = Marshal.AllocHGlobal(maxDeviceBytes);

                    var result = MeetingAgent.GetDeviceList(deviceType, deviceListPointer, maxDeviceCount,
                        ref getDeviceCount);
                    if (result != 0)
                        return new DeviceInfo[0];

                    var getDeviceArray = new DeviceInfo[getDeviceCount];
                    for (var i = 0; i < getDeviceCount; i++)
                    {
                        var pointer = (IntPtr) (deviceListPointer.ToInt64() + i*deviceInfoByte);
                        getDeviceArray[i] = (DeviceInfo) Marshal.PtrToStructure(pointer, typeof(DeviceInfo));
                    }

                    return getDeviceArray;
                }
                catch (Exception)
                {
                    //
                    return new DeviceInfo[0];
                }
                finally
                {
                    if (deviceListPointer != IntPtr.Zero)
                        Marshal.FreeHGlobal(deviceListPointer);
                }
            }
        }

        public AsynCallResult GetMeetingList()
        {
            if (!MeetingAgentStarted)
                return new AsynCallResult {m_rc = -1, m_message = Messages.WarningMeetingServerNotStarted};

            var result = MeetingAgent.GetMeetingList();

            return new AsynCallResult
            {
                m_rc = result,
                m_message = result == 0 ? string.Empty : Messages.WarningGetMeetingListFailed
            };
        }

        public List<ParticipantInfo> GetParticipants()
        {
            var participantsPtr = IntPtr.Zero;

            if (!MeetingAgentStarted)
                return new List<ParticipantInfo>();

            try
            {
                int maxParticipantCount = 100, getParticipantCount = 0;
                var participantByte = Marshal.SizeOf(typeof(ParticipantInfo));
                var maxParticipantBytes = participantByte*maxParticipantCount;

                participantsPtr = Marshal.AllocHGlobal(maxParticipantBytes);
                var result = MeetingAgent.GetParticipantsEx(participantsPtr, ref getParticipantCount,
                    maxParticipantCount);
                if (result != 0)
                {
                }

                var participants = new List<ParticipantInfo>();
                for (var i = 0; i < getParticipantCount; i++)
                {
                    var pointer = new IntPtr(participantsPtr.ToInt64() + participantByte*i);
                    var participant =
                        (ParticipantInfo) Marshal.PtrToStructure(pointer, typeof(ParticipantInfo));
                    participants.Add(participant);
                }

                return participants;
            }
            catch (Exception ex)
            {
                return new List<ParticipantInfo>();
            }
            finally
            {
                if (participantsPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(participantsPtr);
            }
        }

        public Task<int> GetParticipantsCount()
        {
            throw new NotImplementedException();
        }

        public string GetSerialNo()
        {
            IntPtr ptr = Marshal.AllocHGlobal(24);
            string serialNo = string.Empty;
            try
            {
                int result = MeetingAgent.GetSerialNo(ptr);
                serialNo = Marshal.PtrToStringAnsi(ptr, 15);
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【get serial no exception】：{ex}");
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }

            return serialNo;
        }

        public Task<AsynCallResult> InviteParticipants(int meetingId, ContactInfo[] contactInfos, int count)
        {
            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            var tcs = new TaskCallback<AsynCallResult>("InviteParticipants");

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.InviteParticipants(meetingId, contactInfos, count);

            if (result != 0)
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc=result,
                    m_message = "邀请参与者失败！"
                });

            return tcs.Task;
        }

        public bool IsShareDocOpened()
        {
            throw new NotImplementedException();
        }

        public Task<JoinMeetingResult> JoinMeeting(int meetingId, uint[] hwnds, int count, uint[] docHwnds, int docCount)
        {
            var tcs = new TaskCallback<JoinMeetingResult>("JoinMeeting");

            if (!MeetingAgentStarted)
                return Task.FromResult(new JoinMeetingResult()
                {
                    m_result = new AsynCallResult()
                    {
                        m_rc=-1,
                        m_message = Messages.InfoMeetingSdkNotStarted
                    }
                });

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.JoinMeeting(meetingId, hwnds, count, docHwnds, docCount);

            if (result != 0)
            {
                return Task.FromResult(new JoinMeetingResult()
                {
                    m_result = new AsynCallResult()
                    {
                        m_rc = result,
                        m_message = "加入会议失败！"
                    }
                });
            }

            return tcs.Task;
        }

        public Task<AsynCallResult> QueryMeetingExist(int meetingId)
        {
            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            var tcs = new TaskCallback<AsynCallResult>("QueryMeetingExist");

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.QueryMeetingExist(meetingId);

            if (result != 0)
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc= result,
                    m_message = "查询会议失败！"
                });

            return tcs.Task;
        }

        public Task<AsynCallResult> SendUIMessage(int messageId, string pData, int dataLength, string targetPhoneId)
        {
            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            var tcs = new TaskCallback<AsynCallResult>("SendUIMessage");

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.SendUIMessage(messageId, pData, dataLength, targetPhoneId);

            if (result != 0)
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "发送透传消息失败！"
                });

            return tcs.Task;
        }

        //public void SetAudioInfo(int mainMic, int secondaryMic, int speaker, string mainMicName, string secondaryMicName, string speakerName, int sampleRate, int bitRate)
        //{
        //    throw new NotImplementedException();
        //}

        //public void SetDefaultCamera(string cameraName)
        //{
        //    throw new NotImplementedException();
        //}

        public Task<AsynCallResult> SetDoulbeScreenRender(string phoneId, int mediaType, int doubleScreenRenderTrigger,
            IntPtr displayWindowIntPtr)
        {
            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            var tcs = new TaskCallback<AsynCallResult>("SetDoulbeScreenRender");

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.SetDoubleScreenRender(phoneId, mediaType, doubleScreenRenderTrigger,
                displayWindowIntPtr);

            if (result != 0)
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc=result,
                    m_message = "设置双屏失败！"
                });

            return tcs.Task;
        }

        public void SetDefaultDevice(int deviceType, string deviceName)
        {
            if (!MeetingAgentStarted)
                return;

            var result = MeetingAgent.SetDefaultDevice(deviceType, deviceName);
            //
        }

        public void SetVideoResolution(int videoType, int width, int height)
        {
            if (!MeetingAgentStarted)
                return;

            var result = MeetingAgent.SetVideoCapResolution(videoType, width, height);
            //
        }

        public void SetVideoBitRate(int videoType, int bitRate)
        {
            if (!MeetingAgentStarted)
                return;

            var result = MeetingAgent.SetVideoCapBitRate(videoType, bitRate);
            //
        }

        public void SetAudioSampleRate(int sampleRate)
        {
            if (!MeetingAgentStarted)
                return;

            var result = MeetingAgent.SetAudioCapSampleRate(sampleRate);
            //
        }

        public void SetAudioBitRate(int bitRate)
        {
            if (!MeetingAgentStarted)
                return;

            var result = MeetingAgent.SetAudioCapBitRate(bitRate);
            //
        }

        //public void SetVideoInfo(int mainCamera, int secondaryCamera, string mainCameraName, int mainWidth, int mainHeight, int mainBitRate, string secondaryCameraName, int secondaryWidth, int secondaryHeight, int secondaryBitRate)
        //{
        //    if (!MeetingAgentStarted)
        //    {
        //        //
        //        return;
        //    }
        //    int result = 0;

        //    result = MeetingAgent.SetDefaultDevice(mainCamera, mainCameraName);
        //    //
        //    result = MeetingAgent.SetVideoCapResolution(mainCamera, mainWidth, mainHeight);
        //    //
        //    result = MeetingAgent.SetVideoCapBitRate(mainCamera, mainBitRate);
        //    //

        //    result = MeetingAgent.SetDefaultDevice(secondaryCamera, secondaryCameraName);
        //    //
        //    result = MeetingAgent.SetVideoCapResolution(secondaryCamera, secondaryWidth, secondaryHeight);
        //    //
        //    result = MeetingAgent.SetVideoCapBitRate(secondaryCamera, secondaryBitRate);
        //    //
        //}

        public void SetViewDisplayName(IntPtr viewHwnd, string name)
        {
            if (!MeetingAgentStarted)
                return;
            MeetingAgent.SetViewDisplayName(viewHwnd, name);
        }

        public void SetViewPosition(IntPtr viewHwnd, ViewRect viewRect)
        {
            if (!MeetingAgentStarted)
                return;

            int result;
            result = MeetingAgent.SetViewPosition(viewHwnd, viewRect);
            result = MeetingAgent.SetViewVisile(viewHwnd, 1);
        }

        public async Task SetViewVisible(IntPtr viewHwnd, int visible)
        {
            await Task.Run(() =>
            {
                if (!MeetingAgentStarted)
                    return;

                var result = MeetingAgent.SetViewVisile(viewHwnd, visible);
            });
        }

        public void ShowQosView()
        {
            throw new NotImplementedException();
        }

        public Task<StartResult> Start(UserInfo userInfo)
        {
            if (!_hash.ContainsKey("Start"))
                lock (_syncRoot)
                {
                    var tcs = new TaskCallback<StartResult>("Start");
                    _hash.Add(tcs.Name, tcs);

                    //var stopResult = MeetingAgent.Stop();

                    //if (stopResult == 0)
                    //    MeetingAgentStarted = false;

                    Log.Logger.Debug($"【start meeting server begins, is meeting server started】：{MeetingAgentStarted}");

                    if (MeetingAgentStarted)
                        return
                            Task.FromResult(new StartResult
                            {
                                m_result =
                                    new AsynCallResult
                                    {
                                        m_rc = -1,
                                        m_message = Messages.WarningMeetingServerAlreadyStarted
                                    }
                            });

                    if (_callbackFunc == null)
                        _callbackFunc = CallbackHandler;

                    if (string.IsNullOrEmpty(userInfo.GetNube()))
                    {
                        return Task.FromResult(new StartResult()
                        {
                            m_result = new AsynCallResult()
                            {
                                m_rc = -1,
                                m_message = $"{Messages.InfoNubeNotRegistered}\r\n本机设备号：{GlobalData.Instance.SerialNo}"
                            }
                        });
                    }

                    Log.Logger.Debug($"【start meeting server begins】：appkey={userInfo.AppKey}, openId={userInfo.OpenId}, nube={userInfo.GetNube()}");

                    var result = MeetingAgent.Start(_callbackFunc, userInfo.AppKey, userInfo.OpenId, userInfo.GetNube(),
                        "http://xmeeting.butel.com/nps_x1/");

                    if (result != 0)
                    {
                        return Task.FromResult(new StartResult()
                        {
                            m_result = new AsynCallResult()
                            {
                                m_rc = -1,
                                m_message = Messages.InfoMeetingSdkStartedFailed
                            }
                        });
                    }

                    return tcs.Task;
                }

            return Task.FromResult(new StartResult());
        }

        public Task<StartLiveStreamResult> StartLiveStream(LiveParam liveParam, LiveVideoStreamInfo[] streamInfos,
            int count)
        {
            if (!MeetingAgentStarted)
                return Task.FromResult(new StartLiveStreamResult());

            var tcs = new TaskCallback<StartLiveStreamResult>("StartLiveStream");

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.StartLiveStream(liveParam, streamInfos, count);

            if (result != 0)
                return Task.FromResult(new StartLiveStreamResult()
                {
                    m_result = new AsynCallResult()
                    {
                        m_rc = result,
                        m_message = "推流失败！"
                    }
                });

            return tcs.Task;
        }

        public Task<AsynCallResult> UpdateLiveVideoStreams(int liveId, LiveVideoStreamInfo[] streamsInfos, int count)
        {
            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            var tcs = new TaskCallback<AsynCallResult>("UpdateLiveVideoStreams");

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.UpdateLiveVideoStreams(liveId, streamsInfos, count);

            if (result != 0)
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "更新流失败！"
                });

            return tcs.Task;
        }


        public Task<AsynCallResult> StartShareDoc()
        {
            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            var tcs = new TaskCallback<AsynCallResult>("StartShareDoc");

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.StartShareDoc();

            if (result != 0)
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc=result,
                    m_message = "共享数据失败！"
                });

            return tcs.Task;
        }

        public int Stop()
        {
            Log.Logger.Debug("【stop meeting server begins】");
            int result = MeetingAgent.Stop();
            Log.Logger.Debug($"【stop meeting server result】：result={result}");
            if (result == 0)
            {
                MeetingAgentStarted = false;
            }
            return result;
        }

        public Task<AsynCallResult> StopLiveStream(int liveId)
        {
            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            var tcs = new TaskCallback<AsynCallResult>("StopLiveStream");

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.StopLiveStream(liveId);

            if (result != 0)
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc=result,
                    m_message = "停止推流失败！"
                });

            return tcs.Task;
        }

        public Task<AsynCallResult> StopShareDoc()
        {
            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            var tcs = new TaskCallback<AsynCallResult>("StopShareDoc");

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.StopShareDoc();

            if (result != 0)
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "停止共享数据失败！"
                });

            return tcs.Task;
        }

        public async Task<bool> StopSpeak()
        {
            var finalResult = await Task.Run(() =>
            {
                if (!MeetingAgentStarted)
                    return false;

                var result = MeetingAgent.StopSpeak();

                return result == 0;
            });
            return finalResult;
        }

        public Task<MonitorBroadcastResult> StartMonitorBroadcast(LiveParam liveParam)
        {
            if (!MeetingAgentStarted)
                return Task.FromResult(new MonitorBroadcastResult());

            var tcs = new TaskCallback<MonitorBroadcastResult>("StartMonitorBroadcast");

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.StartMonitorBroadcast(liveParam);

            if (result != 0)
                return Task.FromResult(new MonitorBroadcastResult()
                {
                    m_result = new AsynCallResult()
                    {
                        m_message = "监控推流失败！",
                        m_rc = result
                    }
                });

            return tcs.Task;
        }

        public Task<AsynCallResult> StopMonitorBroadcast(int streamId)
        {
            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            var tcs = new TaskCallback<AsynCallResult>("StopMonitorBroadcast");

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.StopMonitorBroadcast(streamId);

            if (result != 0)
                return Task.FromResult(new AsynCallResult()
                {
                    m_message = "停止监控推流失败！",
                    m_rc = result
                });

            return tcs.Task;
        }

        //public void OnInternalMessagePassThrough(string internalMsg)
        //{
        //    InternalMessagePassThroughEvent?.Invoke(internalMsg);
        //}

        public Task<AsynCallResult> HostKickoutUser(string userPhoneId)
        {
            var tcs = new TaskCallback<AsynCallResult>("HostKickoutUser");

            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.HostKickoutUser(userPhoneId);

            if (result != 0)
            {
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "踢除参与者失败！"
                });
            }

            return tcs.Task;
        }

        public Task<AsynCallResult> OpenCamera(string cameraName)
        {
            var tcs = new TaskCallback<AsynCallResult>("OpenCamera");

            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.OpenCamera(cameraName);

            if (result != 0)
            {
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "打开摄像头失败！"
                });
            }

            return tcs.Task;
        }

        public Task<AsynCallResult> CloseCamera()
        {
            var tcs = new TaskCallback<AsynCallResult>("CloseCamera");

            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.CloseCamera();

            if (result != 0)
            {
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "关闭摄像头失败！"
                });
            }

            return tcs.Task;
        }

        public Task<AsynCallResult> OpenSharedCamera(string cameraName)
        {
            var tcs = new TaskCallback<AsynCallResult>("OpenSharedCamera");

            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.OpenSharedCamera(cameraName);

            if (result != 0)
            {
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "打开数据摄像头失败！"
                });
            }

            return tcs.Task;
        }

        public Task<AsynCallResult> CloseSharedCamera()
        {
            var tcs = new TaskCallback<AsynCallResult>("CloseSharedCamera");

            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.CloseSharedCamera();

            if (result != 0)
            {
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "关闭数据摄像头失败！"
                });
            }

            return tcs.Task;
        }

        /// <summary>
        /// </summary>
        /// <param name="type">1：表示人像 2：表示数据</param>
        /// <param name="cameraName"></param>
        /// <returns></returns>
        public Task<AsynCallResult> SetDefaultCamera(int type, string cameraName)
        {
            var tcs = new TaskCallback<AsynCallResult>("SetDefaultCamera");

            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.SetDefaultCamera(type, cameraName);

            if (result != 0)
            {
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "设置默认摄像头失败！"
                });
            }

            return tcs.Task;
        }

        public int ShowCameraProtityPage(string cameraName)
        {
            var result = MeetingAgent.ShowCameraProtityPage(cameraName);
            return result;
        }

        /// <summary>
        ///     设置视频窗口绘制填充模式（启动sdk成功后调用）
        /// </summary>
        /// <param name="fillMode">0 保持原始图片直接显示模式, 1：保持原始图片裁剪之后拉伸模式, 4：原始图片无条件拉伸模式</param>
        /// <returns></returns>
        public int SetFillMode(int fillMode)
        {
            var result = MeetingAgent.SetFillMode(fillMode);
            return result;
        }


        public VideoDeviceInfo GetVideoDeviceInfos(string cameraName)
        {
            var videoDeviceInfoIntPtr = IntPtr.Zero;

            try
            {
                var devInfoByte = Marshal.SizeOf(typeof(VideoDeviceInfo));

                videoDeviceInfoIntPtr = Marshal.AllocHGlobal(devInfoByte);

                var result = MeetingAgent.GetCameraInfo(cameraName, videoDeviceInfoIntPtr);


                var pointer = new IntPtr(videoDeviceInfoIntPtr.ToInt64());

                var videoDeviceInfo =
                    (VideoDeviceInfo) Marshal.PtrToStructure(pointer, typeof(VideoDeviceInfo));

                return videoDeviceInfo;
            }
            catch (Exception ex)
            {
                return new VideoDeviceInfo();
            }
            finally
            {
                if (videoDeviceInfoIntPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(videoDeviceInfoIntPtr);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="muteState">0：取消静音  1：静音</param>
        /// <returns></returns>
        public Task<AsynCallResult> SetMicMuteState(int muteState)
        {
            var tcs = new TaskCallback<AsynCallResult>("SetMicMuteState");

            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.SetMicMuteState(muteState);
            if (result != 0)
            {
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "设置禁言状态失败！"
                });
            }
            return tcs.Task;
        }

        public Task<AsynCallResult> StartScreenSharing()
        {
            var tcs = new TaskCallback<AsynCallResult>("StartScreenSharing");

            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.StartScreenSharing();
            if (result != 0)
            {
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "共享桌面失败！"
                });
            }

            return tcs.Task;
        }

        public Task<AsynCallResult> StopScreenSharing()
        {
            var tcs = new TaskCallback<AsynCallResult>("StopScreenSharing");

            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.StopScreenSharing();

            if (result != 0)
            {
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "停止共享桌面失败！"
                });
            }

            return tcs.Task;
        }

        public Task<AsynCallResult> RequireUserSpeak(string phoneId)
        {
            var tcs = new TaskCallback<AsynCallResult>("RequireUserSpeak");

            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.RequireUserSpeak(phoneId);
            if (result != 0)
            {
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "指定发言失败！"
                });
            }

            return tcs.Task;
        }

        public Task<AsynCallResult> RequireUserStopSpeak(string phoneId)
        {
            var tcs = new TaskCallback<AsynCallResult>("RequireUserStopSpeak");

            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.RequireUserStopSpeak(phoneId);
            if (result != 0)
            {
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "指定禁言失败！"
                });
            }

            return tcs.Task;
        }

        public async Task BanToSpeak(string[] phoneIds)
        {
            if ((phoneIds == null) || (phoneIds.Length == 0))
                return;

            if (!MeetingAgentStarted)
                return;

            var participantInfos = GetParticipants();

            foreach (var phoneId in phoneIds)
            {
                await SendUIMessage((int) UiMessage.BannedToSpeak, UiMessage.BannedToSpeak.ToString(),
                    UiMessage.BannedToSpeak.ToString().Length, phoneId);

                if (participantInfos.Any(p => (p.m_bIsSpeaking == 1) && (p.m_contactInfo.m_szPhoneId == phoneId)))
                    await RequireUserStopSpeak(phoneId);
            }
        }

        public async Task AllowToSpeak(string[] phoneIds)
        {
            if ((phoneIds == null) || (phoneIds.Length == 0))
                return;

            if (!MeetingAgentStarted)
                return;

            foreach (var phoneId in phoneIds)
                await SendUIMessage((int) UiMessage.AllowToSpeak, UiMessage.AllowToSpeak.ToString(),
                    UiMessage.AllowToSpeak.ToString().Length, phoneId);
        }

        public Task<LocalRecordResult> StartRecord(string fileName, LiveVideoStreamInfo[] streamsInfos, int count)
        {
            var tcs = new TaskCallback<LocalRecordResult>("StartRecord");

            if (!MeetingAgentStarted)
                return Task.FromResult(new LocalRecordResult());

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.StartRecord(fileName, streamsInfos, count);
            if (result != 0)
            {
                return Task.FromResult(new LocalRecordResult()
                {
                    m_result = new AsynCallResult()
                    {
                        m_rc = result,
                        m_message = "开始录制失败！"
                    }
                });
            }

            return tcs.Task;
        }

        public Task<AsynCallResult> StopRecord()
        {
            var tcs = new TaskCallback<AsynCallResult>("StopRecord");

            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.StopRecord();
            if (result != 0)
            {
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "停止录制失败！"
                });
            }

            return tcs.Task;
        }

        public async Task<int> SetRecordDirectory(string recordDir)
        {
            var result = await Task.Run(() => MeetingAgent.SetRecordDirectory(recordDir));
            return result;
        }

        public Task<AsynCallResult> SetRecordParam(RecordParam recordParam)
        {
            var tcs = new TaskCallback<AsynCallResult>("SetRecordParam");

            if (!MeetingAgentStarted)
                return Task.FromResult(new AsynCallResult());

            if (_hash.ContainsKey(tcs.Name))
                _hash.Remove(tcs.Name);

            _hash.Add(tcs.Name, tcs);

            var result = MeetingAgent.SetRecordParam(recordParam);
            if (result != 0)
            {
                return Task.FromResult(new AsynCallResult()
                {
                    m_rc = result,
                    m_message = "设置录制参数失败！"
                });
            }
            return tcs.Task;
        }

        public Task<GetvideoParamResult> GetVideoStreamsParam()
        {
            var tcs = new TaskCallback<GetvideoParamResult>("GetVideoParam");

            if (!MeetingAgentStarted)
            {
                return Task.FromResult(new GetvideoParamResult());
            }

            if (_hash.ContainsKey(tcs.Name))
            {
                _hash.Remove(tcs.Name);
            }

            var result = MeetingAgent.GetVideoStreamsParam();
            if (result != 0)
            {
                return Task.FromResult(new GetvideoParamResult()
                {
                    m_count = 0,
                    m_VideoParams = new VideoStreamParam[] {}
                });
            }

            return tcs.Task;
        }


        //methods for handling callbacks
        private void CallbackHandler(int cmdId, IntPtr pData, int dataLen, long ctx)
        {
            lock (_syncRoot)
            {
                var callbackType = (ENUM_CALLBACK_CMD_TYPE) cmdId;
                Log.Logger.Debug($"【sdk callback】：callbackType={callbackType}");
                switch (callbackType)
                {
                    case ENUM_CALLBACK_CMD_TYPE.enum_startup_result:
                        StartUpCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_createMeeting_result:
                        CreateMeetingCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_invite_result:
                        InviteCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_joinmeeting_result:
                        JoinMeetingCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_applyspeak_result:
                        ApplyToSpeakCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_exitmeeting_result:
                        ExitMeetingCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_getmeetinglist_result:
                        GetMeetingListCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_queryname_result:
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_modify_name_result:
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_senduimessage_result:
                        SendUIMessageCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_recive_invitation:
                        ReceiveInvitationCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_view_created:
                        ViewCreateCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_view_closed:
                        ViewCloseCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_startspeak:
                        //when participants start speaking:
                        StartSpeakCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_stopspeak:
                        StopSpeakCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_other_startspeak:
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_other_stopspeak:
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_other_joinmeeting:
                        OtherJoinMeetingCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_other_exitmeeting:
                        OtherExitMeetingCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_other_message:
                        UIMessageReceivedCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_set_double_screen_render:
                        SetDoulbeScreenRenderCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_error_message:
                        ErrorMsgReceivedCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_verifymeetexist_result:
                        QueryMeetingExistCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_open_doc_result:
                        StartShareDocCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_close_doc_result:
                        StopShareDocCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_start_broadcast_result:
                        StartLiveStreamCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_stop_broadcast_result:
                        StopLiveStreamCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_create_datemeeting_result:
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_host_kick_user_result:
                        HostKickoutUserCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_kicked_by_host:
                        KickedByHostCallback(pData);
                        break;

                    case ENUM_CALLBACK_CMD_TYPE.enum_close_camera_result:
                        CloseCameraCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_close_data_camera_result:
                        CloseDataCameraCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_open_camera_result:
                        OpenCameraCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_open_data_camera_result:
                        OpenDataCameraCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_set_default_camera_result:
                        SetDefaultCameraCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_set_mute_state_result:
                        SetMicMuteStateCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_start_screensharing_result:
                        StartScreenSharingCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_stop_screensharing_result:
                        StopScreenSharingCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_req_user_speak_result:
                        RequireUserSpeakCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_req_user_stopspeak_result:
                        RequireUserStopSpeakCallback(pData);
                        break;

                    case ENUM_CALLBACK_CMD_TYPE.enum_start_record_result:
                        StartRecordCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_stop_record_result:
                        StopRecordCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_set_record_param_result:
                        SetRecordParamCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_update_live_layout_result:
                        UpdateLiveVideoStreamsCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_start_monitor_result:
                        StartMonitorBroadcastCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_stop_monitor_result:
                        StopMonitorBroadcastCallback(pData);
                        break;
                    case ENUM_CALLBACK_CMD_TYPE.enum_get_video_param_result:
                        GetVideoParamCallback(pData);
                        break;

                    case ENUM_CALLBACK_CMD_TYPE.enum_check_disk_space:
                        DiskSpaceNotEnoughNotify(pData);
                        break;

                    //case ENUM_CALLBACK_CMD_TYPE.enum_device_update_result:
                    //    DeviceUpdateCallback(pData);
                    //    break;
                    default:
                        break;
                }
            }
        }

        private void DiskSpaceNotEnoughNotify(IntPtr pData)
        {
            var diskSpaceNotEnough = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            DiskSpaceNotEnough?.Invoke(diskSpaceNotEnough);
        }

        private void GetVideoParamCallback(IntPtr pData)
        {
            GetvideoParamResult result = Marshal.PtrToStructure<GetvideoParamResult>(pData);
            SetResult("GetVideoParam",result);
        }

        private void SetDoulbeScreenRenderCallback(IntPtr pData)
        {
            var setDoulbeScreenRenderResult =
                (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("SetDoulbeScreenRender", setDoulbeScreenRenderResult);
        }

        private void UpdateLiveVideoStreamsCallback(IntPtr pData)
        {
            var updateLiveVideoStreamsResult =
                (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("UpdateLiveVideoStreams", updateLiveVideoStreamsResult);
        }

        private void StartLiveStreamCallback(IntPtr pData)
        {
            var startLiveStreamResult =
                (StartLiveStreamResult) Marshal.PtrToStructure(pData, typeof(StartLiveStreamResult));

            SetResult("StartLiveStream", startLiveStreamResult);
        }

        private void StartMonitorBroadcastCallback(IntPtr pData)
        {
            var startMonoitorBroadcastResult =
                (MonitorBroadcastResult) Marshal.PtrToStructure(pData, typeof(MonitorBroadcastResult));

            SetResult("StartMonitorBroadcast", startMonoitorBroadcastResult);
        }

        private void StopMonitorBroadcastCallback(IntPtr pData)
        {
            var stopMonoitorBroadcastResult =
                (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("StopMonitorBroadcast", stopMonoitorBroadcastResult);
        }


        private void StopLiveStreamCallback(IntPtr pData)
        {
            var stopLiveStreamResult = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("StopLiveStream", stopLiveStreamResult);
        }


        private void SetRecordParamCallback(IntPtr pData)
        {
            var setRecordParamResult = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("SetRecordParam", setRecordParamResult);
        }

        private void OpenCameraCallback(IntPtr pData)
        {
            var openCameraResult = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("OpenCamera", openCameraResult);
        }

        private void CloseCameraCallback(IntPtr pData)
        {
            var closeCameraResult = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("CloseCamera", closeCameraResult);
        }

        private void OpenDataCameraCallback(IntPtr pData)
        {
            var openDataCameraResult = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("OpenSharedCamera", openDataCameraResult);
        }

        private void CloseDataCameraCallback(IntPtr pData)
        {
            var closeDataCameraResult =
                (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("CloseSharedCamera", closeDataCameraResult);
        }

        private void SetDefaultCameraCallback(IntPtr pData)
        {
            var setDefaultCameraResult =
                (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("SetDefaultCamera", setDefaultCameraResult);
        }

        private void HostKickoutUserCallback(IntPtr pData)
        {
            var hostKickoutUserResult =
                (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("HostKickoutUser", hostKickoutUserResult);
        }

        private void KickedByHostCallback(IntPtr pData)
        {
            var kickedByHostResult = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            KickedByHostEvent?.Invoke(kickedByHostResult);
        }

        private void SetMicMuteStateCallback(IntPtr pData)
        {
            var setMicMuteStateResult =
                (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("SetMicMuteState", setMicMuteStateResult);
        }

        private void StartScreenSharingCallback(IntPtr pData)
        {
            var startScreenSharingResult =
                (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("StartScreenSharing", startScreenSharingResult);
        }

        private void StopScreenSharingCallback(IntPtr pData)
        {
            var stopScreenSharingResult =
                (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("StopScreenSharing", stopScreenSharingResult);
        }

        private void RequireUserSpeakCallback(IntPtr pData)
        {
            var requireUserSpeakResult =
                (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("RequireUserSpeak", requireUserSpeakResult);
        }

        private void RequireUserStopSpeakCallback(IntPtr pData)
        {
            var requireUserStopSpeakResult =
                (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("RequireUserStopSpeak", requireUserStopSpeakResult);
        }

        private void StartRecordCallback(IntPtr pData)
        {
            var startRecrodResult =
                (LocalRecordResult) Marshal.PtrToStructure(pData, typeof(LocalRecordResult));

            SetResult("StartRecord", startRecrodResult);
        }

        private void StopRecordCallback(IntPtr pData)
        {
            var stopRecordkResult = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));

            SetResult("StopRecord", stopRecordkResult);
        }


        private void StartUpCallback(IntPtr pData)
        {
            var startResult = (StartResult) Marshal.PtrToStructure(pData, typeof(StartResult));

            MeetingAgentStarted = startResult.m_result.m_rc == 0;

            Log.Logger.Debug($"【in startup callback, is meeting server started】：{MeetingAgentStarted}");

            //SelfName = startResult.m_selfInfo.m_szDisplayName;
            //SelfPhoneId = startResult.m_selfInfo.m_szPhoneId;

            SetResult("Start", startResult);
        }

        private void CreateMeetingCallback(IntPtr pData)
        {
            var createMeetingResult =
                (CreateMeetingResult) Marshal.PtrToStructure(pData, typeof(CreateMeetingResult));
            MeetingId = createMeetingResult.m_meetingId;
            SetResult("CreateMeeting", createMeetingResult);
        }

        private void JoinMeetingCallback(IntPtr pData)
        {
            var joinMeetingResult =
                (JoinMeetingResult) Marshal.PtrToStructure(pData, typeof(JoinMeetingResult));

            if (!string.IsNullOrEmpty(joinMeetingResult.m_meetingInfo.m_szTeacherPhoneId))
                TeacherPhoneId = joinMeetingResult.m_meetingInfo.m_szTeacherPhoneId;

            SetResult("JoinMeeting", joinMeetingResult);
        }

        private void ViewCreateCallback(IntPtr pData)
        {
            var speakerView = (SpeakerView) Marshal.PtrToStructure(pData, typeof(SpeakerView));

            ViewCreateEvent?.Invoke(speakerView);
        }

        private void ViewCloseCallback(IntPtr pData)
        {
            var speakerView = (SpeakerView) Marshal.PtrToStructure(pData, typeof(SpeakerView));

            ViewCloseEvent?.Invoke(speakerView);
        }

        private void StartSpeakCallback(IntPtr pData)
        {
            StartSpeakEvent?.Invoke();
        }

        private void ApplyToSpeakCallback(IntPtr pData)
        {
            var applyToSpeakResult = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));
            SetResult("ApplyToSpeak", applyToSpeakResult);
        }

        private void StopSpeakCallback(IntPtr pData)
        {
            StopSpeakEvent?.Invoke();
        }

        private void StartShareDocCallback(IntPtr pData)
        {
            var startShareDocResult = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));
            SetResult("StartShareDoc", startShareDocResult);
        }

        private void StopShareDocCallback(IntPtr pData)
        {
            var stopShareDocResult = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));
            SetResult("StopShareDoc", stopShareDocResult);
        }

        private void ExitMeetingCallback(IntPtr pData)
        {
            var exitMeetingResult = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));
            SetResult("ExitMeeting", exitMeetingResult);
            ExitMeetingEvent?.Invoke();
        }

        private void QueryMeetingExistCallback(IntPtr pData)
        {
            var queryMeetingExistResult =
                (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));
            SetResult("QueryMeetingExist", queryMeetingExistResult);
        }

        private void GetMeetingListCallback(IntPtr pData)
        {
            var getMeetingListResult =
                (GetMeetingListResult) Marshal.PtrToStructure(pData, typeof(GetMeetingListResult));

            //SetResult("GetMeetingList", getMeetingListResult);
            GetMeetingListEvent?.Invoke(getMeetingListResult);
        }

        private void OtherJoinMeetingCallback(IntPtr pData)
        {
            var contactInfo = (ContactInfo) Marshal.PtrToStructure(pData, typeof(ContactInfo));
            OtherJoinMeetingEvent?.Invoke(contactInfo);
        }

        private void OtherExitMeetingCallback(IntPtr pData)
        {
            var contactInfo = (ContactInfo) Marshal.PtrToStructure(pData, typeof(ContactInfo));
            OtherExitMeetingEvent?.Invoke(contactInfo);
        }

        private void UIMessageReceivedCallback(IntPtr pData)
        {
            var message = (UIMessage) Marshal.PtrToStructure(pData, typeof(UIMessage));
            UIMessageReceivedEvent?.Invoke(message);
        }

        private void SendUIMessageCallback(IntPtr pData)
        {
            var sendUIMsgResult = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));
            SetResult("SendUIMessage", sendUIMsgResult);
        }

        private void ErrorMsgReceivedCallback(IntPtr pData)
        {
            var errorMsgResult = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));
            ErrorMsgReceivedEvent?.Invoke(errorMsgResult);
        }

        private void InviteCallback(IntPtr pData)
        {
            var inviteResult = (AsynCallResult) Marshal.PtrToStructure(pData, typeof(AsynCallResult));
            SetResult("InviteParticipants", inviteResult);
            //may needs to trigger an event when inviting someone while creating a meeting?
        }

        private void ReceiveInvitationCallback(IntPtr pData)
        {
            var invitation = (MeetingInvitation) Marshal.PtrToStructure(pData, typeof(MeetingInvitation));
            InvitationReceivedEvent?.Invoke(invitation);
        }

        private void SetResult(string name, object result)
        {
            if (_hash.ContainsKey(name))
            {
                _hash[name].SetResult(result);
                _hash.Remove(name);
            }
        }
    }
}
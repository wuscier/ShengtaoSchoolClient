using Autofac;
using Prism.Commands;
using St.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using MenuItem = System.Windows.Controls.MenuItem;
using Serilog;

namespace St.Meeting
{
    public class MeetingViewModel : ViewModelBase,IExitMeeting
    {
        public MeetingViewModel(MeetingView meetingView, Action<bool, string> startMeetingCallback,
            Action<bool, string> exitMeetingCallback)
        {
            _meetingView = meetingView;

            _viewLayoutService = DependencyResolver.Current.Container.Resolve<IViewLayout>();
            _viewLayoutService.ViewFrameList = InitializeViewFrameList(meetingView);

            _sdkService = DependencyResolver.Current.Container.Resolve<ISdk>();
            _bmsService = DependencyResolver.Current.Container.Resolve<IBms>();

            _localPushLiveService = DependencyResolver.Current.Container.ResolveNamed<IPushLive>(GlobalResources.LocalPushLive);
            _localPushLiveService.ResetStatus();
            _serverPushLiveService = DependencyResolver.Current.Container.ResolveNamed<IPushLive>(GlobalResources.RemotePushLive);
            _serverPushLiveService.ResetStatus();
            _localRecordService = DependencyResolver.Current.Container.Resolve<IRecord>();
            _localRecordService.ResetStatus();

            _startMeetingCallbackEvent = startMeetingCallback;
            _exitMeetingCallbackEvent = exitMeetingCallback;

            MeetingId = _sdkService.MeetingId;
            SpeakingStatus = IsNotSpeaking;
            SelfDescription = $"{_sdkService.SelfName}-{_sdkService.SelfPhoneId}";

            _lessonDetail = DependencyResolver.Current.Container.Resolve<LessonDetail>();
            _userInfo = DependencyResolver.Current.Container.Resolve<UserInfo>();
            _userInfos = DependencyResolver.Current.Container.Resolve<List<UserInfo>>();

            MeetingOrLesson = _lessonDetail.Id == 0 ? "会议号:" : "课程号:";
            LessonName = string.IsNullOrEmpty(_lessonDetail.Name)
                ? string.Empty
                : string.Format($"课程名:{_lessonDetail.Name}");

            LoadCommand = DelegateCommand.FromAsyncHandler(JoinMeetingAsync);
            ModeChangedCommand = DelegateCommand<string>.FromAsyncHandler(MeetingModeChangedAsync);
            SpeakingStatusChangedCommand = DelegateCommand.FromAsyncHandler(SpeakingStatusChangedAsync);
            ExternalDataChangedCommand = DelegateCommand<string>.FromAsyncHandler(ExternalDataChangedAsync);
            SharingDesktopCommand = DelegateCommand.FromAsyncHandler(SharingDesktopAsync);
            CancelSharingCommand = DelegateCommand.FromAsyncHandler(CancelSharingAsync);
            ExitCommand = DelegateCommand.FromAsyncHandler(ExitAsync);
            OpenExitDialogCommand = DelegateCommand.FromAsyncHandler(OpenExitDialogAsync);
            CancelCommand = DelegateCommand.FromAsyncHandler(CancelAsync);
            KickoutCommand = DelegateCommand<string>.FromAsyncHandler(KickoutAsync);
            OpenCloseCameraCommand = DelegateCommand.FromAsyncHandler(OpenCloseCameraAsync);
            GetCameraInfoCommand = DelegateCommand<string>.FromAsyncHandler(GetCameraInfoAsync);
            OpenPropertyPageCommand = DelegateCommand<string>.FromAsyncHandler(OpenPropertyPageAsync);
            SetDefaultDataCameraCommand = DelegateCommand<string>.FromAsyncHandler(SetDefaultDataCameraAsync);
            SetDefaultFigureCameraCommand = DelegateCommand<string>.FromAsyncHandler(SetDefaultFigureCameraAsync);
            SetMicStateCommand = DelegateCommand.FromAsyncHandler(SetMicStateAsync);
            ScreenShareCommand = DelegateCommand.FromAsyncHandler(ScreenShareAsync);
            StartSpeakCommand = DelegateCommand<string>.FromAsyncHandler(StartSpeakAsync);
            StopSpeakCommand = DelegateCommand<string>.FromAsyncHandler(StopSpeakAsync);
            BanToSpeakCommand = DelegateCommand<string>.FromAsyncHandler(BanToSpeakAsync);
            AllowToSpeakCommand = DelegateCommand<string>.FromAsyncHandler(AllowToSpeakAsync);
            RecordCommand = DelegateCommand.FromAsyncHandler(RecordAsync);
            PushLiveCommand = DelegateCommand.FromAsyncHandler(PushLiveAsync);
            TopMostTriggerCommand = new DelegateCommand(TriggerTopMost);
            ShowLogCommand = DelegateCommand.FromAsyncHandler(ShowLogAsync);
            TriggerMenuCommand = new DelegateCommand(TriggerMenu);
            InitializeMenuItems();
            RegisterMeetingEvents();
        }

        private void TriggerMenu()
        {
            IsMenuOpen = !IsMenuOpen;
        }

        private async Task ShowLogAsync()
        {
            await LogManager.ShowLogAsync();
        }

        private void TriggerTopMost()
        {
            _meetingView.Topmost = !_meetingView.Topmost;
        }

        #region private fields

        private readonly MeetingView _meetingView;

        private delegate Task TaskDelegate();

        private TaskDelegate _cancelSharingAction;

        private readonly IViewLayout _viewLayoutService;
        private readonly ISdk _sdkService;
        private readonly IBms _bmsService;
        private readonly IPushLive _localPushLiveService;
        private readonly IPushLive _serverPushLiveService;
        private readonly IRecord _localRecordService;
        private readonly LessonDetail _lessonDetail;
        private readonly UserInfo _userInfo;
        private readonly List<UserInfo> _userInfos;

        private readonly Action<bool, string> _startMeetingCallbackEvent;
        private readonly Action<bool, string> _exitMeetingCallbackEvent;
        private const string IsSpeaking = "取消发言";
        private const string IsNotSpeaking = "发 言";

        #endregion

        #region public properties

        public ViewFrame ViewFrame1 { get; private set; }
        public ViewFrame ViewFrame2 { get; private set; }
        public ViewFrame ViewFrame3 { get; private set; }
        public ViewFrame ViewFrame4 { get; private set; }
        public ViewFrame ViewFrame5 { get; private set; }

        public ObservableCollection<MenuItem> ModeMenuItems { get; set; }
        public ObservableCollection<MenuItem> LayoutMenuItems { get; set; }
        public ObservableCollection<MenuItem> SharingMenuItems { get; set; }


        private string _meetingOrLesson;

        public string MeetingOrLesson
        {
            get { return _meetingOrLesson; }
            set { SetProperty(ref _meetingOrLesson, value); }
        }

        private string _lessonName;

        public string LessonName
        {
            get { return _lessonName; }
            set { SetProperty(ref _lessonName, value); }
        }


        private string _selfDescription;

        public string SelfDescription
        {
            get { return _selfDescription; }
            set { SetProperty(ref _selfDescription, value); }
        }

        private int _meetingId;

        public int MeetingId
        {
            get { return _meetingId; }
            set { SetProperty(ref _meetingId, value); }
        }


        private string _pushLiveStreamTips;

        public string PushLiveStreamTips
        {
            get { return _pushLiveStreamTips; }
            set { SetProperty(ref _pushLiveStreamTips, value); }
        }

        private string _recordTips;

        public string RecordTips
        {
            get { return _recordTips; }
            set { SetProperty(ref _recordTips, value); }
        }

        private string _selectedCamera;

        public string SelectedCamera
        {
            get { return _selectedCamera; }
            set { SetProperty(ref _selectedCamera, value); }
        }

        private string _openCloseCameraOperation = "open camera";

        public string OpenCloseCameraOperation
        {
            get { return _openCloseCameraOperation; }
            set { SetProperty(ref _openCloseCameraOperation, value); }
        }

        private string _openCloseDataOperation = "open data";

        public string OpenCloseDataOperation
        {
            get { return _openCloseDataOperation; }
            set { SetProperty(ref _openCloseDataOperation, value); }
        }

        private string _micState = "静音";

        public string MicState
        {
            get { return _micState; }
            set { SetProperty(ref _micState, value); }
        }

        private string _screenShareState = "共享屏幕";

        public string ScreenShareState
        {
            get { return _screenShareState; }
            set { SetProperty(ref _screenShareState, value); }
        }

        private string _phoneId;

        public string PhoneId
        {
            get { return _phoneId; }
            set { SetProperty(ref _phoneId, value); }
        }

        private string _startStopSpeakOperation = "发言";

        public string StartStopSpeakOperation
        {
            get { return _startStopSpeakOperation; }
            set { SetProperty(ref _startStopSpeakOperation, value); }
        }

        private bool _allowedToSpeak = true;

        public bool AllowedToSpeak
        {
            get { return _allowedToSpeak; }
            set { SetProperty(ref _allowedToSpeak, value); }
        }

        private string _phoneIds;

        public string PhoneIds
        {
            get { return _phoneIds; }
            set { SetProperty(ref _phoneIds, value); }
        }

        private bool _recordChecked;

        public bool RecordChecked
        {
            get { return _recordChecked; }
            set
            {
                if (!value)
                {
                    RecordTips = null;
                }
                SetProperty(ref _recordChecked, value);
            }
        }

        private bool _pushLiveChecked;

        public bool PushLiveChecked
        {
            get { return _pushLiveChecked; }
            set
            {
                if (!value)
                {
                    PushLiveStreamTips = null;
                }
                SetProperty(ref _pushLiveChecked, value);
            }
        }

        private string _speakingStatus;

        public string SpeakingStatus
        {
            get { return _speakingStatus; }
            set { SetProperty(ref _speakingStatus, value); }
        }

        private Visibility _sharingVisibility;

        public Visibility SharingVisibility
        {
            get { return _sharingVisibility; }
            set { SetProperty(ref _sharingVisibility, value); }
        }

        private Visibility _cancelSharingVisibility;

        public Visibility CancelSharingVisibility
        {
            get { return _cancelSharingVisibility; }
            set { SetProperty(ref _cancelSharingVisibility, value); }
        }

        private object _dialogContent;

        public object DialogContent
        {
            get { return _dialogContent; }
            set { SetProperty(ref _dialogContent, value); }
        }

        private bool _isDialogOpen;

        public bool IsDialogOpen
        {
            get { return _isDialogOpen; }
            set { SetProperty(ref _isDialogOpen, value); }
        }

        private string _dialogMsg;

        public string DialogMsg
        {
            get { return _dialogMsg; }
            set { SetProperty(ref _dialogMsg, value); }
        }

        private Visibility _isSpeaker;

        public Visibility IsSpeaker
        {
            get { return _isSpeaker; }
            set { SetProperty(ref _isSpeaker, value); }
        }

        private string _curModeName;

        public string CurModeName
        {
            get { return _curModeName; }
            set { SetProperty(ref _curModeName, value); }
        }

        private string _curLayoutName;

        public string CurLayoutName
        {
            get { return _curLayoutName; }
            set { SetProperty(ref _curLayoutName, value); }
        }

        private bool _isMenuOpen;
        public bool IsMenuOpen
        {
            get { return _isMenuOpen; }
            set { SetProperty(ref _isMenuOpen, value); }
        }

        #endregion

        #region Commands

        public ICommand LoadCommand { get; set; }
        public ICommand ModeChangedCommand { get; set; }
        public ICommand SpeakingStatusChangedCommand { get; set; }
        public ICommand ExternalDataChangedCommand { get; set; }
        public ICommand SharingDesktopCommand { get; set; }
        public ICommand CancelSharingCommand { get; set; }
        public ICommand ExitCommand { get; set; }
        public ICommand OpenExitDialogCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand KickoutCommand { get; set; }
        public ICommand OpenCloseCameraCommand { get; set; }
        public ICommand GetCameraInfoCommand { get; set; }
        public ICommand OpenPropertyPageCommand { get; set; }
        public ICommand SetDefaultFigureCameraCommand { get; set; }
        public ICommand SetDefaultDataCameraCommand { get; set; }
        public ICommand SetMicStateCommand { get; set; }
        public ICommand ScreenShareCommand { get; set; }
        public ICommand StartSpeakCommand { get; set; }
        public ICommand StopSpeakCommand { get; set; }
        public ICommand BanToSpeakCommand { get; set; }
        public ICommand AllowToSpeakCommand { get; set; }
        public ICommand StartDoubleScreenCommand { get; set; }
        public ICommand StopDoubleScreenCommand { get; set; }
        public ICommand StartMonitorStreamCommand { get; set; }
        public ICommand StopMonitorStreamCommand { get; set; }
        public ICommand RecordCommand { get; set; }
        public ICommand PushLiveCommand { get; set; }
        public ICommand TopMostTriggerCommand { get; set; }
        public ICommand ShowLogCommand { get; set; }
        public ICommand TriggerMenuCommand { get; set; }

        #endregion

        #region Command Handlers

        //command handlers
        private async Task JoinMeetingAsync()
        {
            GlobalData.Instance.ViewArea = new ViewArea()
            {
                Width = _meetingView.ActualWidth,
                Height = _meetingView.ActualHeight
            };

            uint[] uint32SOfNonDataArray =
            {
                (uint) _meetingView.PictureBox1.Handle.ToInt32(),
                (uint) _meetingView.PictureBox2.Handle.ToInt32(),
                (uint) _meetingView.PictureBox3.Handle.ToInt32(),
                (uint) _meetingView.PictureBox4.Handle.ToInt32(),
            };

            foreach (var hwnd in uint32SOfNonDataArray)
            {
                Log.Logger.Debug($"【figure hwnd】：{hwnd}");
            }

            uint[] uint32SOfDataArray = {(uint) _meetingView.PictureBox5.Handle.ToInt32()};

            foreach (var hwnd in uint32SOfDataArray)
            {
                Log.Logger.Debug($"【data hwnd】：{hwnd}");
            }

            JoinMeetingResult joinMeetingResult =
                await
                    _sdkService.JoinMeeting(MeetingId, uint32SOfNonDataArray, uint32SOfNonDataArray.Length,
                        uint32SOfDataArray,
                        uint32SOfDataArray.Length);

            //if failed to join meeting, needs to roll back
            if (joinMeetingResult.m_result.m_rc != 0)
            {
                Log.Logger.Error(
                    $"【join meeting result】：result={joinMeetingResult.m_result.m_rc}, msg={joinMeetingResult.m_result.m_message}");
                switch (joinMeetingResult.m_result.m_rc)
                {
                    case 13:
                        joinMeetingResult.m_result.m_message = Messages.WarningNoCamera;
                        break;
                    case 14:
                        joinMeetingResult.m_result.m_message = Messages.WarningNoMicrophone;
                        break;
                    case 15:
                    case -1009:
                        joinMeetingResult.m_result.m_message = Messages.WarningNoSpeaker;
                        break;
                    case -915:
                        joinMeetingResult.m_result.m_message = Messages.WarningInvalidMeetingNo;
                        break;
                    case -914:
                        joinMeetingResult.m_result.m_message = Messages.WarningMeetingHasBeenEnded;
                        break;
                }

                _startMeetingCallbackEvent(false, joinMeetingResult.m_result.m_message);
                _meetingView.Close();

            }
            else
            {
                //if join meeting successfully, then make main view invisible
                _startMeetingCallbackEvent(true, "");


                //if not speaker, then clear mode menu items
                if (!_sdkService.IsSpeaker)
                {
                    ModeMenuItems.Clear();
                    IsSpeaker = Visibility.Collapsed;
                }
                else
                {
                    IsSpeaker = Visibility.Visible;
                }

                if (_lessonDetail.Id > 0)
                {
                    ResponseResult result = await
                        _bmsService.UpdateMeetingStatus(_lessonDetail.Id, _userInfo.UserId,
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            string.Empty);

                    HasErrorMsg(result.Status, result.Message);
                }
            }
        }

        private async Task MeetingModeChangedAsync(string meetingMode)
        {
            if (!CheckIsUserSpeaking(true))
            {
                return;
            }

            if (meetingMode == MeetingMode.Speaker.ToString() &&
                !_viewLayoutService.ViewFrameList.Any(
                    v => v.PhoneId == _sdkService.TeacherPhoneId && v.ViewType == 1))
            {
                //如果选中的模式条件不满足，则回滚到之前的模式，
                //没有主讲者视图无法设置主讲模式，没有共享无法共享模式，没有发言无法设置任何模式

                HasErrorMsg("-1", Messages.WarningNoSpeaderView);
                return;
            }

            if (meetingMode == MeetingMode.Sharing.ToString() &&
                !_viewLayoutService.ViewFrameList.Any(
                    v => v.PhoneId == _sdkService.TeacherPhoneId && v.ViewType == 2))
            {
                //如果选中的模式条件不满足，则回滚到之前的模式，
                //没有主讲者视图无法设置主讲模式，没有共享无法共享模式，没有发言无法设置任何模式

                HasErrorMsg("-1", Messages.WarningNoSharingView);
                return;
            }

            var newMeetingMode = (MeetingMode) Enum.Parse(typeof(MeetingMode), meetingMode);

            _viewLayoutService.ChangeMeetingMode(newMeetingMode);

            _viewLayoutService.ResetAsAutoLayout();

            await _viewLayoutService.LaunchLayout();
        }

        private async Task SpeakingStatusChangedAsync()
        {
            if (SpeakingStatus == IsSpeaking)
            {
                bool stopSucceeded = await _sdkService.StopSpeak();
                if (!stopSucceeded)
                {
                    //
                }
                //will change SpeakStatus in StopSpeakCallbackEventHandler.
            }

            if (SpeakingStatus == IsNotSpeaking)
            {
                AsynCallResult result = await _sdkService.ApplyToSpeak();
                if (!HasErrorMsg(result.m_rc.ToString(), result.m_message))
                {
                    // will change SpeakStatus in callback???
                    SpeakingStatus = IsSpeaking;
                }
            }
        }

        private async Task ExternalDataChangedAsync(string sourceName)
        {
            if (!CheckIsUserSpeaking(true))
            {
                return;
            }

            AsynCallResult openDataResult = await _sdkService.OpenSharedCamera(sourceName);
            if (!HasErrorMsg(openDataResult.m_rc.ToString(), openDataResult.m_message))
            {
                _cancelSharingAction = async () =>
                {
                    AsynCallResult result = await _sdkService.CloseSharedCamera();
                    if (!HasErrorMsg(result.m_rc.ToString(), result.m_message))
                    {
                        SharingVisibility = Visibility.Visible;
                        CancelSharingVisibility = Visibility.Collapsed;
                    }
                };

                SharingVisibility = Visibility.Collapsed;
                CancelSharingVisibility = Visibility.Visible;
            }
        }

        private async Task SharingDesktopAsync()
        {
            if (!CheckIsUserSpeaking(true))
            {
                return;
            }

            AsynCallResult startResult = await _sdkService.StartScreenSharing();
            if (!HasErrorMsg(startResult.m_rc.ToString(), startResult.m_message))
            {
                _cancelSharingAction = async () =>
                {
                    AsynCallResult result = await _sdkService.StopScreenSharing();
                    if (!HasErrorMsg(result.m_rc.ToString(), result.m_message))
                    {
                        SharingVisibility = Visibility.Visible;
                        CancelSharingVisibility = Visibility.Collapsed;
                    }
                };
                SharingVisibility = Visibility.Collapsed;
                CancelSharingVisibility = Visibility.Visible;
            }
        }

        private async Task CancelSharingAsync()
        {
            await _cancelSharingAction();
        }

        public async Task ExitAsync()
        {
            IsDialogOpen = false;
            UnRegisterMeetingEvents();

            await _meetingView.Dispatcher.BeginInvoke(new Action(() =>
            {
                _meetingView.Close();

                _exitMeetingCallbackEvent(true, "");

            }));

            await StopAllLives();

            AsynCallResult exitResult = await _sdkService.ExitMeeting();
            Log.Logger.Debug($"【exit meeting】：result={exitResult.m_rc}, msg={exitResult.m_message}");
            HasErrorMsg(exitResult.m_rc.ToString(), exitResult.m_message);
        }

        private async Task StopAllLives()
        {
            await _localPushLiveService.StopPushLiveStream();
            await _serverPushLiveService.StopPushLiveStream();
            await _localRecordService.StopRecord();
        }

        private async Task UpdateExitTime()
        {
            if (_lessonDetail.Id > 0)
            {
                ResponseResult updateResult = await
                    _bmsService.UpdateMeetingStatus(_lessonDetail.Id, _userInfo.UserId,
                        string.Empty, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    );

                HasErrorMsg(updateResult.Status, updateResult.Message);
            }
        }

        private async Task OpenExitDialogAsync()
        {
            await _meetingView.Dispatcher.BeginInvoke(new Action(() =>
            {
                DialogMsg = "确定退出？";
                DialogContent = new ConfirmDialogContent();
                IsDialogOpen = true;
            }));
        }

        private async Task CancelAsync()
        {
            await Task.Run(() =>
            {
                IsDialogOpen = false;
            });
        }

        private async Task KickoutAsync(string userPhoneId)
        {
            await _sdkService.HostKickoutUser(userPhoneId);
        }

        private async Task OpenCloseCameraAsync()
        {
            if (OpenCloseCameraOperation == "open camera")
            {
                AsynCallResult result = await _sdkService.OpenCamera(SelectedCamera);
                if (!HasErrorMsg(result.m_rc.ToString(), result.m_message))
                {
                    OpenCloseCameraOperation = "close camera";
                }

            }
            else
            {
                AsynCallResult result = await _sdkService.CloseCamera();
                if (!HasErrorMsg(result.m_rc.ToString(), result.m_message))
                {
                    OpenCloseCameraOperation = "open camera";
                }

            }
        }

        private async Task OpenPropertyPageAsync(string cameraName)
        {
            await _meetingView.Dispatcher.BeginInvoke(new Action(() =>
            {
                int result = _sdkService.ShowCameraProtityPage(cameraName);
            }));
        }

        private async Task GetCameraInfoAsync(string cameraName)
        {
            await Task.Run(() =>
            {
                VideoDeviceInfo videoDeviceInfo = _sdkService.GetVideoDeviceInfos(cameraName);
            });
        }

        private async Task SetDefaultFigureCameraAsync(string cameraName)
        {
            AsynCallResult result = await _sdkService.SetDefaultCamera(1, cameraName);
            HasErrorMsg(result.m_rc.ToString(), result.m_message);
        }

        private async Task SetDefaultDataCameraAsync(string cameraName)
        {
            AsynCallResult result = await _sdkService.SetDefaultCamera(2, cameraName);
            HasErrorMsg(result.m_rc.ToString(), result.m_message);
        }

        private async Task ScreenShareAsync()
        {

            if (ScreenShareState == "共享屏幕")
            {
                AsynCallResult result = await _sdkService.StartScreenSharing();
                if (!HasErrorMsg(result.m_rc.ToString(), result.m_message))
                {
                    ScreenShareState = "取消屏幕共享";
                }
            }
            else
            {
                AsynCallResult result = await _sdkService.StopScreenSharing();
                if (!HasErrorMsg(result.m_rc.ToString(), result.m_message))
                {
                    ScreenShareState = "共享屏幕";
                }
            }

        }

        private async Task SetMicStateAsync()
        {
            if (MicState == "静音")
            {
                AsynCallResult result = await _sdkService.SetMicMuteState(1);
                if (!HasErrorMsg(result.m_rc.ToString(), result.m_message))
                {
                    MicState = "取消静音";
                }
            }
            else
            {
                AsynCallResult result = await _sdkService.SetMicMuteState(0);
                if (!HasErrorMsg(result.m_rc.ToString(), result.m_message))
                {
                    MicState = "静音";
                }
            }
        }

        private async Task AllowToSpeakAsync(string arg)
        {
            string[] userPhoneIds = PhoneIds.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            await _sdkService.AllowToSpeak(userPhoneIds);
        }

        private async Task BanToSpeakAsync(string arg)
        {
            string[] userPhoneIds = PhoneIds.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            await _sdkService.BanToSpeak(userPhoneIds);
        }

        private async Task StartSpeakAsync(string userPhoneId)
        {
            AsynCallResult result = await _sdkService.RequireUserSpeak(userPhoneId);
            HasErrorMsg(result.m_rc.ToString(), result.m_message);
        }

        private async Task StopSpeakAsync(string userPhoneId)
        {
            AsynCallResult result = await _sdkService.RequireUserStopSpeak(userPhoneId);
            HasErrorMsg(result.m_rc.ToString(), result.m_message);
        }

        private async Task PushLiveAsync()
        {
            if (PushLiveChecked)
            {
                _localPushLiveService.GetLiveParam();

                StartLiveStreamResult result =
                    await
                        _localPushLiveService.StartPushLiveStream(
                            _viewLayoutService.GetStreamLayout(_localPushLiveService.LiveParam.m_nWidth,
                                _localPushLiveService.LiveParam.m_nHeight));

                if (HasErrorMsg(result.m_result.m_rc.ToString(), result.m_result.m_message))
                {
                    PushLiveChecked = false;
                }
                else
                {
                    PushLiveStreamTips =
                        string.Format(
                            $"分辨率：{_localPushLiveService.LiveParam.m_nWidth}*{_localPushLiveService.LiveParam.m_nHeight}\r\n" +
                            $"码率：{_localPushLiveService.LiveParam.m_nVideoBitrate}\r\n" +
                            $"推流地址：{_localPushLiveService.LiveParam.m_url1}");
                }
            }
            else
            {
                AsynCallResult result = await _localPushLiveService.StopPushLiveStream();
                if (HasErrorMsg(result.m_rc.ToString(), result.m_message))
                {
                    PushLiveChecked = true;
                }
            }
        }

        private async Task RecordAsync()
        {
            if (RecordChecked)
            {
                _localRecordService.GetRecordParam();

                LocalRecordResult result =
                    await
                        _localRecordService.StartRecord(
                            _viewLayoutService.GetStreamLayout(_localRecordService.RecordParam.Width,
                                _localRecordService.RecordParam.Height));

                if (HasErrorMsg(result.m_result.m_rc.ToString(), result.m_result.m_message))
                {
                    RecordChecked = false;
                }
                else
                {
                    RecordTips =
                        string.Format(
                            $"分辨率：{_localRecordService.RecordParam.Width}*{_localRecordService.RecordParam.Height}\r\n" +
                            $"码率：{_localRecordService.RecordParam.VideoBitrate}\r\n" +
                            $"录制路径：{_localRecordService.RecordDirectory}");
                }
            }
            else
            {
                AsynCallResult result = await _localRecordService.StopRecord();
                if (HasErrorMsg(result.m_rc.ToString(), result.m_message))
                {
                    RecordChecked = true;
                }
            }
        }


        //dynamic commands
        private async Task ViewModeChangedAsync(ViewMode viewMode)
        {
            if (!CheckIsUserSpeaking(true))
            {
                return;
            }

            _viewLayoutService.ChangeViewMode(viewMode);
            await _viewLayoutService.LaunchLayout();
        }

        private async Task FullScreenViewChangedAsync(ViewFrame fullScreenView)
        {
            if (!CheckIsUserSpeaking(true))
            {
                return;
            }


            if (!CheckIsUserSpeaking(fullScreenView, true))
            {
                return;
            }

            _viewLayoutService.ChangeViewMode(ViewMode.Closeup);

            _viewLayoutService.SetSpecialView(fullScreenView, SpecialViewType.FullScreen);

            await _viewLayoutService.LaunchLayout();
        }

        private async Task BigViewChangedAsync(ViewFrame bigView)
        {
            if (!CheckIsUserSpeaking(true))
            {
                return;
            }

            if (_viewLayoutService.ViewFrameList.Count(viewFrame => viewFrame.IsOpened) < 2)
            {
                //一大多小至少有两个视图，否则不予设置

                HasErrorMsg("-1", Messages.WarningBigSmallLayoutNeedsTwoAboveViews);
                return;
            }

            if (!CheckIsUserSpeaking(bigView, true))
            {
                return;
            }

            _viewLayoutService.ChangeViewMode(ViewMode.BigSmalls);

            var bigSpeakerView =
                _viewLayoutService.ViewFrameList.FirstOrDefault(
                    v => v.PhoneId == bigView.PhoneId && v.Hwnd == bigView.Hwnd);

            if (bigSpeakerView == null)
            {
                //LOG ViewFrameList may change during this period.
            }

            _viewLayoutService.SetSpecialView(bigSpeakerView, SpecialViewType.Big);

            await _viewLayoutService.LaunchLayout();
        }

        #endregion

        #region Methods

        private List<ViewFrame> InitializeViewFrameList(MeetingView meetingView)
        {
            List<ViewFrame> viewFrames = new List<ViewFrame>();

            ViewFrame1 = new ViewFrame(meetingView.PictureBox1.Handle, meetingView.PictureBox1);
            ViewFrame2 = new ViewFrame(meetingView.PictureBox2.Handle, meetingView.PictureBox2);
            ViewFrame3 = new ViewFrame(meetingView.PictureBox3.Handle, meetingView.PictureBox3);
            ViewFrame4 = new ViewFrame(meetingView.PictureBox4.Handle, meetingView.PictureBox4);
            ViewFrame5 = new ViewFrame(meetingView.PictureBox5.Handle, meetingView.PictureBox5);

            viewFrames.Add(ViewFrame1);
            viewFrames.Add(ViewFrame2);
            viewFrames.Add(ViewFrame3);
            viewFrames.Add(ViewFrame4);
            viewFrames.Add(ViewFrame5);

            return viewFrames;
        }

        public void RefreshLayoutMenu(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = e.OriginalSource as MenuItem;
            if (menuItem.Header is StackPanel)
            {
                RefreshLayoutMenuItems();
            }
            else
            {
                e.Handled = true;
            }
        }

        private void InitializeMenuItems()
        {
            LoadModeMenuItems();
            RefreshLayoutMenuItems();
            RefreshExternalData();
        }

        private void RegisterMeetingEvents()
        {
            _meetingView.Closing += _meetingView_Closing;
            _sdkService.ViewCreateEvent += ViewCreateEventHandler;
            _sdkService.ViewCloseEvent += ViewCloseEventHandler;
            _sdkService.StartSpeakEvent += StartSpeakEventHandler;
            _sdkService.StopSpeakEvent += StopSpeakEventHandler;
            _viewLayoutService.MeetingModeChangedEvent += MeetingModeChangedEventHandler;
            _viewLayoutService.ViewModeChangedEvent += ViewModeChangedEventHandler;
            _sdkService.OtherJoinMeetingEvent += OtherJoinMeetingEventHandler;
            _sdkService.OtherExitMeetingEvent += OtherExitMeetingEventHandler;
            _sdkService.UIMessageReceivedEvent += UIMessageReceivedEventHandler;
            _sdkService.ErrorMsgReceivedEvent += ErrorMsgReceivedEventHandler;
            _sdkService.KickedByHostEvent += KickedByHostEventHandler;
            _sdkService.DiskSpaceNotEnough += DiskSpaceNotEnoughEventHandler;
        }

        private void DiskSpaceNotEnoughEventHandler(AsynCallResult msg)
        {
            HasErrorMsg(msg.m_rc.ToString(), msg.m_message);
        }

        private async void _meetingView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UnRegisterMeetingEvents();

            await UpdateExitTime();
        }

        private void UnRegisterMeetingEvents()
        {
            _sdkService.ViewCreateEvent -= ViewCreateEventHandler;
            _sdkService.ViewCloseEvent -= ViewCloseEventHandler;
            _sdkService.StartSpeakEvent -= StartSpeakEventHandler;
            _sdkService.StopSpeakEvent -= StopSpeakEventHandler;
            _viewLayoutService.MeetingModeChangedEvent -= MeetingModeChangedEventHandler;
            _viewLayoutService.ViewModeChangedEvent -= ViewModeChangedEventHandler;
            _sdkService.OtherJoinMeetingEvent -= OtherJoinMeetingEventHandler;
            _sdkService.OtherExitMeetingEvent -= OtherExitMeetingEventHandler;
            _sdkService.UIMessageReceivedEvent -= UIMessageReceivedEventHandler;
            _sdkService.ErrorMsgReceivedEvent -= ErrorMsgReceivedEventHandler;
            _sdkService.KickedByHostEvent -= KickedByHostEventHandler;
            _sdkService.DiskSpaceNotEnough -= DiskSpaceNotEnoughEventHandler;
        }

        private void KickedByHostEventHandler(AsynCallResult Msg)
        {
            _meetingView.Dispatcher.BeginInvoke(new Action(() =>
            {
                _meetingView.Close();


                _exitMeetingCallbackEvent(true, "");

                //MetroWindow mainView = App.SSCBootstrapper.Container.ResolveKeyed<MetroWindow>("MainView");
                //mainView.GlowBrush = new SolidColorBrush(Colors.Purple);
                //mainView.NonActiveGlowBrush = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#FF999999"));
                //mainView.Visibility = Visibility.Visible;
            }));
        }

        private void ViewModeChangedEventHandler(ViewMode viewMode)
        {
            CurLayoutName = EnumHelper.GetDescription(typeof(ViewMode), viewMode);
        }

        private async Task MeetingModeChangedEventHandler(MeetingMode meetingMode)
        {
            CurModeName = EnumHelper.GetDescription(typeof(MeetingMode), meetingMode);

            if (_sdkService.IsSpeaker)
            {
                AsynCallResult result =
                    await
                        _sdkService.SendUIMessage((int) _viewLayoutService.MeetingMode,
                            _viewLayoutService.MeetingMode.ToString(), _viewLayoutService.MeetingMode.ToString().Length,
                            null);
                HasErrorMsg(result.m_rc.ToString(), result.m_message);
            }
        }

        private void ErrorMsgReceivedEventHandler(AsynCallResult error)
        {
            HasErrorMsg("-1", error.m_message);
        }

        private void UIMessageReceivedEventHandler(UIMessage message)
        {
            if (message.m_messageId < 3)
            {
                _sdkService.SetTeacherPhoneId(message.m_sender.m_szPhoneId);

                MeetingMode meetingMode = (MeetingMode) message.m_messageId;
                _viewLayoutService.ChangeMeetingMode(meetingMode);

                _viewLayoutService.LaunchLayout();
            }
            else
            {
                if (message.m_messageId == (int) UiMessage.BannedToSpeak)
                {
                    AllowedToSpeak = false;
                }
                if (message.m_messageId == (int) UiMessage.AllowToSpeak)
                {
                    AllowedToSpeak = true;
                }
            }
        }

        private void OtherExitMeetingEventHandler(ContactInfo contactInfo)
        {
            //var attendee = _userInfos.FirstOrDefault(userInfo => userInfo.GetNube() == contactInfo.m_szPhoneId);

            //string displayName = string.Empty;
            //if (!string.IsNullOrEmpty(attendee?.Name))
            //{
            //    displayName = attendee.Name + " - ";
            //}

            //string exitMsg = $"{displayName}{contactInfo.m_szPhoneId}退出会议！";
            //HasErrorMsg("-1", exitMsg);

            if (contactInfo.m_szPhoneId == _sdkService.TeacherPhoneId)
            {
                //
            }
        }

        private void OtherJoinMeetingEventHandler(ContactInfo contactInfo)
        {
            var attendee = _userInfos.FirstOrDefault(userInfo => userInfo.GetNube() == contactInfo.m_szPhoneId);

            //string displayName = string.Empty;
            //if (!string.IsNullOrEmpty(attendee?.Name))
            //{
            //    displayName = attendee.Name + " - ";
            //}

            //string joinMsg = $"{displayName}{contactInfo.m_szPhoneId}加入会议！";
            //HasErrorMsg("-1", joinMsg);

            //speaker automatically sends a message(with creatorPhoneId) to nonspeakers
            //!!!CAREFUL!!! ONLY speaker will call this
            if (_sdkService.IsSpeaker)
            {
                _sdkService.SendUIMessage((int) _viewLayoutService.MeetingMode,
                    _viewLayoutService.MeetingMode.ToString(), _viewLayoutService.MeetingMode.ToString().Length, null);
            }
        }

        private async void ViewCloseEventHandler(SpeakerView speakerView)
        {
            await _viewLayoutService.HideViewAsync(speakerView);
        }

        private void StopSpeakEventHandler()
        {
            _viewLayoutService.ChangeViewMode(ViewMode.Auto);

            if (_sdkService.IsSpeaker)
            {
                _viewLayoutService.ChangeMeetingMode(MeetingMode.Interaction);
            }

            SpeakingStatus = IsNotSpeaking;
            SharingVisibility = Visibility.Visible;
            CancelSharingVisibility = Visibility.Collapsed;

            _meetingView.Dispatcher.BeginInvoke(new Action(RefreshExternalData));
            //reload menus
        }

        private void StartSpeakEventHandler()
        {
            SpeakingStatus = IsSpeaking;
        }

        private async void ViewCreateEventHandler(SpeakerView speakerView)
        {
            await _viewLayoutService.ShowViewAsync(speakerView);
        }

        private async Task GetViewSize()
        {
            GetvideoParamResult videoParamResult;

            do
            {
                videoParamResult = await _sdkService.GetVideoStreamsParam();
                Console.WriteLine($"count:{videoParamResult.m_count}");
                foreach (var videoParam in videoParamResult.m_VideoParams)
                {
                    Console.WriteLine($"width:{videoParam.m_nVideoWidth}, height:{videoParam.m_nVideoHeight}");
                }

                Thread.Sleep(2000);

            } while (videoParamResult.m_count != _viewLayoutService.ViewFrameList.Count);
        }

        private bool CheckIsUserSpeaking(bool showMsgBar = false)
        {
            //return true;

            List<ParticipantInfo> participants = _sdkService.GetParticipants();

            var self = participants.FirstOrDefault(p => p.m_contactInfo.m_szPhoneId == _sdkService.SelfPhoneId);

            if (showMsgBar && self.m_bIsSpeaking != 1)
            {
                HasErrorMsg("-1", Messages.WarningYouAreNotSpeaking);
            }

            return self.m_bIsSpeaking == 1;
        }

        private bool CheckIsUserSpeaking(ViewFrame speakerView, bool showMsgBar = false)
        {
            //return true;

            List<ParticipantInfo> participants = _sdkService.GetParticipants();

            var speaker = participants.FirstOrDefault(p => p.m_contactInfo.m_szPhoneId == speakerView.PhoneId);

            bool isUserNotSpeaking = string.IsNullOrEmpty(speaker.m_contactInfo.m_szPhoneId) ||
                                     speaker.m_bIsSpeaking != 1;

            if (isUserNotSpeaking && showMsgBar)
            {
                HasErrorMsg("-1", Messages.WarningUserNotSpeaking);
            }

            return !isUserNotSpeaking;
        }

        private void RefreshExternalData()
        {
            if (SharingMenuItems == null)
            {
                SharingMenuItems = new ObservableCollection<MenuItem>();

            }
            else
            {
                SharingMenuItems.Clear();
            }

            var sharings = Enum.GetNames(typeof(Sharing));
            foreach (var sharing in sharings)
            {
                var newSharingMenu = new MenuItem();
                newSharingMenu.Header = EnumHelper.GetDescription(typeof(Sharing), Enum.Parse(typeof(Sharing), sharing));

                if (sharing == Sharing.Desktop.ToString())
                {
                    newSharingMenu.Command = SharingDesktopCommand;
                }

                if (sharing == Sharing.ExternalData.ToString())
                {
                    DeviceInfo[] cameras = _sdkService.GetDeviceList(1);
                    foreach (var camera in cameras)
                    {
                        if (!string.IsNullOrEmpty(camera.m_szDevName) && camera.m_isDefault == 0)
                        {
                            newSharingMenu.Items.Add(
                                new MenuItem()
                                {
                                    Header = camera.m_szDevName,
                                    Command = ExternalDataChangedCommand,
                                    CommandParameter = camera.m_szDevName
                                });
                        }
                    }
                }

                SharingMenuItems.Add(newSharingMenu);
            }
        }

        private void LoadModeMenuItems()
        {
            if (ModeMenuItems == null)
            {
                ModeMenuItems = new ObservableCollection<MenuItem>();
            }
            else
            {
                ModeMenuItems.Clear();
            }

            var modes = Enum.GetNames(typeof(MeetingMode));
            foreach (var mode in modes)
            {
                var newModeMenu = new MenuItem();
                newModeMenu.Header = EnumHelper.GetDescription(typeof(MeetingMode),
                    Enum.Parse(typeof(MeetingMode), mode));
                newModeMenu.Command = ModeChangedCommand;
                newModeMenu.CommandParameter = mode;

                ModeMenuItems.Add(newModeMenu);
            }
            CurModeName = EnumHelper.GetDescription(typeof(MeetingMode), _viewLayoutService.MeetingMode);

        }

        private void RefreshLayoutMenuItems()
        {
            if (LayoutMenuItems == null)
            {
                LayoutMenuItems = new ObservableCollection<MenuItem>();
            }
            else
            {
                LayoutMenuItems.Clear();
            }

            var layouts = Enum.GetNames(typeof(ViewMode));
            foreach (var layout in layouts)
            {
                var newLayoutMenu = new MenuItem();
                newLayoutMenu.Header = EnumHelper.GetDescription(typeof(ViewMode), Enum.Parse(typeof(ViewMode), layout));
                newLayoutMenu.Tag = layout;

                if (layout == ViewMode.BigSmalls.ToString() || layout == ViewMode.Closeup.ToString())
                {
                    foreach (var speakerView in _viewLayoutService.ViewFrameList)
                    {
                        if (speakerView.IsOpened)
                        {
                            newLayoutMenu.Items.Add(new MenuItem()
                            {
                                Header =
                                    string.IsNullOrEmpty(speakerView.ViewName)
                                        ? speakerView.PhoneId
                                        : (speakerView.ViewName + " - " + speakerView.PhoneId),
                                Tag = speakerView
                            });
                        }
                    }
                }

                newLayoutMenu.Click += LayoutChangedEventHandler;

                LayoutMenuItems.Add(newLayoutMenu);
            }
            CurLayoutName = EnumHelper.GetDescription(typeof(ViewMode), _viewLayoutService.ViewMode);
        }

        private async void LayoutChangedEventHandler(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            MenuItem sourceMenuItem = e.OriginalSource as MenuItem;

            string header = menuItem.Tag.ToString();

            ViewMode viewMode = (ViewMode) Enum.Parse(typeof(ViewMode), header);

            switch (viewMode)
            {
                case ViewMode.Auto:
                case ViewMode.Average:
                    await ViewModeChangedAsync(viewMode);
                    break;
                case ViewMode.BigSmalls:
                    ViewFrame bigView = sourceMenuItem.Tag as ViewFrame;
                    await BigViewChangedAsync(bigView);
                    break;
                case ViewMode.Closeup:
                    ViewFrame fullView = sourceMenuItem.Tag as ViewFrame;
                    await FullScreenViewChangedAsync(fullView);
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}
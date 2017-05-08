using Prism.Commands;

using St.Common;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Autofac;
using Prism.Regions;
using Serilog;

namespace St.InteractiveWithouLive
{
    public class InteractiveWithouLiveContentViewModel : ViewModelBase
    {
        public InteractiveWithouLiveContentViewModel(InteractiveWithouLiveContentView interactiveWithouLiveContentView)
        {
            _interactiveWithouLiveContentView = interactiveWithouLiveContentView;
            _bmsService = DependencyResolver.Current.GetService<IBms>();
            _sdkService = DependencyResolver.Current.GetService<ISdk>();
            _lessonInfo = DependencyResolver.Current.GetService<LessonInfo>();
            _groupManager = DependencyResolver.Current.GetService<IGroupManager>();
            _visualizeShellService = DependencyResolver.Current.GetService<IVisualizeShell>();

            ParticipateOrWatchCommand = DelegateCommand.FromAsyncHandler(ParticipateOrWatchAsync);
            SelectionChangedCommand = DelegateCommand<LessonInfo>.FromAsyncHandler(SelectionChangedAsync);
            LoadCommand = DelegateCommand.FromAsyncHandler(LoadAsync);
            RefreshCommand = DelegateCommand.FromAsyncHandler(RefreshAsync);
            CloseMainPointCommand = new DelegateCommand(() => { ShowMainPoint = false; });
            ShowMainPointCommand = new DelegateCommand(() => { ShowMainPoint = true; });
            GotoSettingCommand = new DelegateCommand(GotoSetting);

            Attendees = new ObservableCollection<UserInfo>();
            Lessons = new ObservableCollection<LessonInfo>();
        }

        private void GotoSetting()
        {
            ShowNoDeviceMsg = false;
            IRegionManager regionManager = DependencyResolver.Current.GetService<IRegionManager>();

            regionManager.RequestNavigate(RegionNames.ContentRegion,
                new Uri(GlobalResources.SettingContentView, UriKind.Relative));
            _visualizeShellService.SetSelectedMenu(GlobalResources.SettingNavView);
        }

        private void GroupCallback(string code, HashSet<string> users)
        {
            Log.Logger.Debug($"【group members】：{users.Count}");
            foreach (var user in users)
            {
                Log.Logger.Debug($"openId：{user}");
            }

            if (Attendees != null && Attendees.Count > 0)
            {
                foreach (var attendee in Attendees)
                {
                    Log.Logger.Debug($"【iterate attendee】：openId={attendee.OpenId}");
                    attendee.IsOnline = users.Contains(attendee.OpenId);
                }
            }
        }

        //private fields
        private readonly InteractiveWithouLiveContentView _interactiveWithouLiveContentView;
        private readonly IBms _bmsService;
        private readonly ISdk _sdkService;
        private readonly LessonInfo _lessonInfo;
        private readonly IGroupManager _groupManager;
        private readonly IVisualizeShell _visualizeShellService;

        //properties
        private LessonDetail _lessonDetail;
        public LessonDetail CurLessonDetail
        {
            get { return _lessonDetail; }
            set { SetProperty(ref _lessonDetail, value); }
        }

        private LessonInfo _selectedLesson;

        public LessonInfo SelectedLesson
        {
            get { return _selectedLesson; }
            set { SetProperty(ref _selectedLesson, value); }
        }

        private string _attendeesCount;
        public string AttendeesCount
        {
            get { return _attendeesCount; }
            set { SetProperty(ref _attendeesCount, value); }
        }

        private Visibility _detailVisibility = Visibility.Collapsed;
        public Visibility DetailVisibility
        {
            get { return _detailVisibility; }
            set { SetProperty(ref _detailVisibility, value); }
        }

        private Visibility _participateVisibility = Visibility.Collapsed;
        public Visibility ParticipateVisibility
        {
            get { return _participateVisibility; }
            set { SetProperty(ref _participateVisibility, value); }
        }

        private string _isTeacher;
        public string IsTeacher
        {
            get { return _isTeacher; }
            set { SetProperty(ref _isTeacher, value); }
        }

        private string _mainPoint;
        public string MainPoint
        {
            get { return _mainPoint; }
            set { SetProperty(ref _mainPoint, value); }
        }

        private bool _showMainPoint;
        public bool ShowMainPoint
        {
            get { return _showMainPoint; }
            set { SetProperty(ref _showMainPoint, value); }
        }

        private bool _showNoDeviceMsg;

        public bool ShowNoDeviceMsg
        {
            get { return _showNoDeviceMsg; }
            set
            {
                _visualizeShellService.SetTopMost(!value);
                SetProperty(ref _showNoDeviceMsg, value);
            }
        }


        private string _noDeviceMsg;
        public string NoDeviceMsg
        {
            get { return _noDeviceMsg; }
            set { SetProperty(ref _noDeviceMsg, value); }
        }

        public ObservableCollection<LessonInfo> Lessons { get; set; }
        public ObservableCollection<UserInfo> Attendees { get; set; }


        //commands
        public ICommand LoadCommand { get; set; }
        public ICommand ParticipateOrWatchCommand { get; set; }
        public ICommand SelectionChangedCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand CloseMainPointCommand { get; set; }
        public ICommand ShowMainPointCommand { get; set; }
        public ICommand GotoSettingCommand { get; set; }

        //command hanlders
        private async Task LoadAsync()
        {
            RtServerHandler.Instance.GroupNotifyHandler = GroupCallback;

            ResetStatus();
            await GetLessonsAsync();
            if (_lessonInfo.Id > 0)
            {
                await AutoSelectLessonAsync();
            }
        }

        private async Task ParticipateOrWatchAsync()
        {
            ResponseResult meetingResult = await _bmsService.GetMeetingByLessonId(CurLessonDetail.Id.ToString());


            Log.Logger.Debug(
                $"【get meetingId by lessonId result】：result={meetingResult.Status}, msg={meetingResult.Message}, data={meetingResult.Data}");

            if (HasErrorMsg(meetingResult.Status, meetingResult.Message))
            {
                return;
            }

            if (meetingResult.Data == null)
            {
                HasErrorMsg("-1", Messages.WarningNullDataFromServer);
                return;
            }

            switch (meetingResult.Data.ToString())
            {
                case "-1":
                    HasErrorMsg("-1", Messages.WarningYouNeedCreateAMeeting);

                    //create a meeting and update to database
                    CreateMeetingResult createMeetingResult = await _sdkService.CreateMeeting(new ContactInfo[0], 0);

                    Log.Logger.Debug(
                        $"【create meeting result】：result={createMeetingResult.m_result.m_rc}, msg={createMeetingResult.m_result.m_message}");

                    switch (createMeetingResult.m_result.m_rc)
                    {
                        case 13:
                            createMeetingResult.m_result.m_message = Messages.WarningNoCamera;
                            break;
                        case 14:
                            createMeetingResult.m_result.m_message = Messages.WarningNoMicrophone;
                            break;
                        case 15:
                        case -1009:
                            createMeetingResult.m_result.m_message = Messages.WarningNoSpeaker;
                            break;
                        default:
                            break;
                    }

                    if (
                        !HasErrorMsg(createMeetingResult.m_result.m_rc.ToString(),
                            createMeetingResult.m_result.m_message))
                    {
                        ResponseResult updateResult =
                            await _bmsService.UpdateMeetingId(CurLessonDetail.Id, createMeetingResult.m_meetingId);

                        if (!HasErrorMsg(updateResult.Status, updateResult.Message))
                        {
                            await GotoMeeting(createMeetingResult.m_meetingId);
                        }
                    }

                    break;
                case "0":
                    //someone is creating a meeting, just wait
                    HasErrorMsg("-1", Messages.WarningSomeoneIsCreatingAMeeting);

                    break;
                default:
                    int meetingId = int.Parse(meetingResult.Data.ToString());

                    await GotoMeeting(meetingId);
                    break;
            }
        }

        private async Task GotoMeeting(int meetingId)
        {
            var curUser = DependencyResolver.Current.Container.Resolve<UserInfo>();

            if (curUser.UserId == CurLessonDetail.MasterUserId)
            {
                _sdkService.SetTeacherPhoneId(curUser.GetNube());
            }

            var lessonDetail = DependencyResolver.Current.Container.Resolve<LessonDetail>();
            lessonDetail.CloneLessonDetail(CurLessonDetail);

            _sdkService.SetMeetingId(meetingId);

            await _interactiveWithouLiveContentView.Dispatcher.BeginInvoke(new Action(() =>
            {
                IMeeting meetingService = DependencyResolver.Current.Container.Resolve<IMeeting>();

                meetingService.StartMeetingCallbackEvent += MeetingService_StartMeetingCallbackEvent;

                meetingService.ExitMeetingCallbackEvent += MeetingService_ExitMeetingCallbackEvent;

                meetingService.StartMeeting();
            }));

        }

        private async Task SelectionChangedAsync(LessonInfo selectedLessonInfo)
        {
            if (selectedLessonInfo == null || selectedLessonInfo.Id == 0)
            {
                return;
            }

            await GetLessonDetail(selectedLessonInfo);
            await GetAttendees(selectedLessonInfo);
            await _groupManager.JoinGroup(selectedLessonInfo.Code, GlobalResources.InteractiveWithouLiveContentView);
        }

        private async Task RefreshAsync()
        {
            await LoadAsync();
            await _groupManager.LeaveGroup();
        }

        private void MeetingService_ExitMeetingCallbackEvent(bool exitedSuccessful, string arg2)
        {
            if (exitedSuccessful)
            {
                IVisualizeShell visualizeShellService = DependencyResolver.Current.Container.Resolve<IVisualizeShell>();
                visualizeShellService.ShowShell();
            }
        }

        private void MeetingService_StartMeetingCallbackEvent(bool startedSuccessful, string msg)
        {
            if (startedSuccessful)
            {
                IVisualizeShell visualizeShellService = DependencyResolver.Current.Container.Resolve<IVisualizeShell>();
                visualizeShellService.HideShell();
            }
            else
            {
                HasErrorMsg("-1", msg);
            }
        }

        private void ResetStatus()
        {
            DetailVisibility = Visibility.Collapsed;
            ParticipateVisibility = Visibility.Collapsed;
        }

        private async Task GetLessonsAsync()
        {
            ResponseResult lessonResult = await _bmsService.GetLessons(false,LessonType.InteractiveWithoutLive);

            if (HasErrorMsg(lessonResult.Status, lessonResult.Message))
            {
                return;
            }

            List<LessonInfo> lessonList = lessonResult.Data as List<LessonInfo>;

            Lessons.Clear();
            lessonList?.ForEach((lesson) =>
            {
                Lessons.Add(lesson);
            });
        }

        private async Task GetLessonDetail(LessonInfo selectedLessonInfo)
        {
            ResponseResult lessonDetail = await _bmsService.GetLessonById(selectedLessonInfo.Id.ToString());

            if (!HasErrorMsg(lessonDetail.Status, lessonDetail.Message))
            {
                CurLessonDetail = lessonDetail.Data as LessonDetail;

                if (CurLessonDetail != null)
                {
                    CurLessonDetail.LessonType = selectedLessonInfo.LessonType;

                    DetailVisibility = Visibility.Visible;

                    IsTeacher = CurLessonDetail.MasterUserId ==
                                DependencyResolver.Current.Container.Resolve<UserInfo>().UserId
                        ? "(你是主讲)"
                        : string.Empty;

                    MainPoint = CurLessonDetail.MainPoint;

                    DateTime now = DateTime.Now;
                    if (now >= CurLessonDetail.StartTime.AddMinutes(-20) &&
                        now <= CurLessonDetail.EndTime.AddMinutes(20))
                    {
                        ParticipateVisibility = Visibility.Visible;
                    }
                    else
                    {
                        ParticipateVisibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private async Task GetAttendees(LessonInfo selectedLessonInfo)
        {
            ResponseResult attendeesResult =
                await _bmsService.GetUsersByLessonId(selectedLessonInfo.Id.ToString());

            if (!HasErrorMsg(attendeesResult.Status, attendeesResult.Message))
            {
                List<UserInfo> attendees = attendeesResult.Data as List<UserInfo>;

                if (attendees != null)
                {
                    List<UserInfo> globalUserInfos = DependencyResolver.Current.Container.Resolve<List<UserInfo>>();
                    globalUserInfos.Clear();

                    Attendees.Clear();
                    attendees.ForEach(attendee =>
                    {
                        Attendees.Add(attendee);
                        globalUserInfos.Add(attendee);
                    });

                    AttendeesCount = string.Format($"(共{Attendees.Count}人)");

                    var masterUser =
                        Attendees.FirstOrDefault(userInfo => userInfo.UserId == CurLessonDetail?.MasterUserId);

                    if (masterUser != null)
                    {
                        masterUser.IsTeacher = true;
                    }
                }
            }
        }

        private async Task AutoSelectLessonAsync()
        {
            _visualizeShellService.SetSelectedMenu(GlobalResources.InteractiveWithouLiveNavView);
            SelectedLesson = Lessons.FirstOrDefault(l => l.Id == _lessonInfo.Id);
            await SelectionChangedAsync(_lessonInfo);
            _lessonInfo.CloneLessonInfo(new LessonInfo());
        }

        protected override bool HasErrorMsg(string status, string msg)
        {
            switch (msg)
            {
                case Messages.WarningNoMicrophone:
                case Messages.WarningNoSpeaker:
                case Messages.WarningNoCamera:
                    NoDeviceMsg = msg;
                    ShowNoDeviceMsg = true;
                    return true;
                default:
                    return base.HasErrorMsg(status, msg);
            }
        }
    }
}

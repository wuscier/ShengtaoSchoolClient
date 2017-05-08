using Autofac;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using St.Common;
using System.Collections.Generic;

namespace St.InstantMeeting
{
    public class InstantMeetingContentViewModel : ViewModelBase
    {
        public InstantMeetingContentViewModel(InstantMeetingContentView meetingContentView)
        {
            _meetingContentView = meetingContentView;
            _sdkService = DependencyResolver.Current.Container.Resolve<ISdk>();

            MeetingRecords = new ObservableCollection<MeetingRecord>();

            CreateMeetingCommand = DelegateCommand.FromAsyncHandler(CreateMeetingAsync);
            JoinMeetingByNoCommand = DelegateCommand.FromAsyncHandler(JoinMeetingAsync);
            LoadMeetingListCommand = DelegateCommand.FromAsyncHandler(LoadMeetingListAsync);
            JoinMeetingFromListCommand = DelegateCommand<string>.FromAsyncHandler(JoinMeetingFromListAsync);

            RegisterEvents();
        }

        //private fields
        private readonly InstantMeetingContentView _meetingContentView;
        private readonly ISdk _sdkService;

        //properties
        private string _meetingId;
        public string MeetingId
        {
            get { return _meetingId; }
            set { SetProperty(ref _meetingId, value); }
        }

        public ObservableCollection<MeetingRecord> MeetingRecords { get; set; }

        //commands
        public ICommand CreateMeetingCommand { get; set; }
        public ICommand JoinMeetingByNoCommand { get; set; }
        public ICommand LoadMeetingListCommand { get; set; }
        public ICommand JoinMeetingFromListCommand { get; set; }

        //command handlers
        private async Task CreateMeetingAsync()
        {
            CreateMeetingResult createMeetingResult = await _sdkService.CreateMeeting(new ContactInfo[0], 0);

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

            if (HasErrorMsg(createMeetingResult.m_result.m_rc.ToString(), createMeetingResult.m_result.m_message))
            {
                return;
            }

            await GotoMeetingViewAsync();
        }

        private async Task JoinMeetingAsync()
        {
            uint mId;
            if (!uint.TryParse(MeetingId, out mId))
            {
                HasErrorMsg("-1", Messages.WarningInvalidMeetingNo);
                return;
            }


            int meetingId = (int) mId;
            if (meetingId == 0)
            {
                HasErrorMsg("-1", Messages.WarningInvalidMeetingNo);
                return;
            }

            AsynCallResult result = await _sdkService.QueryMeetingExist(meetingId);
            if (result.m_rc == 6)
            {
                result.m_message = Messages.WarningMeetingNoDoesNotExist;
            }

            if (HasErrorMsg(result.m_rc.ToString(), result.m_message))
            {
                return;
            }

            _sdkService.SetMeetingId(meetingId);

            await GotoMeetingViewAsync();
        }

        private async Task LoadMeetingListAsync()
        {
            await Task.Run(() =>
            {
                AsynCallResult getMeetingListResult = _sdkService.GetMeetingList();

                HasErrorMsg(getMeetingListResult.m_rc.ToString(), getMeetingListResult.m_message);
            });
        }

        private async Task JoinMeetingFromListAsync(string meetingNo)
        {
            //some validation
            _sdkService.SetMeetingId(int.Parse(meetingNo));

            await GotoMeetingViewAsync();
        }


        //methods
        private async Task GotoMeetingViewAsync()
        {
            var lessonDetail = DependencyResolver.Current.Container.Resolve<LessonDetail>();
            lessonDetail.CloneLessonDetail(new LessonDetail());

            var attendees = DependencyResolver.Current.Container.Resolve<List<UserInfo>>();
            attendees.Clear();

            await _meetingContentView.Dispatcher.BeginInvoke(new Action(() =>
            {
                //Window meetingView = _container.ResolveNamed<Window>("MeetingView", new TypedParameter(typeof(int), meetingId));
                IMeeting meetingService = DependencyResolver.Current.Container.Resolve<IMeeting>();

                meetingService.StartMeetingCallbackEvent += MeetingService_StartMeetingCallbackEvent;

                meetingService.ExitMeetingCallbackEvent += MeetingService_ExitMeetingCallbackEvent;

                meetingService.StartMeeting();
            }));
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

        private void RegisterEvents()
        {
            //_meetingService.InternalMessagePassThroughEvent += InternalMessagePassThroughEventHandler;
            _sdkService.GetMeetingListEvent += GetMeetingListEventHandler;
        }

        private void GetMeetingListEventHandler(GetMeetingListResult getMeetingListResult)
        {
            int meetingInfoByte = Marshal.SizeOf(typeof(MeetingInfo));

            MeetingInfo[] meetingInfos = new MeetingInfo[getMeetingListResult.m_meetingList.m_count];

            for (int i = 0; i < getMeetingListResult.m_meetingList.m_count; i++)
            {
                IntPtr missPtr = (IntPtr) (getMeetingListResult.m_meetingList.m_pMeetings.ToInt64() + i*meetingInfoByte);
                meetingInfos[i] = (MeetingInfo) Marshal.PtrToStructure(missPtr, typeof(MeetingInfo));
            }

            _meetingContentView.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (meetingInfos.Count() > 0)
                {
                    MeetingRecords.Clear();
                }

                meetingInfos.ToList().ForEach((meetingInfo) =>
                {
                    DateTime baseDateTime = new DateTime(1970, 1, 1);
                    baseDateTime = baseDateTime.AddSeconds(double.Parse(meetingInfo.m_szStartTime));
                    string formattedStartTime = baseDateTime.ToString("yyyy-MM-dd HH:mm:ss");

                    MeetingRecords.Add(new MeetingRecord()
                    {
                        CreatorPhoneId = meetingInfo.m_szCreatorId,
                        CreatorName = meetingInfo.m_szCreatorName,
                        MeetingNo = meetingInfo.m_meetingId.ToString(),
                        StartTime = formattedStartTime,
                        JoinMeetingByListCommand = JoinMeetingFromListCommand
                    });
                });
            }));
        }

        private void InternalMessagePassThroughEventHandler(string internalMessage)
        {
            HasErrorMsg("-1",internalMessage);
        }
    }
}

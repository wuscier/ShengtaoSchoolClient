using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using Serilog;
using St.Common;

namespace St.Core
{
    public class ViewLayoutService : IViewLayout
    {
        private readonly ISdk _sdkService;
        private readonly IPushLive _localPushLiveService;
        private readonly IPushLive _serverPushLiveService;
        private readonly IRecord _localRecordService;
        private readonly List<UserInfo> _attendees;
        private readonly LessonDetail _lessonDetail;
        private static readonly double Columns = 30;
        private static readonly double Rows = 10;


        private MeetingMode meetingMode;

        private ViewMode viewMode;

        public ViewLayoutService()
        {
            _sdkService = DependencyResolver.Current.Container.Resolve<ISdk>();
            _attendees = DependencyResolver.Current.Container.Resolve<List<UserInfo>>();
            _lessonDetail = DependencyResolver.Current.Container.Resolve<LessonDetail>();
            _localPushLiveService =
                DependencyResolver.Current.Container.ResolveNamed<IPushLive>(GlobalResources.LocalPushLive);
            _serverPushLiveService =
                DependencyResolver.Current.Container.ResolveNamed<IPushLive>(GlobalResources.RemotePushLive);
            _localRecordService = DependencyResolver.Current.Container.Resolve<IRecord>();


            _sdkService.ExitMeetingEvent += ExitMeetingEventHandler;

            InitializeStatus();
        }

        public List<ViewFrame> ViewFrameList { get; set; }

        public event MeetingModeChanged MeetingModeChangedEvent;
        public event ViewModeChanged ViewModeChangedEvent;

        public MeetingMode MeetingMode
        {
            get { return meetingMode; }
            private set
            {
                meetingMode = value;
                MeetingModeChangedEvent?.Invoke(value);
            }
        }

        public ViewMode ViewMode
        {
            get { return viewMode; }
            private set
            {
                viewMode = value;
                ViewModeChangedEvent?.Invoke(value);
            }
        }

        public ViewFrame FullScreenView { get; private set; }

        public async Task ShowViewAsync(SpeakerView view)
        {
            Log.Logger.Debug(
                $"【create view】：hwnd={view.m_viewHwnd}, phoneId={view.m_speaker.m_szPhoneId}, viewType={view.m_viewType}");
            var viewFrameVisible = ViewFrameList.FirstOrDefault(viewFrame => viewFrame.Hwnd == view.m_viewHwnd);

            if (viewFrameVisible != null)
            {
                // LOG return a handle which can not be found in handle list.

                viewFrameVisible.IsOpened = true;
                viewFrameVisible.Visibility = Visibility.Visible;
                viewFrameVisible.PhoneId = view.m_speaker.m_szPhoneId;


                var attendee = _attendees.FirstOrDefault(userInfo => userInfo.GetNube() == view.m_speaker.m_szPhoneId);
                string displayName = string.Empty;
                if (!string.IsNullOrEmpty(attendee?.Name))
                {
                    displayName = attendee.Name;
                }

                viewFrameVisible.ViewName = view.m_viewType == 1
                    ? displayName
                    : $"(共享){displayName}";

                viewFrameVisible.ViewType = view.m_viewType;
                viewFrameVisible.ViewOrder = ViewFrameList.Max(viewFrame => viewFrame.ViewOrder) + 1;
            }

            await LaunchLayout();
        }

        public async Task HideViewAsync(SpeakerView view)
        {
            Log.Logger.Debug(
                $"【close view】：hwnd={view.m_viewHwnd}, phoneId={view.m_speaker.m_szPhoneId}, viewType={view.m_viewType}");

            ResetFullScreenView(view);

            var viewFrameInvisible = ViewFrameList.FirstOrDefault(viewFrame => viewFrame.Hwnd == view.m_viewHwnd);

            if (viewFrameInvisible != null)
            {
                // LOG return a handle which can not be found in handle list.

                viewFrameInvisible.IsOpened = false;
                viewFrameInvisible.Visibility = Visibility.Collapsed;
            }

            await LaunchLayout();
        }

        public void ResetAsAutoLayout()
        {
            ViewFrameList.ForEach(viewFrame => { viewFrame.IsBigView = false; });

            FullScreenView = null;

            ViewMode = ViewMode.Auto;
        }

        public void ResetAsInitialStatus()
        {
            //will call this method when user exits meeting
            MakeAllViewsInvisible();
            InitializeStatus();
        }

        public void ChangeMeetingMode(MeetingMode meetingMode)
        {
            if (MeetingMode != meetingMode)
                MeetingMode = meetingMode;
        }

        public void ChangeViewMode(ViewMode viewMode)
        {
            if (ViewMode != viewMode)
                ViewMode = viewMode;
        }

        public async Task LaunchLayout()
        {
            switch (ViewMode)
            {
                //case ViewMode.Auto:
                default:
                    switch (MeetingMode)
                    {
                        //模式优先级 高于 画面布局，选择一个模式将会重置布局为自动
                        //在某种模式下，用户可以随意更改布局
                        case MeetingMode.Interaction:
                            await LaunchAverageLayout();
                            break;
                        case MeetingMode.Speaker:
                            await GotoSpeakerMode();
                            break;
                        case MeetingMode.Sharing:
                            await GotoSharingMode();
                            break;
                    }
                    break;
                case ViewMode.Average:
                    await LaunchAverageLayout();
                    break;
                case ViewMode.BigSmalls:
                    await LaunchBigSmallLayout();
                    break;
                case ViewMode.Closeup:
                    await LaunchCloseUpLayout();
                    break;
            }

            await StartOrRefreshLiveAsync();
        }

        private async Task StartOrRefreshLiveAsync()
        {
            if (ViewFrameList.Count(viewFrame => viewFrame.IsOpened && viewFrame.Visibility == Visibility.Visible) > 0)
            {
                if (_sdkService.IsSpeaker && !_serverPushLiveService.HasPushLiveSuccessfully &&
                    (_lessonDetail.LessonType == LessonType.Interactive ||
                     _lessonDetail.LessonType == LessonType.Discussion))
                {
                    _serverPushLiveService.HasPushLiveSuccessfully = true;
                    await StartPushLiveStreamAutomatically();
                }

                if (_localPushLiveService.LiveId != 0)
                {
                    await
                        _localPushLiveService.RefreshLiveStream(GetStreamLayout(
                            _localPushLiveService.LiveParam.m_nWidth,
                            _localPushLiveService.LiveParam.m_nHeight));
                }

                if (_serverPushLiveService.LiveId != 0)
                {
                    await
                        _serverPushLiveService.RefreshLiveStream(
                            GetStreamLayout(_serverPushLiveService.LiveParam.m_nWidth,
                                _serverPushLiveService.LiveParam.m_nHeight));
                }

                if (_localRecordService.RecordId != 0)
                {
                    await
                        _localRecordService.RefreshLiveStream(GetStreamLayout(_localRecordService.RecordParam.Width,
                            _localRecordService.RecordParam.Height));
                }
            }
        }

        public void SetSpecialView(ViewFrame view, SpecialViewType type)
        {
            switch (type)
            {
                case SpecialViewType.Big:
                    SetBigView(view);
                    break;
                case SpecialViewType.FullScreen:
                    SetFullScreenView(view);
                    break;
                default:
                    break;
            }
        }

        private void ExitMeetingEventHandler()
        {
            ResetAsInitialStatus();
        }

        private void InitializeStatus()
        {
            ViewFrameList = new List<ViewFrame>();

            MeetingMode = MeetingMode.Interaction;

            ViewMode = ViewMode.Auto;
            FullScreenView = null;

            var count = 0;
            var participants = _sdkService.GetParticipants();
            if (participants != null)
                count = participants.Count(p => p.m_contactInfo.m_szPhoneId == _sdkService.SelfPhoneId);

            if (count == 0)
                ViewFrameList.Clear();
        }

        public async Task GotoSpeakerMode()
        {
            // 主讲模式下，不会显示听讲者视图
            //1. 有主讲者视图和共享视图，主讲者大，共享小
            //2. 有主讲者，没有共享，主讲者全屏
            //3. 无主讲者，无法设置主讲模式【选择主讲模式时会校验】

            var speakerView =
                ViewFrameList.FirstOrDefault(
                    v =>
                        (v.PhoneId == _sdkService.TeacherPhoneId) && v.IsOpened &&
                        (v.ViewType == 1));
            if (speakerView == null)
            {
                await GotoDefaultMode();
                return;
            }

            var sharingView =
                ViewFrameList.FirstOrDefault(
                    v =>
                        (v.PhoneId == _sdkService.TeacherPhoneId) && v.IsOpened &&
                        (v.ViewType == 2));
            if (sharingView == null)
            {
                FullScreenView = speakerView;
                await LaunchCloseUpLayout();
                return;
            }

            SetBigView(speakerView);
            await LaunchBigSmallLayout();
        }

        public async Task GotoSharingMode()
        {
            // 共享模式下，不会显示听讲者视图【设置完共享源，将自动开启共享模式】
            //1. 有主讲者视图和共享视图，主讲者小，共享大
            //2. 无主讲者，有共享，共享全屏
            //3. 没有共享，无法设置共享模式【选择共享模式时会校验】

            var sharingView =
                ViewFrameList.FirstOrDefault(
                    v => (v.PhoneId == _sdkService.TeacherPhoneId) && v.IsOpened && (v.ViewType == 2));
            if (sharingView == null)
            {
                await GotoDefaultMode();
                return;
            }

            var speakerView =
                ViewFrameList.FirstOrDefault(
                    v => (v.PhoneId == _sdkService.TeacherPhoneId) && v.IsOpened && (v.ViewType == 1));
            if (speakerView == null)
            {
                FullScreenView = sharingView;
                await LaunchCloseUpLayout();
                return;
            }

            SetBigView(sharingView);

            await LaunchBigSmallLayout();
        }

        public void MakeAllViewsInvisible()
        {
            ViewFrameList.ForEach(viewFrame => { viewFrame.Visibility = Visibility.Collapsed; });
        }

        private async Task GotoDefaultMode()
        {
            MeetingMode = MeetingMode.Interaction;
            ResetAsAutoLayout();

            await LaunchLayout();
        }

        //private void MakeNonCreatorViewsInvisible()
        //{
        //    ViewFrameList.ForEach(viewFrame =>
        //    {
        //        if (viewFrame.PhoneId != _sdkService.TeacherPhoneId)
        //            viewFrame.Visibility = Visibility.Collapsed;
        //    });
        //}

        private void ResetFullScreenView(SpeakerView toBeClosedView)
        {
            if ((FullScreenView != null) && (FullScreenView.PhoneId == toBeClosedView.m_speaker.m_szPhoneId) &&
                (FullScreenView.ViewType == toBeClosedView.m_viewType))
                FullScreenView = null;
        }

        public List<LiveVideoStreamInfo> GetStreamLayout(int resolutionWidth, int resolutionHeight)
        {
            var viewFramesVisible =
                ViewFrameList.Where(viewFrame => viewFrame.IsOpened && viewFrame.Visibility == Visibility.Visible);

            var viewFramesByDesending = viewFramesVisible.OrderBy(viewFrame => viewFrame.ViewOrder);

            var orderViewFrames = viewFramesByDesending.ToList();

            List<LiveVideoStreamInfo> liveVideoStreamInfos = new List<LiveVideoStreamInfo>();


            foreach (var orderViewFrame in orderViewFrames)
            {
                LiveVideoStreamInfo newLiveVideoStreamInfo = new LiveVideoStreamInfo();
                RefreshLiveLayout(ref newLiveVideoStreamInfo, orderViewFrame, resolutionWidth, resolutionHeight);
                liveVideoStreamInfos.Add(newLiveVideoStreamInfo);
            }

            return liveVideoStreamInfos;
        }

        private void RefreshLiveLayout(ref LiveVideoStreamInfo liveVideoStreamInfo, ViewFrame viewFrame,
            int resolutionWidth, int resolutionHeight)
        {
            liveVideoStreamInfo.Handle = (uint) viewFrame.Hwnd.ToInt32();

            liveVideoStreamInfo.XLocation = (int)((viewFrame.Column/Columns)*resolutionWidth);
            liveVideoStreamInfo.Width = (int)((viewFrame.ColumnSpan/Columns)*resolutionWidth);

            liveVideoStreamInfo.YLocation =(int)((viewFrame.Row/Rows)*resolutionHeight);
            liveVideoStreamInfo.Height = (int)((viewFrame.RowSpan/Rows)*resolutionHeight);
        }

        private async Task LaunchAverageLayout()
        {
            await Task.Run(() =>
            {
                var viewFramesVisible = ViewFrameList.Where(viewFrame => viewFrame.IsOpened);

                var viewFramesByDesending = viewFramesVisible.OrderBy(viewFrame => viewFrame.ViewOrder);

                var orderViewFrames = viewFramesByDesending.ToList();
                switch (orderViewFrames.Count)
                {
                    case 0:
                        //displays a picture
                        break;
                    case 1:
                        var viewFrameFull = orderViewFrames[0];
                        viewFrameFull.Visibility = Visibility.Visible;
                        viewFrameFull.Row = 0;
                        viewFrameFull.RowSpan = 10;
                        viewFrameFull.Column = 0;
                        viewFrameFull.ColumnSpan = 30;

                        viewFrameFull.Width = GlobalData.Instance.ViewArea.Width;
                        viewFrameFull.Height = GlobalData.Instance.ViewArea.Height;
                        viewFrameFull.VerticalAlignment = VerticalAlignment.Center;
                        break;


                    case 2:
                        var viewFrameLeft2 = orderViewFrames[0];
                        var viewFrameRight2 = orderViewFrames[1];

                        viewFrameLeft2.Visibility = Visibility.Visible;
                        viewFrameLeft2.Row = 0;
                        viewFrameLeft2.RowSpan = 10;
                        viewFrameLeft2.Column = 0;
                        viewFrameLeft2.ColumnSpan = 15;
                        viewFrameLeft2.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameLeft2.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameLeft2.VerticalAlignment = VerticalAlignment.Center;

                        viewFrameRight2.Visibility = Visibility.Visible;
                        viewFrameRight2.Row = 0;
                        viewFrameRight2.RowSpan = 10;
                        viewFrameRight2.Column = 15;
                        viewFrameRight2.ColumnSpan = 15;
                        viewFrameRight2.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameRight2.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameRight2.VerticalAlignment = VerticalAlignment.Center;

                        break;
                    case 3:

                        var viewFrameLeft3 = orderViewFrames[0];
                        var viewFrameRight3 = orderViewFrames[1];
                        var viewFrameBottom3 = orderViewFrames[2];


                        viewFrameLeft3.Visibility = Visibility.Visible;
                        viewFrameLeft3.Row = 0;
                        viewFrameLeft3.RowSpan = 5;
                        viewFrameLeft3.Column = 0;
                        viewFrameLeft3.ColumnSpan = 15;
                        viewFrameLeft3.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameLeft3.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameLeft3.VerticalAlignment = VerticalAlignment.Center;

                        viewFrameRight3.Visibility = Visibility.Visible;
                        viewFrameRight3.Row = 0;
                        viewFrameRight3.RowSpan = 5;
                        viewFrameRight3.Column = 15;
                        viewFrameRight3.ColumnSpan = 15;
                        viewFrameRight3.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameRight3.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameRight3.VerticalAlignment = VerticalAlignment.Center;

                        viewFrameBottom3.Visibility = Visibility.Visible;
                        viewFrameBottom3.Row = 5;
                        viewFrameBottom3.RowSpan = 5;
                        viewFrameBottom3.Column = 0;
                        viewFrameBottom3.ColumnSpan = 15;
                        viewFrameBottom3.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameBottom3.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameBottom3.VerticalAlignment = VerticalAlignment.Center;

                        break;
                    case 4:
                        var viewFrameLeftTop4 = orderViewFrames[0];
                        var viewFrameRightTop4 = orderViewFrames[1];
                        var viewFrameLeftBottom4 = orderViewFrames[2];
                        var viewFrameRightBottom4 = orderViewFrames[3];

                        viewFrameLeftTop4.Visibility = Visibility.Visible;
                        viewFrameLeftTop4.Row = 0;
                        viewFrameLeftTop4.RowSpan = 5;
                        viewFrameLeftTop4.Column = 0;
                        viewFrameLeftTop4.ColumnSpan = 15;
                        viewFrameLeftTop4.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameLeftTop4.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameLeftTop4.VerticalAlignment = VerticalAlignment.Center;

                        viewFrameRightTop4.Visibility = Visibility.Visible;
                        viewFrameRightTop4.Row = 0;
                        viewFrameRightTop4.RowSpan = 5;
                        viewFrameRightTop4.Column = 15;
                        viewFrameRightTop4.ColumnSpan = 15;
                        viewFrameRightTop4.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameRightTop4.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameRightTop4.VerticalAlignment = VerticalAlignment.Center;

                        viewFrameLeftBottom4.Visibility = Visibility.Visible;
                        viewFrameLeftBottom4.Row = 5;
                        viewFrameLeftBottom4.RowSpan = 5;
                        viewFrameLeftBottom4.Column = 0;
                        viewFrameLeftBottom4.ColumnSpan = 15;
                        viewFrameLeftBottom4.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameLeftBottom4.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameLeftBottom4.VerticalAlignment = VerticalAlignment.Center;

                        viewFrameRightBottom4.Visibility = Visibility.Visible;
                        viewFrameRightBottom4.Row = 5;
                        viewFrameRightBottom4.RowSpan = 5;
                        viewFrameRightBottom4.Column = 15;
                        viewFrameRightBottom4.ColumnSpan = 15;
                        viewFrameRightBottom4.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameRightBottom4.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameRightBottom4.VerticalAlignment = VerticalAlignment.Center;

                        break;
                    case 5:
                        #region 三托二
                        //var viewFrameLeftTop5 = orderViewFrames[0];
                        //var viewFrameMiddleTop5 = orderViewFrames[1];
                        //var viewFrameRightTop5 = orderViewFrames[2];
                        //var viewFrameLeftBottom5 = orderViewFrames[3];
                        //var viewFrameRightBottom5 = orderViewFrames[4];

                        //viewFrameLeftTop5.Visibility = Visibility.Visible;
                        //viewFrameLeftTop5.Row = 0;
                        //viewFrameLeftTop5.RowSpan = 5;
                        //viewFrameLeftTop5.Column = 5;
                        //viewFrameLeftTop5.ColumnSpan = 10;
                        //viewFrameLeftTop5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        //viewFrameLeftTop5.Height = GlobalData.Instance.ViewArea.Width*0.1875;
                        //viewFrameLeftTop5.VerticalAlignment = VerticalAlignment.Bottom;

                        //viewFrameMiddleTop5.Visibility = Visibility.Visible;
                        //viewFrameMiddleTop5.Row = 0;
                        //viewFrameMiddleTop5.RowSpan = 5;
                        //viewFrameMiddleTop5.Column = 15;
                        //viewFrameMiddleTop5.ColumnSpan = 10;
                        //viewFrameMiddleTop5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        //viewFrameMiddleTop5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        //viewFrameMiddleTop5.VerticalAlignment = VerticalAlignment.Bottom;


                        //viewFrameRightTop5.Visibility = Visibility.Visible;
                        //viewFrameRightTop5.Row = 5;
                        //viewFrameRightTop5.RowSpan = 5;
                        //viewFrameRightTop5.Column = 0;
                        //viewFrameRightTop5.ColumnSpan = 10;
                        //viewFrameRightTop5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        //viewFrameRightTop5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        //viewFrameRightTop5.VerticalAlignment = VerticalAlignment.Top;

                        //viewFrameLeftBottom5.Visibility = Visibility.Visible;
                        //viewFrameLeftBottom5.Row = 5;
                        //viewFrameLeftBottom5.RowSpan = 5;
                        //viewFrameLeftBottom5.Column = 10;
                        //viewFrameLeftBottom5.ColumnSpan = 10;
                        //viewFrameLeftBottom5.Width = GlobalData.Instance.ViewArea.Width*0.3333;
                        //viewFrameLeftBottom5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        //viewFrameLeftBottom5.VerticalAlignment = VerticalAlignment.Top;

                        //viewFrameRightBottom5.Visibility = Visibility.Visible;
                        //viewFrameRightBottom5.Row = 5;
                        //viewFrameRightBottom5.RowSpan = 5;
                        //viewFrameRightBottom5.Column = 20;
                        //viewFrameRightBottom5.ColumnSpan = 10;
                        //viewFrameRightBottom5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        //viewFrameRightBottom5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        //viewFrameRightBottom5.VerticalAlignment = VerticalAlignment.Top;

                        #endregion

                        #region 平均排列，两行三列
                        var viewFrameLeftTop5 = orderViewFrames[0];
                        var viewFrameMiddleTop5 = orderViewFrames[1];
                        var viewFrameRightTop5 = orderViewFrames[2];
                        var viewFrameLeftBottom5 = orderViewFrames[3];
                        var viewFrameRightBottom5 = orderViewFrames[4];

                        viewFrameLeftTop5.Visibility = Visibility.Visible;
                        viewFrameLeftTop5.Row = 0;
                        viewFrameLeftTop5.RowSpan = 5;
                        viewFrameLeftTop5.Column = 0;
                        viewFrameLeftTop5.ColumnSpan = 10;
                        viewFrameLeftTop5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        viewFrameLeftTop5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        viewFrameLeftTop5.VerticalAlignment = VerticalAlignment.Bottom;

                        viewFrameMiddleTop5.Visibility = Visibility.Visible;
                        viewFrameMiddleTop5.Row = 0;
                        viewFrameMiddleTop5.RowSpan = 5;
                        viewFrameMiddleTop5.Column = 10;
                        viewFrameMiddleTop5.ColumnSpan = 10;
                        viewFrameMiddleTop5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        viewFrameMiddleTop5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        viewFrameMiddleTop5.VerticalAlignment = VerticalAlignment.Bottom;


                        viewFrameRightTop5.Visibility = Visibility.Visible;
                        viewFrameRightTop5.Row = 0;
                        viewFrameRightTop5.RowSpan = 5;
                        viewFrameRightTop5.Column = 20;
                        viewFrameRightTop5.ColumnSpan = 10;
                        viewFrameRightTop5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        viewFrameRightTop5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        viewFrameRightTop5.VerticalAlignment = VerticalAlignment.Bottom;

                        viewFrameLeftBottom5.Visibility = Visibility.Visible;
                        viewFrameLeftBottom5.Row = 5;
                        viewFrameLeftBottom5.RowSpan = 5;
                        viewFrameLeftBottom5.Column = 0;
                        viewFrameLeftBottom5.ColumnSpan = 10;
                        viewFrameLeftBottom5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        viewFrameLeftBottom5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        viewFrameLeftBottom5.VerticalAlignment = VerticalAlignment.Top;

                        viewFrameRightBottom5.Visibility = Visibility.Visible;
                        viewFrameRightBottom5.Row = 5;
                        viewFrameRightBottom5.RowSpan = 5;
                        viewFrameRightBottom5.Column = 10;
                        viewFrameRightBottom5.ColumnSpan = 10;
                        viewFrameRightBottom5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        viewFrameRightBottom5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        viewFrameRightBottom5.VerticalAlignment = VerticalAlignment.Top;

                        #endregion
                        break;
                    default:

                        // LOG count of view frames is not between 0 and 5 
                        break;
                }
            });
        }

        private async Task LaunchBigSmallLayout()
        {
            var viewFramesVisible = ViewFrameList.Where(viewFrame => viewFrame.IsOpened);
            var framesVisible = viewFramesVisible as ViewFrame[] ?? viewFramesVisible.ToArray();
            if (framesVisible.Length <= 1)
            {
                await GotoDefaultMode();
                return;
            }

            var bigViewFrame = framesVisible.FirstOrDefault(viewFrame => viewFrame.IsBigView);
            if (bigViewFrame == null)
            {
                await GotoDefaultMode();
                return;
            }

            bigViewFrame.Visibility = Visibility.Visible;
            bigViewFrame.Row = 1;
            bigViewFrame.RowSpan = 8;
            bigViewFrame.Column = 0;
            bigViewFrame.ColumnSpan = 24;
            bigViewFrame.Width = GlobalData.Instance.ViewArea.Width*0.8;
            bigViewFrame.Height = GlobalData.Instance.ViewArea.Width*0.45;
            bigViewFrame.VerticalAlignment = VerticalAlignment.Center;


            var smallViewFrames = framesVisible.Where(viewFrame => !viewFrame.IsBigView);
            var row = 1;
            foreach (var frame in smallViewFrames.OrderBy(viewFrame => viewFrame.ViewOrder))
            {
                if (row > 7)
                    break;

                frame.Visibility = Visibility.Visible;
                frame.Row = row;
                frame.RowSpan = 2;
                frame.Column = 24;
                frame.ColumnSpan = 6;
                frame.Width = GlobalData.Instance.ViewArea.Width*0.2;
                frame.Height = GlobalData.Instance.ViewArea.Width*0.1125;
                frame.VerticalAlignment = VerticalAlignment.Center;

                row += 2;
            }
        }

        private async Task LaunchCloseUpLayout()
        {
            if (FullScreenView == null)
            {
                await GotoDefaultMode();
                return;
            }

            ViewFrameList.ForEach(viewFrame =>
            {
                if (viewFrame.Hwnd != FullScreenView.Hwnd)
                    viewFrame.Visibility = Visibility.Collapsed;
            });

            FullScreenView.Visibility = Visibility.Visible;
            FullScreenView.Row = 0;
            FullScreenView.RowSpan = 10;
            FullScreenView.Column = 0;
            FullScreenView.ColumnSpan = 30;
            FullScreenView.Width = GlobalData.Instance.ViewArea.Width;
            FullScreenView.Height = GlobalData.Instance.ViewArea.Height;
            FullScreenView.VerticalAlignment = VerticalAlignment.Center;
        }

        private void SetBigView(ViewFrame view)
        {
            ViewFrameList.ForEach(viewFrame => { viewFrame.IsBigView = viewFrame.Hwnd == view.Hwnd ? true : false; });
        }

        private void SetFullScreenView(ViewFrame view)
        {
            FullScreenView = view;
        }

        private async Task StartPushLiveStreamAutomatically()
        {
            _serverPushLiveService.GetLiveParam();

            UserInfo userInfo = DependencyResolver.Current.GetService<UserInfo>();

            StartLiveStreamResult result =
                await
                    _serverPushLiveService.StartPushLiveStream(
                        GetStreamLayout(_serverPushLiveService.LiveParam.m_nWidth,
                            _serverPushLiveService.LiveParam.m_nHeight), userInfo.PushStreamUrl);

            if (result.m_result.m_rc == 0)
            {
                await
                    _serverPushLiveService.RefreshLiveStream(
                        GetStreamLayout(_serverPushLiveService.LiveParam.m_nWidth,
                            _serverPushLiveService.LiveParam.m_nHeight));
            }
            else
            {
                //log error msg
            }
        }
    }
}
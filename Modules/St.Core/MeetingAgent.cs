using System;
using System.Runtime.InteropServices;
using St.Common;

namespace St.Core
{
    public class MeetingAgent
    {
        #region c++Api

        private const string DllName = "MeetingSDKAgent.dll";
        /*
         * 判断会议是否存在
         *    参数： meetingId  会议ID
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QueryMeetingExist(int meetingId);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Init(string exePath);


        /*
         * 启动SDK agent
         *    参数： callback_func 回调接口 
         *    返回值： 0 成功   非0 失败
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Start2(PFunc_CallBack callbackFunc, string imie, string ip);

        /*
         * 启SDK agent 鉴权传appKey uid 和 视讯号
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Start(PFunc_CallBack callbackFunc, string appKey, string uid, string phoneId,
            string npsIp);

        /*
         * 停止SDK agent (同步)
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Stop();

        /*
         * 修改终端显示名称
         *    参数： newName 新的显示名称 
         *           ctx     上下文参数
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ModifyDisplayName(string newName, long ctx);

        /*
         * 根据视讯号查询显示名称
         *    参数： phoneId  视讯号 
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QueryDisplayNameByPhoneId(string phoneId);

        /*
         * 创建会议
         *    参数： pUsers 被邀请用户列表
         *           count  被邀请用户个数
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CreateMeeting(ContactInfo[] contactInfos, int count);

        /*
         * 获取可参加的会议列表()
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetMeetingList();

        /*
         * 邀请参会
         *    参数： meetingId 邀请别人参加的会议ID
         *           pUsers 被邀请用户列表
         *           count  被邀请用户个数
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //public static extern int InviteParticipants(int meetingId, IntPtr pUsers, int count);
        public static extern int InviteParticipants(int meetingId, ContactInfo[] contactInfos, int count);

        /*
         * 加入会议
         *    参数： meetingId  参加的会议ID
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JoinMeeting(int meetingId, uint[] hwnds, int count, uint[] docHwnds, int docCount);

        /*
         * 取得当前会议中参会人的个数
         *    返回值： 参会人个数
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetParticipantsCount(ref int count);

        /*
         * 取得当前会议中的参会人员列表 (同步)
         *    参数： pUsers   保存参会人信息的指针
         *           pGetNum  实际取到的参会人信息的数目
         *           maxCount 最多取多少个
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetParticipants(IntPtr pUsers, ref int pGetNum, int maxCount);

        /*
         * 取得当前会议中的参会人员列表 (同步)
         *    参数： pUsers   保存参会人信息的指针 包含是否正在发言的状态
         *           pGetNum  实际取到的参会人信息的数目
         *           maxCount 最多取多少个
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetParticipantsEx(IntPtr pUsers, ref int pGetNum, int maxCount);


        /*
         * 申请发言
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ApplyToSpeak();

        /*
         * 停止发言
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StopSpeak();

        /*
         * 向会议中的其他用户发送消息 （指令、数据透传接口，支持一对一，一对其他所有人）
         *    参数：messageId  消息Id
         *          pData      消息附带参数
         *          dataLen	   消息附带参数的长度（最大不超过160）
         *          toPhoneId  消息目的参会者的视讯号，为NULL时，广播给其他所有参会者
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //public static extern int SendUIMessage(int messageId, IntPtr pData, int dataLen, IntPtr toPhoneId);
        public static extern int SendUIMessage(int messageId, string pData, int dataLen, string toPhoneId);

        /*
         * 打开课件 (同步)
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StartShareDoc();

        /*
         * 关闭课件 (同步)
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StopShareDoc();

        /*
         * 课件是否打开 (同步)
         *    返回值：0 没有打开   非0 已经打开
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int IsShareDocOpened();

        /*
         * 退出当前会议
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ExitMeeting();

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int HostKickoutUser(string userPhoneId);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int OpenCamera(string cameraName);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CloseCamera();

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int OpenSharedCamera(string cameraName);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CloseSharedCamera();

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetDefaultCamera(int type, string cameraName);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ShowCameraProtityPage(string cameraName);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetCameraInfo(string cameraName, IntPtr devInfo);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetMicMuteState(int muteState);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StartScreenSharing();

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StopScreenSharing();

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int RequireUserSpeak(string phoneId);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int RequireUserStopSpeak(string phoneId);


        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StartRecord(string fileName, LiveVideoStreamInfo[] streams, int count);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StopRecord();

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetRecordDirectory(string recordDir);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetRecordParam(RecordParam recordParam);

        /*******************视图显示相关接口**********************/

        /*
         * 取得视频窗口的显示位置 (同步)
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //private static extern int GetViewPosition(void* hwnd, ViewRect* viewPos);
        public static extern int GetViewPosition(IntPtr hwnd, IntPtr viewPos);

        /*
         * 设置视频窗口的显示位置 (同步)
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //private static extern int SetViewPosition(void* hwnd, ViewRect viewPos);
        public static extern int SetViewPosition(IntPtr hwnd, ViewRect viewPos);

        /*
         * 设置视频窗口的可见性 (同步)
         *    参数：hwnd	窗口句柄  
         *          visible 0 隐藏窗口 非0 显示窗口
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //private static extern int GetViewVisile(void* hwnd, int visible);
        public static extern int SetViewVisile(IntPtr hwnd, int visible);

        /*
         * 判断视频窗口是否可见 (同步)
         *    参数：hwnd	窗口句柄  
         *    返回值： 0 窗口隐藏    非0 窗口可见
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        // private static extern int IsViewVisile(void* hwnd);
        public static extern int IsViewVisile(IntPtr hwnd);

        /*************************参数设置相关接口**********************************/

        /*
         * 取得本地网络参数 (同步)
         *    参数： pUseDhcp [out]  返回非0 使用DHCP    返回0 手动配置IP
         *           pIp      [out]  本地IP
         *			 maskIp   [out]  掩码
         *			 pGateway [out]  网关
         *			 pDns     [out]  DNS
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //private static extern int GetLocalNetConfig(int* pUseDhcp, char* pIp, char* maskIp, char* pGateway, char* pDns);
        public static extern int GetLocalNetConfig(ref int pUseDhcp, ref string pIp, ref string maskIp,
            ref string pGateway, ref string pDns);

        /*
         * 设置本地网络参数 (同步)
         *    参数： pUseDhcp [in]  非0 使用DHCP    0 手动配置IP
         *           pIp      [in]  本地IP
         *			 maskIp   [in]  掩码
         *			 pGateway [in]  网关
         *			 pDns     [in]  DNS
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //private static extern int SetLocalNetConfig(int* pUseDhcp, char* pIp, char* maskIp, char* pGateway, char* pDns);
        public static extern int SetLocalNetConfig(ref int pUseDhcp, ref string pIp, ref string maskIp,
            ref string pGateway, ref string pDns);

        /*
         * 设置服务器地址 (同步)
         *    参数： pIp    服务器ip地址
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetServerAddr(string pIp);

        /*
         * 取得设备列表 (同步)
         *    参数：devType    设备类型  1：摄像头采集源  2：文档视频采集源
         *                               3：音频采集源  4：音频播放设备
         *          pDevList   存放设备列表的指针
         *          maxCount   设备列表最多接收多少个设备信息
         *          pGetCount  实际取到的设备数量
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //private static extern int GetDeviceList(int devType, DeviceInfo* pDevList, int maxCount, int* pGetCount);
        public static extern int GetDeviceList(int devType, IntPtr pDevList, int maxCount, ref int pGetCount);

        /*
         * 设置音视频采集/播放的默认设备
         *    参数：devType    设备类型  1：摄像头采集源  2：文档视频采集源  
         *                               3：音频采集源  4：音频播放设备
         *          devName    选择设备的名称
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetDefaultDevice(int devType, string devName);

        /*
         * 设置视频采集分辨率
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetVideoCapResolution(int devicetype, int width, int height);

        /*
         * 设置采集视频码率
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetVideoCapBitRate(int deviceType, int bitrate);

        /*
         * 设置音频采集采样率
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetAudioCapSampleRate(int samplerate);

        /*
         * 设置采集音频码率
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetAudioCapBitRate(int bitrate);

        /*
       * 设置推流参数
       */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetBroadcastParam(int videoWidth, int videoHeight, int videoBitrate);

        /*
         * 取设备串号
         *   参数：devSerialNo 存储结果的字符串地址，默认长度24字节
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetSerialNo(IntPtr devSerialNo);


        /*
     * 将视频窗口提到最前面
     */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BringViewPanelFront(IntPtr hwnd);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CreateDateMeeting(int year, int month, int day, int hour, int min, int second);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StartBroadcast(string url);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StopBroadcast();

        /// <summary>
        ///     推流
        /// </summary>
        /// <param name="liveParam">推流参数</param>
        /// <param name="streams"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StartLiveStream(LiveParam liveParam, LiveVideoStreamInfo[] streams, int count);

        /// <summary>
        ///     停止推流
        /// </summary>
        /// <param name="liveId"></param>
        /// <returns></returns>
        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StopLiveStream(int liveId);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int UpdateLiveVideoStreams(int liveId, LiveVideoStreamInfo[] streams, int count);


        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StartMonitorBroadcast(LiveParam liveParam);

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StopMonitorBroadcast(int liveId);


        /*
         * 设置视频窗口绘制填充模式（启动sdk成功后调用）
         * 参数：0 保持原始图片直接显示模式
         *		 1：保持原始图片裁剪之后拉伸模式 
         *		 4：原始图片无条件拉伸模式
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetFillMode(int fillmode);


        /// <summary>
        ///     双屏渲染
        /// </summary>
        /// <param name="phoneId">视讯号</param>
        /// <param name="mediaType">视频类型</param>
        /// <param name="isRenderOnDoubleScreen">渲染双屏开关</param>
        /// <param name="displayWindowIntPtr"></param>
        /// <returns></returns>
        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetDoubleScreenRender(string phoneId, int mediaType, int isRenderOnDoubleScreen,
            IntPtr displayWindowIntPtr);

        /*
        * 窗口布局调整结束
        */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AdjustVideoLayoutEnd();

        /*
       * 修改本地显示的名称
       *    参数： newName 新的显示名称 
       */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetViewDisplayName(IntPtr hwnd, string newName);

        //显示Qos  和 关闭 Qos的接口
        /*
         * 显示Qos
         */

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ShowQosView();

        /*
         * 关闭Qos
         */
        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CloseQosView();

        [DllImport(DllName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetVideoStreamsParam();

        #endregion
    }
}
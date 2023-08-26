using BigoSitePlugin;
using LineLiveSitePlugin;
using MildomSitePlugin;
using MirrativSitePlugin;
using NicoSitePlugin;
using OpenrecSitePlugin;
using PeriscopeSitePlugin;
using Plugin;
using PluginCommon;
using SitePlugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using ShowRoomSitePlugin;
using TwicasSitePlugin;
using TwitchSitePlugin;
using WhowatchSitePlugin;
using YouTubeLiveSitePlugin;
using WebSocket4Net;
using Newtonsoft.Json;
using System.Net;

namespace TwiChaDashPlugin
{
    static class MessageParts
    {
        public static string ToTextWithImageAlt(this IEnumerable<IMessagePart> parts)
        {
            string s = "";
            if (parts != null)
            {
                foreach (var part in parts)
                {
                    if (part is IMessageText text)
                    {
                        s += text;
                    }
                    else if (part is IMessageImage image)
                    {
                        s += image.Alt;
                    }
                }
            }
            return s;
        }
    }
    [Export(typeof(IPlugin))]
    public class TwiChaDashPlugin : IPlugin, IDisposable
    {
        WebSocket _wSocket_TwiChaDash;
        Process _TwiChaDashProcess;
        Queue<string[]> PostData = new Queue<string[]>();
        private Options _options;

        public string Name => "TwiChaDash連携";

        public string Description => "TwiChaDashとうまく連携できるか試してみるプラグインです。";
        public void OnTopmostChanged(bool isTopmost)
        {
            if (_settingsView != null)
            {
                _settingsView.Topmost = isTopmost;
            }
        }
        public void OnLoaded()
        {
            try
            {
                var s = Host.LoadOptions(GetSettingsFilePath());
                _options.Deserialize(s);
            }
            catch (System.IO.FileNotFoundException) { }
            try
            {
                if (_options.IsExecTwiChaDashAtBoot && !IsExecutingProcess("TwiChaDash"))
                {
                    StartTwiChaDash();
                }
            }
            catch (Exception) { }

            Connect_To_TwiChaDash();

        }

        private void Connect_To_TwiChaDash()
        {
            if (_wSocket_TwiChaDash != null) 
            {
                try
                {
                    _wSocket_TwiChaDash?.Close();
                    _wSocket_TwiChaDash?.Dispose();
                }
                catch { }
                _wSocket_TwiChaDash = null; 
            }
            try
            {
                _wSocket_TwiChaDash = new WebSocket4Net.WebSocket("ws://" + _options.TwiChaDashIP + ":" + _options.TwiChaDashPort.ToString() + "/");
                _wSocket_TwiChaDash.Security.AllowUnstrustedCertificate = true; // 信頼されない検証を通す
                _wSocket_TwiChaDash.NoDelay = true;
                _wSocket_TwiChaDash.ReceiveBufferSize = 81920;
                _wSocket_TwiChaDash.AutoSendPingInterval = 10;
                //_wSocket_TwiChaDash.LocalEndPoint = new IPEndPoint( new IPAddress(new byte[]{ 127, 0, 0, 1 }), _options.TwiChaDashPort+1);

                _wSocket_TwiChaDash.Opened += (s, es) =>
                {
                    while (PostData.Count > 0)
                    {
                        string[] _data = PostData.Dequeue();
                        TalkText(_data[0], _data[1], _data[2], _data[3]);
                    }
                };
                _wSocket_TwiChaDash.MessageReceived += _wSocket_TwiChaDash_MessageReceived;
                //_wSocket_TwiChaDash.Closed += (s, es) =>
                //{
                //
                //};
                _wSocket_TwiChaDash?.Open();
            }
            catch (Exception) { }
        }

        private void _wSocket_TwiChaDash_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //JSONデータの分解
            //JsonConverter msg = new JsonConverter();
            //msg.Text = e.Message;
            //msg.DivideTransact_Message();

            //this.Invoke(new MethodInvoker(delegate ()
            //{
            //    //RecvList.Text += msg.Text + "\r\n";
            //    if (rb_MachanDirect.Checked)
            //    {
            //        CommandRequest(msg, 2);
            //    }
            //}));
        }

        /// <summary>
        /// 指定したプロセス名を持つプロセスが起動中か
        /// </summary>
        /// <param name="processName">プロセス名</param>
        /// <returns></returns>
        private bool IsExecutingProcess(string processName)
        {
            return Process.GetProcessesByName(processName).Length > 0;
        }

        public void OnClosing()
        {
            _settingsView?.ForceClose();
            var s = _options.Serialize();
            Host.SaveOptions(GetSettingsFilePath(), s);
            if (_wSocket_TwiChaDash != null && _options.IsKillTwiChaDash)
            {
                try
                {
                    _TwiChaDashProcess.Kill();
                }
                catch (Exception) { }
            }
        }
        private void StartTwiChaDash()
        {
            if (_TwiChaDashProcess == null && System.IO.File.Exists(_options.TwiChaDashPath))
            {
                _TwiChaDashProcess = Process.Start(_options.TwiChaDashPath);
                _TwiChaDashProcess.EnableRaisingEvents = true;
                _TwiChaDashProcess.Exited += TwiChaDashProcess_Exited;
            }
        }

        private void TwiChaDashProcess_Exited(object sender, EventArgs e)
        {
            try
            {
                _TwiChaDashProcess?.Close();//2018/03/25ここで_TwiChaDashProcessがnullになる場合があった
            }
            catch { }
            _TwiChaDashProcess = null;
        }
        private static (string name, string id,string comment,string site) GetData(ISiteMessage message, Options options)
        {
            string name = null;
            string id = null;
            string comment = null;
            string site = null;

            if (false) { }
            else if (message is IYouTubeLiveMessage youTubeLiveMessage)
            {
                site = "YouTube";
                switch (youTubeLiveMessage.YouTubeLiveMessageType)
                {
                    case YouTubeLiveMessageType.Connected:
                        if (options.IsYouTubeLiveConnect)
                        {
                            name = null;
                            id = null;
                            comment = (youTubeLiveMessage as IYouTubeLiveConnected).Text;
                        }
                        break;
                    case YouTubeLiveMessageType.Disconnected:
                        if (options.IsYouTubeLiveDisconnect)
                        {
                            name = null;
                            id = null;
                            comment = (youTubeLiveMessage as IYouTubeLiveDisconnected).Text;
                        }
                        break;
                    case YouTubeLiveMessageType.Comment:
                        if (options.IsYouTubeLiveComment)
                        {
                            id = (youTubeLiveMessage as IYouTubeLiveComment).Id;
                            if (options.IsYouTubeLiveCommentNickname)
                            {
                                name = (youTubeLiveMessage as IYouTubeLiveComment).NameItems.ToText();
                            }
                            if (options.IsYouTubeLiveCommentStamp)
                            {
                                comment = (youTubeLiveMessage as IYouTubeLiveComment).CommentItems.ToTextWithImageAlt();
                            }
                            else
                            {
                                comment = (youTubeLiveMessage as IYouTubeLiveComment).CommentItems.ToText();
                            }
                        }
                        break;
                    case YouTubeLiveMessageType.Superchat:
                        if (options.IsYouTubeLiveSuperchat)
                        {
                            if (options.IsYouTubeLiveSuperchatNickname)
                            {
                                name = (youTubeLiveMessage as IYouTubeLiveSuperchat).NameItems.ToText();
                            }
                            id = (youTubeLiveMessage as IYouTubeLiveComment).Id;
                            //TODO:superchat中のスタンプも読ませるべきでは？
                            comment = (youTubeLiveMessage as IYouTubeLiveSuperchat).CommentItems.ToText();
                        }
                        break;
                }
            }
            else if (message is IOpenrecMessage openrecMessage)
            {
                site = "OpenRec";

                switch (openrecMessage.OpenrecMessageType)
                {
                    case OpenrecMessageType.Connected:
                        if (options.IsOpenrecConnect)
                        {
                            name = null;
                            id = null;
                            comment = (openrecMessage as IOpenrecConnected).Text;
                        }
                        break;
                    case OpenrecMessageType.Disconnected:
                        if (options.IsOpenrecDisconnect)
                        {
                            name = null;
                            id = null;
                            comment = (openrecMessage as IOpenrecDisconnected).Text;
                        }
                        break;
                    case OpenrecMessageType.Comment:
                        if (options.IsOpenrecComment)
                        {
                            if (options.IsOpenrecCommentNickname)
                            {
                                name = (openrecMessage as IOpenrecComment).NameItems.ToText();
                            }
                            id = (openrecMessage as IOpenrecComment).UserId;
                            comment = (openrecMessage as IOpenrecComment).MessageItems.ToText();
                        }
                        break;
                }
            }
            else if (message is ITwitchMessage twitchMessage)
            {
                site = "Twitch";

                switch (twitchMessage.TwitchMessageType)
                {
                    case TwitchMessageType.Connected:
                        if (options.IsTwitchConnect)
                        {
                            name = null;
                            id = null;
                            comment = (twitchMessage as ITwitchConnected).Text;
                        }
                        break;
                    case TwitchMessageType.Disconnected:
                        if (options.IsTwitchDisconnect)
                        {
                            name = null;
                            id = null;
                            comment = (twitchMessage as ITwitchDisconnected).Text;
                        }
                        break;
                    case TwitchMessageType.Comment:
                        if (options.IsTwitchComment)
                        {
                            if (options.IsTwitchCommentNickname)
                            {
                                name = (twitchMessage as ITwitchComment).DisplayName;
                            }
                            id = (twitchMessage as ITwitchComment).UserName;
                            comment = (twitchMessage as ITwitchComment).CommentItems.ToText();
                        }
                        break;
                }
            }
            else if (message is INicoMessage NicoMessage)
            {
                site = "NicoNico";

                switch (NicoMessage.NicoMessageType)
                {
                    case NicoMessageType.Connected:
                        if (options.IsNicoConnect)
                        {
                            name = null;
                            id = null;
                            comment = (NicoMessage as INicoConnected).Text;
                        }
                        break;
                    case NicoMessageType.Disconnected:
                        if (options.IsNicoDisconnect)
                        {
                            name = null;
                            id = null;
                            comment = (NicoMessage as INicoDisconnected).Text;
                        }
                        break;
                    case NicoMessageType.Comment:
                        if (options.IsNicoComment)
                        {
                            if (options.IsNicoCommentNickname)
                            {
                                name = (NicoMessage as INicoComment).UserName;
                            }
                            id = (NicoMessage as INicoComment).UserId;
                            comment = (NicoMessage as INicoComment).Text;
                        }
                        break;
                    case NicoMessageType.Item:
                        if (options.IsNicoItem)
                        {
                            if (options.IsNicoItemNickname)
                            {
                                //name = (NicoMessage as INicoItem).NameItems.ToText();
                            }
                            comment = (NicoMessage as INicoGift).Text;
                        }
                        break;
                    case NicoMessageType.Ad:
                        if (options.IsNicoAd)
                        {
                            name = null;
                            id = null;
                            comment = (NicoMessage as INicoAd).Text;
                        }
                        break;
                }
            }
            else if (message is ITwicasMessage twicasMessage)
            {
                site = "Twicas";

                switch (twicasMessage.TwicasMessageType)
                {
                    case TwicasMessageType.Connected:
                        if (options.IsTwicasConnect)
                        {
                            name = null;
                            id = null;
                            comment = (twicasMessage as ITwicasConnected).Text;
                        }
                        break;
                    case TwicasMessageType.Disconnected:
                        if (options.IsTwicasDisconnect)
                        {
                            name = null;
                            id = null;
                            comment = (twicasMessage as ITwicasDisconnected).Text;
                        }
                        break;
                    case TwicasMessageType.Comment:
                        if (options.IsTwicasComment)
                        {
                            if (options.IsTwicasCommentNickname)
                            {
                                name = (twicasMessage as ITwicasComment).UserName;
                            }
                            id = (twicasMessage as ITwicasComment).UserId;
                            comment = (twicasMessage as ITwicasComment).CommentItems.ToText();
                        }
                        break;
                    case TwicasMessageType.Item:
                        if (options.IsTwicasItem)
                        {
                            if (options.IsTwicasItemNickname)
                            {
                                name = (twicasMessage as ITwicasItem).UserName;
                            }
                            id = (twicasMessage as ITwicasComment).UserId;
                            comment = (twicasMessage as ITwicasItem).CommentItems.ToTextWithImageAlt();
                        }
                        break;
                }
            }
            else if (message is ILineLiveMessage lineLiveMessage)
            {
                site = "LineLive";

                switch (lineLiveMessage.LineLiveMessageType)
                {
                    case LineLiveMessageType.Connected:
                        if (options.IsLineLiveConnect)
                        {
                            name = null;
                            id = null;
                            comment = (lineLiveMessage as ILineLiveConnected).Text;
                        }
                        break;
                    case LineLiveMessageType.Disconnected:
                        if (options.IsLineLiveDisconnect)
                        {
                            name = null;
                            id = null;
                            comment = (lineLiveMessage as ILineLiveDisconnected).Text;
                        }
                        break;
                    case LineLiveMessageType.Comment:
                        if (options.IsLineLiveComment)
                        {
                            if (options.IsLineLiveCommentNickname)
                            {
                                name = (lineLiveMessage as ILineLiveComment).DisplayName;
                            }
                            id = (lineLiveMessage as ILineLiveComment).UserId.ToString();
                            comment = (lineLiveMessage as ILineLiveComment).Text;
                        }
                        break;
                }
            }
            else if (message is IWhowatchMessage whowatchMessage)
            {
                site = "WhoWatch";

                switch (whowatchMessage.WhowatchMessageType)
                {
                    case WhowatchMessageType.Connected:
                        if (options.IsWhowatchConnect)
                        {
                            name = null;
                            id = null;
                            comment = (whowatchMessage as IWhowatchConnected).Text;
                        }
                        break;
                    case WhowatchMessageType.Disconnected:
                        if (options.IsWhowatchDisconnect)
                        {
                            name = null;
                            id = null;
                            comment = (whowatchMessage as IWhowatchDisconnected).Text;
                        }
                        break;
                    case WhowatchMessageType.Comment:
                        if (options.IsWhowatchComment)
                        {
                            if (options.IsWhowatchCommentNickname)
                            {
                                name = (whowatchMessage as IWhowatchComment).UserName;
                            }
                            id = (whowatchMessage as IWhowatchComment).UserId;
                            comment = (whowatchMessage as IWhowatchComment).Comment;
                        }
                        break;
                    case WhowatchMessageType.Item:
                        if (options.IsWhowatchItem)
                        {
                            if (options.IsWhowatchItemNickname)
                            {
                                name = (whowatchMessage as IWhowatchItem).UserName;
                            }
                            id = (whowatchMessage as IWhowatchComment).UserId;
                            comment = (whowatchMessage as IWhowatchItem).Comment;
                        }
                        break;
                }
            }
            else if (message is IMirrativMessage mirrativMessage)
            {
                site = "Mirrativ";

                switch (mirrativMessage.MirrativMessageType)
                {
                    case MirrativMessageType.Connected:
                        if (options.IsMirrativConnect)
                        {
                            name = null;
                            id = null;
                            comment = (mirrativMessage as IMirrativConnected).Text;
                        }
                        break;
                    case MirrativMessageType.Disconnected:
                        if (options.IsMirrativDisconnect)
                        {
                            name = null;
                            id = null;
                            comment = (mirrativMessage as IMirrativDisconnected).Text;
                        }
                        break;
                    case MirrativMessageType.Comment:
                        if (options.IsMirrativComment)
                        {
                            if (options.IsMirrativCommentNickname)
                            {
                                name = (mirrativMessage as IMirrativComment).UserName;
                            }
                            id = (mirrativMessage as IMirrativComment).UserId;
                            comment = (mirrativMessage as IMirrativComment).Text;
                        }
                        break;
                    case MirrativMessageType.JoinRoom:
                        if (options.IsMirrativJoinRoom)
                        {
                            name = null;
                            id = null;
                            comment = (mirrativMessage as IMirrativJoinRoom).Text;
                        }
                        break;
                    case MirrativMessageType.Item:
                        if (options.IsMirrativItem)
                        {
                            name = null;
                            id = null;
                            comment = (mirrativMessage as IMirrativItem).Text;
                        }
                        break;
                }
            }
            else if (message is IPeriscopeMessage PeriscopeMessage)
            {
                site = "Periscope";

                switch (PeriscopeMessage.PeriscopeMessageType)
                {
                    case PeriscopeMessageType.Connected:
                        if (options.IsPeriscopeConnect)
                        {
                            name = null;
                            id = null;
                            comment = (PeriscopeMessage as IPeriscopeConnected).Text;
                        }
                        break;
                    case PeriscopeMessageType.Disconnected:
                        if (options.IsPeriscopeDisconnect)
                        {
                            name = null;
                            id = null;
                            comment = (PeriscopeMessage as IPeriscopeDisconnected).Text;
                        }
                        break;
                    case PeriscopeMessageType.Comment:
                        if (options.IsPeriscopeComment)
                        {
                            if (options.IsPeriscopeCommentNickname)
                            {
                                name = (PeriscopeMessage as IPeriscopeComment).DisplayName;
                            }
                            id = (PeriscopeMessage as IPeriscopeComment).UserId;
                            comment = (PeriscopeMessage as IPeriscopeComment).Text;
                        }
                        break;
                    case PeriscopeMessageType.Join:
                        if (options.IsPeriscopeJoin)
                        {
                            name = null;
                            id = null;
                            comment = (PeriscopeMessage as IPeriscopeJoin).Text;
                        }
                        break;
                    case PeriscopeMessageType.Leave:
                        if (options.IsPeriscopeLeave)
                        {
                            name = null;
                            id = null;
                            comment = (PeriscopeMessage as IPeriscopeLeave).Text;
                        }
                        break;
                }            
            }
            else if (message is IMildomMessage MildomMessage)
            {
                site = "Mildom";

                switch (MildomMessage.MildomMessageType)
                {
                    case MildomMessageType.Connected:
                        if (options.IsMildomConnect)
                        {
                            name = null;
                            comment = (MildomMessage as IMildomConnected).Text;
                        }
                        break;
                    case MildomMessageType.Disconnected:
                        if (options.IsMildomDisconnect)
                        {
                            name = null;
                            id = null;
                            comment = (MildomMessage as IMildomDisconnected).Text;
                        }
                        break;
                    case MildomMessageType.Comment:
                        if (options.IsMildomComment)
                        {
                            if (options.IsMildomCommentNickname)
                            {
                                name = (MildomMessage as IMildomComment).UserName;
                                id = (MildomMessage as IMildomComment).UserId;
                            }
                            comment = (MildomMessage as IMildomComment).CommentItems.ToText();
                        }
                        break;
                    case MildomMessageType.JoinRoom:
                        if (options.IsMildomJoin)
                        {
                            name = null;
                            id = null;
                            comment = (MildomMessage as IMildomJoinRoom).CommentItems.ToText();
                        }
                        break;
                        //case MildomMessageType.Leave:
                        //    if (_options.IsMildomLeave)
                        //    {
                        //        name = null;
                        //        comment = (MildomMessage as IMildomLeave).CommentItems.ToText();
                        //    }
                        //    break;
                }
            }
            else if (message is IShowRoomMessage showroomMessage)
            {
                site = "ShowRoom";
                switch (showroomMessage.ShowRoomMessageType)
                {
                    case ShowRoomMessageType.Comment:
                        if (options.IsShowRoomComment)
                        {
                            if (options.IsShowRoomCommentNickname)
                            {
                                name = (showroomMessage as IShowRoomComment).UserName;
                            }
                            comment = (showroomMessage as IShowRoomComment).Text;
                        }
                        break;
                    default:
                        break;
                }
            }
            else if (message is IBigoMessage bigoMessage)
            {
                site = "Bigo";
                switch (bigoMessage.BigoMessageType)
                {
                    case BigoMessageType.Comment:
                        if (options.IsBigoLiveComment)
                        {
                            if (options.IsBigoLiveCommentNickname)
                            {
                                name = (bigoMessage as IBigoComment).Name;
                            }
                            comment = (bigoMessage as IBigoComment).Message;
                        }
                        break;
                    default:
                        break;
                }
            }
            else
            {
                site = "";
            }
            return (name,id, comment,site);
        }
        public void OnMessageReceived(ISiteMessage message, IMessageMetadata messageMetadata)
        {
            if (!_options.IsEnabled || messageMetadata.IsNgUser || messageMetadata.IsInitialComment || (messageMetadata.Is184 && !_options.Want184Read))
                return;

            try
            {
                var (name, id , comment , site) = GetData(message, _options);

                //nameがnullでは無い場合かつUser.Nicknameがある場合はNicknameを採用
                if (!string.IsNullOrEmpty(name) && messageMetadata.User != null && !string.IsNullOrEmpty(messageMetadata.User.Nickname))
                {
                    name = messageMetadata.User.Nickname;
                }

                if (
                    (_wSocket_TwiChaDash == null) || (_wSocket_TwiChaDash.State != WebSocketState.Open)
                )
                {
                    PostData.Enqueue(new string[] { name, id, comment, site });
                    Connect_To_TwiChaDash();

                }
                else
                {
                    try
                    {
                        while(PostData.Count>0)
                        {
                            string[] _data = PostData.Dequeue();
                            TalkText(_data[0], _data[1], _data[2], _data[3]);
                        }

                        TalkText(name, id, comment, site);
                    }
                    catch (Exception ex)
                    {
                        if (_wSocket_TwiChaDash.State != WebSocketState.Open)
                        {
                            PostData.Enqueue(new string[] { name, id, comment, site });
                            Connect_To_TwiChaDash();
                        }
                    }
                }
            }
            catch(Exception ex)
            {

            }
        }

        private int TalkText(string name , string id , string comment, string site)
        {
            try
            {
                Dictionary<string, string> Data = new Dictionary<string, string>();
                Data.Add("Name", name);
                Data.Add("ID", id);
                Data.Add("Comment", comment);
                Data.Add("Site", site);
                _wSocket_TwiChaDash?.Send( JsonConvert.SerializeObject(Data));
            }
            catch (Exception ex)
            {

            }
            return 0;
        }

        public IPluginHost Host { get; set; }
        public string GetSettingsFilePath()
        {
            //ここでRemotingExceptionが発生。終了時の処理だが、既にHostがDisposeされてるのかも。
            var dir = Host.SettingsDirPath;
            return System.IO.Path.Combine(dir, $"{Name}.xml");
        }
        ConfigView _settingsView;
        public void ShowSettingView()
        {
            if (_settingsView == null)
            {
                _settingsView = new ConfigView
                {
                    DataContext = new ConfigViewModel(_options)
                };
            }
            _settingsView.Topmost = Host.IsTopmost;
            _settingsView.Left = Host.MainViewLeft;
            _settingsView.Top = Host.MainViewTop;

            _settingsView.Show();
        }
        public TwiChaDashPlugin()
        {

            _options = new Options();
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }
                if (_TwiChaDashProcess != null)
                {
                    _TwiChaDashProcess.Close();
                    _TwiChaDashProcess = null;
                }
                if (_wSocket_TwiChaDash != null)
                {
                    _wSocket_TwiChaDash.Close();
                    _wSocket_TwiChaDash.Dispose();
                    _wSocket_TwiChaDash = null;
                }
                _disposedValue = true;
            }
        }

        ~TwiChaDashPlugin()
        {
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {

            if (_wSocket_TwiChaDash != null) { _wSocket_TwiChaDash?.Dispose(); _wSocket_TwiChaDash = null; }

            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
    class OptionsLoader
    {
        public Options Load(string path)
        {
            var options = new Options();
            return options;
        }
        public void Save(Options options, string path)
        {

        }
    }
}

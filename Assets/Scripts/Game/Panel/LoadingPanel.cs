using System;
using System.Net;
using System.Text;
using BarrageGrab.Scripts;
using Game.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityWebSocket;

namespace Game.Panel
{
    public class LoadingPanel : MonoBehaviour
    {
        //public static bool IsLicenseQualified;

        public GameObject content;
        public TextMeshProUGUI loadingText;
        private float _loadingAnimTimer;
        private bool _showLoading;
        private WebSocket _webSocket;

        private float _timeSyncTimer = 999;
        void Start()
        {
            //IsLicenseQualified = true;
            _showLoading = true;
            const string webPath = "ws://127.0.0.1:8888";
            _webSocket = new WebSocket(webPath);
            _webSocket.OnOpen += WebsocketOnOpen;
            _webSocket.OnClose += WebsocketOnClose;
            _webSocket.OnMessage += WebsocketOnMessage;
            
            _webSocket.ConnectAsync();
        }

        private void WebsocketOnMessage(object sender, MessageEventArgs e)
        {
            if (!LicenseManager.Instance.IsLicenseValid)
            {
                return;
            }
            var decrypt = RC4.Decrypt(Encoding.UTF8.GetBytes("DKLSGLKWE"),e.RawData);
            var message = Encoding.UTF8.GetString(decrypt);
            var messageJObject = JsonConvert.DeserializeObject<JObject>(message);
            var value = messageJObject.Value<int>("Type");
            if (value == (int)EPackMsgType.礼物消息)
            {
                var giftData = messageJObject.Value<string>("Data");
                var deserializeObject = JsonConvert.DeserializeObject<JObject>(giftData);
                var time = deserializeObject.Value<long>("Time");
                CheckData(time);
            }
            
        }
        private void WebsocketOnClose(object sender, CloseEventArgs e)
        {
            if (!LicenseManager.Instance.IsLicenseValid)
            {
                return;
            }
            content.SetActive(true);
            loadingText.text = "断开连接！";
            loadingText.color = Color.red;
            _showLoading = false;
        }

        private void WebsocketOnOpen(object sender, OpenEventArgs e)
        {
            if (!LicenseManager.Instance.IsLicenseValid)
            {
                return;
            }
            content.SetActive(false);
            _showLoading = false;
        }

        // Update is called once per frame
        void Update()
        {
            if (!LicenseManager.Instance.IsLicenseValid)
            {
                content.SetActive(true);
                loadingText.text = "试用到期！";
                loadingText.color = Color.red;
                return;
            }
            if (_showLoading)
            {
                const float loadingAnimDuration = 0.618f;
                _loadingAnimTimer += Time.deltaTime;
                if (_loadingAnimTimer < loadingAnimDuration)
                {
                    loadingText.text = "连接中..";
                }else if (_loadingAnimTimer < 2 * loadingAnimDuration)
                {
                    loadingText.text = "连接中...";
                }else if (_loadingAnimTimer < 3 * loadingAnimDuration)
                {
                    loadingText.text = "连接中....";
                }else if (_loadingAnimTimer < 4 * loadingAnimDuration)
                {
                    loadingText.text = "连接中.";
                }
                else
                {
                    _loadingAnimTimer = 0;
                }
            }
            _timeSyncTimer += Time.deltaTime;
            if (_timeSyncTimer > 60)
            {
                _timeSyncTimer = 0;
                var dateTime = GetNetDateTime();
                CheckData(dateTime);
            }
        }

        private void OnDestroy()
        {
            _webSocket.CloseAsync();
        }

        private void CheckData(long timeStamp)
        {
            var nowMillisecond = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (Mathf.Abs(timeStamp - nowMillisecond) > 1000 * 60 * 30)
            {
                LicenseManager.Instance.IsLicenseValid = false;
                return;
            }
            LicenseManager.Instance.CheckLicenseValid(timeStamp);
        }

        private static long GetNetDateTime()
        {
            WebRequest request = null;
            WebResponse response = null;
            WebHeaderCollection headerCollection = null;
            var datetimeStr = string.Empty;
            try
            {
                request = WebRequest.Create("https://www.baidu.com");
                request.Timeout = 3000;
                request.Credentials = CredentialCache.DefaultCredentials;
                response = request.GetResponse();
                headerCollection = response.Headers;
                foreach (var h in headerCollection.AllKeys)
                {
                    if (h == "Date")
                    {
                        datetimeStr = headerCollection[h];
                    }
                }
                var dateTime = Convert.ToDateTime(datetimeStr);
                // 开始时间
                DateTime startTime = new(1970, 1, 1, 8, 0, 0);
                // 13位的时间戳
                var timeStamp = Convert.ToInt64(dateTime.Subtract(startTime).TotalMilliseconds);
                return timeStamp;
            }
            catch (Exception) { return 0; }
            finally
            {
                if (request != null)
                { request.Abort(); }
                if (response != null)
                { response.Close(); }
                if (headerCollection != null)
                { headerCollection.Clear(); }
            }
        }
    }
}

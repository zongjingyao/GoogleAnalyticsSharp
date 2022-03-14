using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

namespace GoogleAnalyticsSharp
{
    public class GoogleAnalytics
    {
        private const string BaseUrl = "https://www.google-analytics.com/g/collect";
        private const int MaxEventParamNameLength = 40;
        private const int MaxEventParamValueLength = 100;

        private static GoogleAnalytics instance;

        public static bool Initialized => instance != null && instance.config != null;

        private readonly Config config;
        private readonly string commonUrl;
        private readonly Random random = new Random();
        private uint sessionHitCount; // How many events has been reported in this session.
        private DateTime? lastEventTime;

        public static void Initialize(string userId, Config config, Dictionary<string, string> customCommonQueryParams = null)
        {
            if (Initialized)
                return;

            Debug.Log($"[{nameof(GoogleAnalytics)}] Initialize with config {config.Name}.");
            instance = new GoogleAnalytics(userId, config, customCommonQueryParams);
        }

        public static void SendEvent(string eventName, IDictionary<string, object> parameters = null)
        {
            if (!Initialized)
                return;

            instance.SendEventInternal(eventName, parameters);
        }

        private GoogleAnalytics(string userId, Config config, Dictionary<string, string> customCommonQueryParams)
        {
            this.config = config;
            Resolution currentResolution = Screen.currentResolution;

            Dictionary<string, string> commonQueryParams = new Dictionary<string, string>
            {
                ["v"] = "2", // protocol version
                ["gtm"] = "2oe4k0", // gtm hash
                ["_z"] = "ccd.NbB", // ???
                ["tid"] = config.MeasurementId, // measurement id
                ["uid"] = userId, // user id
                ["cid"] = SystemInfo.deviceUniqueIdentifier, // client id
                ["ul"] = Application.systemLanguage.ToString(), // user language
                ["sid"] = DateTime.UtcNow.Ticks.ToString(NumberFormatInfo.InvariantInfo), // session id
                ["sr"] = $"{currentResolution.width}x{currentResolution.height}", // screen resolution
                ["sct"] = "1", // session count
                ["seg"] = "1", // session engagement
                ["av"] = Application.version, // application version
                ["dl"] = "https://studio.yahaha.com/", // document location
                ["dt"] = "Yahaha Studio", // document title
            };
            if (config.DebugMode)
                commonQueryParams["_dbg"] = "1";
            if (customCommonQueryParams != null && customCommonQueryParams.Count > 0)
            {
                foreach (KeyValuePair<string, string> item in customCommonQueryParams)
                {
                    if (commonQueryParams.ContainsKey(item.Key))
                    {
                        Log($"You cannot use key: {item.Key}.", LogLevel.Error);
                        continue;
                    }
                    commonQueryParams[item.Key] = item.Value;
                }
            }
            string queryString = string.Join("&", commonQueryParams.Select(x => $"{x.Key}={x.Value}"));
            commonUrl = $"{BaseUrl}?{queryString}";
            SendDeviceInfo();
        }

        private void SendDeviceInfo()
        {
            const string eventName = "device_info";
            Dictionary<string, object> eventParams = new Dictionary<string, object>
            {
                ["device_model"] = SystemInfo.deviceModel,
                ["device_type"] = SystemInfo.deviceType,
                ["operating_system"] = SystemInfo.operatingSystem,
                ["processor_type"] = SystemInfo.processorType,
                ["processor_count"] = SystemInfo.processorCount,
                ["processor_frequency"] = SystemInfo.processorFrequency,
                ["system_memory_size"] = SystemInfo.systemMemorySize,
                ["graphics_device_id"] = SystemInfo.graphicsDeviceID,
                ["graphics_device_name"] = SystemInfo.graphicsDeviceName,
                ["graphics_shader_level"] = SystemInfo.graphicsShaderLevel,
                ["graphics_device_type"] = SystemInfo.graphicsDeviceType,
                ["graphics_memory_size"] = SystemInfo.graphicsMemorySize,
                ["graphics_device_vendor"] = SystemInfo.graphicsDeviceVendor
            };

            Dictionary<string, string> queryParams = GetQueryParams(eventName);
            queryParams["_ss"] = "1";
            queryParams["_fv"] = "1";
            queryParams["_nsi"] = "1";
            string url = GetUrl(queryParams, eventParams);
            SendRequest(url, null).Forget();
        }

        private Dictionary<string, string> GetQueryParams(string eventName)
        {
            sessionHitCount++;
            Dictionary<string, string> queryParams = new Dictionary<string, string>
            {
                ["en"] = eventName,
                ["_p"] = random.Next(int.MaxValue).ToString(NumberFormatInfo.InvariantInfo), // random value
                ["_s"] = sessionHitCount.ToString(NumberFormatInfo.InvariantInfo),
            };
            if (lastEventTime == null)
            {
                lastEventTime = DateTime.UtcNow;
                return queryParams;
            }

            DateTime now = DateTime.UtcNow;
            double deltaMilliseconds = now.Subtract(lastEventTime.Value).TotalMilliseconds;
            queryParams["_et"] = ((long)deltaMilliseconds).ToString(NumberFormatInfo.InvariantInfo);
            lastEventTime = now;

            return queryParams;
        }

        private string GetUrl(Dictionary<string, string> queryParams, IDictionary<string, object> eventParams)
        {
            if (queryParams.Count == 0)
                return commonUrl;

            string queryString = string.Join("&", queryParams.Select(x => $"{x.Key}={x.Value}"));
            if (eventParams == null || eventParams.Count == 0)
                return $"{commonUrl}&{queryString}";

            return $"{commonUrl}&{queryString}&" + string.Join("&",
                eventParams.Select(x =>
                {
                    string name = x.Key;
                    if (name.Length > MaxEventParamNameLength)
                        name = name.Substring(0, MaxEventParamNameLength);

                    string value = x.Value.ToString();
                    if (value.Length > MaxEventParamValueLength)
                        value = value.Substring(0, MaxEventParamValueLength);

                    return $"ep.{name}={value}";
                }));
        }

        private void SendEventInternal(string eventName, IDictionary<string, object> eventParams)
        {
            Dictionary<string, string> queryParams = GetQueryParams(eventName);
            string url = GetUrl(queryParams, eventParams);
            SendRequest(url, null).Forget();
        }

        private async UniTaskVoid SendRequest(string url, string body)
        {
            Log(url, LogLevel.Verbose);
            Log(body, LogLevel.Verbose);
            bool logResponse = config.LogLevel == LogLevel.Verbose;
            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            if (!string.IsNullOrEmpty(body))
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            if (logResponse)
                request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            try
            {
                await request.SendWebRequest();
            }
            catch (Exception ex)
            {
                Log(ex.Message, LogLevel.Info);
            }
            if (request.result == UnityWebRequest.Result.Success)
            {
                Log("Send request completed.", LogLevel.Verbose);
            }
            else
            {
                Log($"Send request failed: {request.error}", LogLevel.Error);
            }
            if (logResponse)
                Log(request.downloadHandler.text, LogLevel.Info);
        }

        private void Log(string log, LogLevel debugMode)
        {
            if (log == null || config.LogLevel < debugMode)
                return;

            log = $"[{nameof(GoogleAnalytics)}] {log}";
            switch (debugMode)
            {
                case LogLevel.Error:
                    Debug.LogError(log);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(log);
                    break;
                case LogLevel.Info:
                case LogLevel.Verbose:
                    Debug.Log(log);
                    break;
                default:
                    Debug.Log(log);
                    break;
            }
        }
    }
}

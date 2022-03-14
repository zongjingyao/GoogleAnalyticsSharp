using System;
using System.Linq;
using UnityEngine;

namespace GoogleAnalyticsSharp
{
    [CreateAssetMenu(fileName = "GoogleAnalyticsConfigs", menuName = "GoogleAnalyticsSharp/GoogleAnalyticsConfigs")]
    public class GoogleAnalyticsConfigs : ScriptableObject
    {
        [SerializeField] private Config[] configs;

        public Config Get(string configName)
        {
            return configs.FirstOrDefault(x => x != null && string.Equals(x.Name, configName, StringComparison.Ordinal));
        }
    }

    public enum LogLevel
    {
        Error,
        Warning,
        Info,
        Verbose
    };

    [Serializable]
    public class Config
    {
        [Tooltip("Config name")]
        [SerializeField] private string name;

        [Tooltip("The measurement ID associated with a stream. Found in the Google Analytics UI under:\nAdmin > Data Streams > choose your stream > Measurement ID")]
        [SerializeField] private string measurementId;

        [Tooltip("The log level. Default is WARNING.")]
        [SerializeField] private LogLevel logLevel = LogLevel.Warning;

        [SerializeField] private bool debugMode;

        public string Name => name;
        public string MeasurementId => measurementId;
        public LogLevel LogLevel => logLevel;
        public bool DebugMode => debugMode;
    }
}

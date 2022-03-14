using System.Collections.Generic;
using GoogleAnalyticsSharp;
using UnityEngine;
using UnityEngine.UI;

public class Sample : MonoBehaviour
{
    [SerializeField] private Button sendEventButton;

    private void Awake()
    {
        GoogleAnalyticsConfigs googleAnalyticsConfigs = Resources.Load<GoogleAnalyticsConfigs>("GoogleAnalyticsConfigs");
        Config config = googleAnalyticsConfigs.Get("Int");
        GoogleAnalytics.Initialize("zjy", config);

        sendEventButton.onClick.AddListener(OnSendEventButtonClicked);
    }

    private static void OnSendEventButtonClicked()
    {
        GoogleAnalytics.SendEvent("test_event", new Dictionary<string, object>
        {
            { "string_value", "this is a string" },
            { "int_value", 100 },
            { "float_value", 3.14f },
            { "bool_value", true },
        });
    }
}

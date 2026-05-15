#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClaudeSettingsPanel : MonoBehaviour
{
    public TMP_InputField apiKeyInput;
    public TMP_Dropdown modelDropdown;

    void Start()
    {
        if (SaveLoadHandler.Instance == null) return;
        if (apiKeyInput != null)
            apiKeyInput.text = SaveLoadHandler.Instance.data.claudeApiKey;
        if (modelDropdown != null)
        {
            string current = SaveLoadHandler.Instance.data.aiModel;
            for (int i = 0; i < modelDropdown.options.Count; i++)
            {
                if (modelDropdown.options[i].text == current)
                {
                    modelDropdown.value = i;
                    break;
                }
            }
        }
    }

    public void SaveSettings()
    {
        if (SaveLoadHandler.Instance == null) return;
        if (apiKeyInput != null)
        {
            SaveLoadHandler.Instance.data.claudeApiKey = apiKeyInput.text;
            MateEngine.ClaudeChatProvider.ApiKey = apiKeyInput.text;
        }
        if (modelDropdown != null)
            SaveLoadHandler.Instance.data.aiModel =
                modelDropdown.options[modelDropdown.value].text;
        SaveLoadHandler.Instance.SaveToDisk();
        Debug.Log("[VirtualBuddy] Settings saved.");
    }
}

#if UNITY_EDITOR

public class ClaudeSettingsPanelSetup
{
    [MenuItem("MateEngine/Setup Claude Settings Panel")]
    public static void CreatePanel()
    {
        GameObject chatBotAI = GameObject.Find("ChatBot AI");
        if (chatBotAI == null)
        {
            Debug.LogError("ChatBot AI not found in scene.");
            return;
        }

        GameObject panel = new GameObject("ClaudeSettingsPanel");
        panel.transform.SetParent(chatBotAI.transform, false);
        panel.SetActive(false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(420, 300);

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(15, 15, 15, 15);
        vlg.spacing = 8;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;

        // API Key Label
        CreateLabel(panel.transform, "API Key Label", "Claude API Key");

        // API Key Input
        TMP_InputField apiInput = CreateInputField(panel.transform, "API Key Input");
        apiInput.contentType = TMP_InputField.ContentType.Password;

        // Model Label
        CreateLabel(panel.transform, "Model Label", "AI Model");

        // Model Dropdown
        TMP_Dropdown dropdown = CreateDropdown(panel.transform, "Model Dropdown");
        dropdown.options.Clear();
        dropdown.options.Add(new TMP_Dropdown.OptionData("claude-sonnet-4-20250514"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("claude-opus-4-20250514"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("claude-haiku-4-5"));

        // Save Button
        Button saveBtn = CreateButton(panel.transform, "Save Button", "Save Settings");

        // Attach runtime component
        ClaudeSettingsPanel runtime = panel.AddComponent<ClaudeSettingsPanel>();
        runtime.apiKeyInput = apiInput;
        runtime.modelDropdown = dropdown;

        // Wire save button
        saveBtn.onClick.AddListener(runtime.SaveSettings);

        Debug.Log("[VirtualBuddy] ClaudeSettingsPanel created.");
    }

    static void CreateLabel(Transform parent, string name, string text)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 14;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 24);
    }

    static TMP_InputField CreateInputField(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 36);
        go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var input = go.AddComponent<TMP_InputField>();
        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 13;
        input.textComponent = tmp;
        return input;
    }

    static TMP_Dropdown CreateDropdown(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 36);
        go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
        return go.AddComponent<TMP_Dropdown>();
    }

    static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
        go.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.9f, 1f);
        Button btn = go.AddComponent<Button>();
        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        return btn;
    }
}
#endif
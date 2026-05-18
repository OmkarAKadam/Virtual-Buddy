using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace MateEngine
{
    public class CompanionActionRouter : MonoBehaviour
    {
        public string systemPrompt = "You are Virtual Buddy, a desktop companion. Be VERY concise - max 8 words per response. For actions output raw JSON first then 3-4 word confirmation. Format for search: {\"action\":\"search\",\"query\":\"...\"} then write 'Searching for X...' only. For chat: reply in max 8 words. No markdown, no bullets, no lists, no long explanations.";
        public TextMeshProUGUI responseText;
        public TMP_InputField inputField;

        private IAIChatProvider claudeProvider;
        private CompanionServerClient serverClient;
        private AvatarActionAnimationController animController;

        private async void Start()
        {
            try
            {
                // claudeProvider = new ClaudeChatProvider();
                claudeProvider = new NvidiaProxyChatProvider();
                serverClient = CompanionServerClient.Instance;
                animController = AvatarActionAnimationController.Instance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing CompanionActionRouter: {ex.Message}");
            }
        }

        public async Task SendChatMessage(string userMessage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userMessage))
                    return;

                if (responseText != null)
                    responseText.text = "Thinking...";

                string response = await claudeProvider.SendMessageAsync(userMessage, systemPrompt);
                response = await ParseAndExecute(response);
                // Truncate to first sentence only for display
                if (response.Length > 60)
                {
                    int periodIdx = response.IndexOf('.');
                    int exclamIdx = response.IndexOf('!');
                    int questionIdx = response.IndexOf('?');
                    int cutAt = response.Length;
                    if (periodIdx > 0 && periodIdx < cutAt) cutAt = periodIdx + 1;
                    if (exclamIdx > 0 && exclamIdx < cutAt) cutAt = exclamIdx + 1;
                    if (questionIdx > 0 && questionIdx < cutAt) cutAt = questionIdx + 1;
                    if (cutAt < response.Length) response = response.Substring(0, cutAt).Trim();
                }

                if (responseText != null)
                    responseText.text = response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending message: {ex.Message}");
                if (responseText != null)
                    responseText.text = $"Error: {ex.Message}";
            }
        }

        private async Task<string> ParseAndExecute(string response)
        {
            try
            {
                response = response.Replace("```json", "").Replace("```", "").Trim();
                response = response.Replace("**Action Block:**", "").Trim();
                // Also handle "type" field as alias for "action"
                response = response.Replace("\"type\": \"search\"", "\"action\": \"search\"");
                response = response.Replace("\"type\": \"type\"", "\"action\": \"type\"");
                response = response.Replace("\"type\": \"notify\"", "\"action\": \"notify\"");

                // Detect action - handle both "action":"search" and "action": "search"
                bool isSearch = response.Contains("\"action\":\"search\"") || response.Contains("\"action\": \"search\"");
                bool isType = response.Contains("\"action\":\"type\"") || response.Contains("\"action\": \"type\"");
                bool isNotify = response.Contains("\"action\":\"notify\"") || response.Contains("\"action\": \"notify\"");
                bool isClipboard = response.Contains("\"action\":\"clipboard_write\"") || response.Contains("\"action\": \"clipboard_write\"");

                if (isSearch)
                {
                    string queryKey = response.Contains("\"query\": \"") ? "\"query\": \"" : "\"query\":\"";
                    int startIndex = response.IndexOf(queryKey) + queryKey.Length;
                    int endIndex = response.IndexOf("\"", startIndex);
                    string query = response.Substring(startIndex, endIndex - startIndex);
                    await serverClient.SearchAsync(query);
                    if (animController != null) animController.StartTyping();
                    await Task.Delay(3000);
                    if (animController != null) animController.StopTyping();
                }
                else if (isType)
                {
                    string textKey = response.Contains("\"text\": \"") ? "\"text\": \"" : "\"text\":\"";
                    int startIndex = response.IndexOf(textKey) + textKey.Length;
                    int endIndex = response.IndexOf("\"", startIndex);
                    string text = response.Substring(startIndex, endIndex - startIndex);
                    await serverClient.TypeTextAsync(text);
                    if (animController != null) animController.StartTyping();
                    await Task.Delay(2000);
                    if (animController != null) animController.StopTyping();
                }
                else if (isNotify)
                {
                    string titleKey = response.Contains("\"title\": \"") ? "\"title\": \"" : "\"title\":\"";
                    int titleStart = response.IndexOf(titleKey) + titleKey.Length;
                    int titleEnd = response.IndexOf("\"", titleStart);
                    string title = response.Substring(titleStart, titleEnd - titleStart);
                    string msgKey = response.Contains("\"message\": \"") ? "\"message\": \"" : "\"message\":\"";
                    int msgStart = response.IndexOf(msgKey) + msgKey.Length;
                    int msgEnd = response.IndexOf("\"", msgStart);
                    string message = response.Substring(msgStart, msgEnd - msgStart);
                    await serverClient.NotifyAsync(title, message);
                }
                else if (isClipboard)
                {
                    string textKey = response.Contains("\"text\": \"") ? "\"text\": \"" : "\"text\":\"";
                    int startIndex = response.IndexOf(textKey) + textKey.Length;
                    int endIndex = response.IndexOf("\"", startIndex);
                    string text = response.Substring(startIndex, endIndex - startIndex);
                    await serverClient.WriteClipboardAsync(text);
                }

                // Strip JSON block before displaying
                if (isSearch || isType || isNotify || isClipboard)
                {
                    int jsonEnd = response.LastIndexOf("}") + 1;
                    response = response.Substring(jsonEnd).Trim();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing and executing: {ex.Message}");
            }

            return response;
        }

        public void OnSubmitMessage()
        {
            try
            {
                string message = inputField.text;
                inputField.text = "";
                _ = SendChatMessage(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error submitting message: {ex.Message}");
            }
        }
    }
}
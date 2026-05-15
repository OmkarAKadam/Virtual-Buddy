using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace MateEngine
{
    public class CompanionActionRouter : MonoBehaviour
    {
        public string systemPrompt = "You are Virtual Buddy, a helpful desktop companion. You are friendly, concise, and proactive. When the user asks you to DO something (search, type, notify), you respond with a JSON action block AND a friendly message. Otherwise just chat.";
        public TextMeshProUGUI responseText;
        public TMP_InputField inputField;

        private ClaudeChatProvider claudeProvider;
        private CompanionServerClient serverClient;
        private AvatarActionAnimationController animController;

        private async void Start()
        {
            try
            {
                claudeProvider = new ClaudeChatProvider();
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
                if (response.Contains("{\"action\":\"search\""))
                {
                    // Extract query value
                    int startIndex = response.IndexOf("\"query\":\"") + 9;
                    int endIndex = response.IndexOf("\"", startIndex);
                    string query = response.Substring(startIndex, endIndex - startIndex);

                    // Execute search action
                    string searchResult = await serverClient.SearchAsync(query);

                    // Show typing animation
                    if (animController != null) animController.StartTyping();
                    await Task.Delay(3000);
                    if (animController != null) animController.StopTyping();
                }
                else if (response.Contains("{\"action\":\"type\""))
                {
                    // Extract text value
                    int startIndex = response.IndexOf("\"text\":\"") + 9;
                    int endIndex = response.IndexOf("\"", startIndex);
                    string text = response.Substring(startIndex, endIndex - startIndex);

                    // Execute type action
                    string typeResult = await serverClient.TypeTextAsync(text);

                    // Show typing animation
                    if (animController != null) animController.StartTyping();
                    await Task.Delay(2000);
                    if (animController != null) animController.StopTyping();
                }
                else if (response.Contains("{\"action\":\"notify\""))
                {
                    // Extract title and message values
                    int titleStartIndex = response.IndexOf("\"title\":\"") + 9;
                    int titleEndIndex = response.IndexOf("\"", titleStartIndex);
                    string title = response.Substring(titleStartIndex, titleEndIndex - titleStartIndex);

                    int messageStartIndex = response.IndexOf("\"message\":\"") + 11;
                    int messageEndIndex = response.IndexOf("\"", messageStartIndex);
                    string message = response.Substring(messageStartIndex, messageEndIndex - messageStartIndex);

                    // Execute notification action
                    string notifyResult = await serverClient.NotifyAsync(title, message);
                }
                else if (response.Contains("{\"action\":\"clipboard_write\""))
                {
                    // Extract text value
                    int startIndex = response.IndexOf("\"text\":\"") + 8;
                    int endIndex = response.IndexOf("\"", startIndex);
                    string text = response.Substring(startIndex, endIndex - startIndex);

                    // Execute clipboard write action
                    string clipboardResult = await serverClient.WriteClipboardAsync(text);
                }

                // Strip the JSON block from response before displaying text
                if (response.Contains("{\"action\":"))
                {
                    int jsonEndIndex = response.LastIndexOf("}") + 1;
                    response = response.Substring(jsonEndIndex).Trim();
                }

                if (responseText != null)
                    responseText.text = response;
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
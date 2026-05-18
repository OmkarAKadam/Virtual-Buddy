using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MateEngine
{
    public class NvidiaProxyChatProvider : IAIChatProvider
    {
        public static string ApiKey = "";

        private const string ApiUrl = "https://integrate.api.nvidia.com/v1/chat/completions";
        private const string Model = "meta/llama-3.1-8b-instruct";
        private const int MaxTokens = 1024;

        private List<MessageData> history = new List<MessageData>();

        public string ProviderName => "NvidiaProxy";

        public bool IsAvailable()
        {
            return !string.IsNullOrEmpty(ApiKey);
        }

        public void ClearHistory()
        {
            history.Clear();
        }

        public async Task<string> SendMessageAsync(string userMessage, string systemPrompt)
        {
            try
            {
                if (!IsAvailable())
                {
                    return "API key not set";
                }

                // Add user message to history
                history.Add(new MessageData { role = "user", content = userMessage });

                // Build JSON manually to handle nested arrays properly
                StringBuilder jsonBuilder = new StringBuilder();
                jsonBuilder.Append("{");
                jsonBuilder.Append("\"model\":\"").Append(Model).Append("\",");
                jsonBuilder.Append("\"max_tokens\":").Append(MaxTokens).Append(",");
                jsonBuilder.Append("\"stream\":false,");
                jsonBuilder.Append("\"messages\":[");

                // Add system message first
                jsonBuilder.Append("{\"role\":\"system\",\"content\":\"").Append(EscapeJsonString(systemPrompt)).Append("\"},");

                // Build messages array from history
                for (int i = 0; i < history.Count; i++)
                {
                    if (i > 0)
                        jsonBuilder.Append(",");
                    jsonBuilder.Append("{");
                    jsonBuilder.Append("\"role\":\"").Append(history[i].role).Append("\",");
                    jsonBuilder.Append("\"content\":\"").Append(EscapeJsonString(history[i].content)).Append("\"");
                    jsonBuilder.Append("}");
                }
                jsonBuilder.Append("]}");

                string jsonData = jsonBuilder.ToString();

                using (UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", "Bearer " + ApiKey);

                    UnityWebRequestAsyncOperation operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    if (request.result == UnityWebRequest.Result.Success)
{
    string rawResponse = request.downloadHandler.text;

    int choicesIndex = rawResponse.IndexOf("\"choices\":");
    if (choicesIndex != -1)
    {
        int contentIndex = rawResponse.IndexOf("\"content\":\"", choicesIndex);
        if (contentIndex != -1)
        {
            int startIdx = contentIndex + 11;
            // Find closing quote handling escaped quotes
            int endIdx = startIdx;
            while (endIdx < rawResponse.Length)
            {
                endIdx = rawResponse.IndexOf("\"", endIdx);
                if (endIdx == -1) break;
                int backslashes = 0;
                int check = endIdx - 1;
                while (check >= 0 && rawResponse[check] == '\\') { backslashes++; check--; }
                if (backslashes % 2 == 0) break;
                endIdx++;
            }
            if (endIdx > startIdx && endIdx != -1)
            {
                string extracted = rawResponse.Substring(startIdx, endIdx - startIdx);
                Debug.Log("[NvidiaProxy] Raw content: " + extracted.Substring(0, Mathf.Min(150, extracted.Length)));
                extracted = extracted.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
                history.Add(new MessageData { role = "assistant", content = extracted });
                return extracted;
            }
        }
    }

    Debug.Log("[NvidiaProxy] Failed - raw: " + rawResponse.Substring(0, Mathf.Min(500, rawResponse.Length)));
    return "Failed to parse response";
}
else
{
    return $"Error: {request.error} - {request.downloadHandler.text}";
}
                }
            }
            catch (Exception e)
            {
                return $"Exception: {e.Message}";
            }
        }

        private string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        [System.Serializable]
        public class MessageData
        {
            public string role;
            public string content;
        }
    }
}
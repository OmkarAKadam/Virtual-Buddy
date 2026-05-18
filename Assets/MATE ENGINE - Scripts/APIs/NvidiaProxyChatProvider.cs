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

                        // Parse response - find "content":" in the JSON and extract the value
                        int contentIndex = rawResponse.IndexOf("\"content\":\"");
                        if (contentIndex != -1)
                        {
                            int startIndex = contentIndex + 11; // Length of "\"content\":\""
                            int endIndex = rawResponse.IndexOf("\"", startIndex);
                            if (endIndex != -1)
                            {
                                string content = rawResponse.Substring(startIndex, endIndex - startIndex);
                                content = content.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");

                                // Add assistant response to history
                                history.Add(new MessageData { role = "assistant", content = content });
                                return content;
                            }
                        }

                        Debug.Log("[NvidiaProxy] Failed to parse response - raw: " + rawResponse.Substring(0, Mathf.Min(500, rawResponse.Length)));
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
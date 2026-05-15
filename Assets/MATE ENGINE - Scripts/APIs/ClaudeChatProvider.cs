using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MateEngine
{
    public class ClaudeChatProvider : IAIChatProvider
    {
        public static string ApiKey = "";

        private const string ApiUrl = "https://api.anthropic.com/v1/messages";
        private const string Model = "claude-sonnet-4-20250514";
        private const int MaxTokens = 1024;

        private List<MessageData> history = new List<MessageData>();

        public string ProviderName => "Claude";

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
                jsonBuilder.Append("\"system\":\"").Append(EscapeJsonString(systemPrompt)).Append("\",");
                jsonBuilder.Append("\"messages\":[");

                // Build messages array
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
                    request.SetRequestHeader("x-api-key", ApiKey);
                    request.SetRequestHeader("anthropic-version", "2023-06-01");

                    UnityWebRequestAsyncOperation operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string responseJson = request.downloadHandler.text;
                        var response = JsonUtility.FromJson<ResponseData>(responseJson);

                        if (response.content != null && response.content.Length > 0)
                        {
                            string reply = response.content[0].text;
                            // Add assistant response to history
                            history.Add(new MessageData { role = "assistant", content = reply });
                            return reply;
                        }
                        else
                        {
                            return "No response content received";
                        }
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
        private class MessageData
        {
            public string role;
            public string content;
        }

        [System.Serializable]
        private class RequestData
        {
            public string model;
            public int max_tokens;
            public MessageData[] messages;
            public string system;
        }

        [System.Serializable]
        private class ResponseContent
        {
            public string type;
            public string text;
        }

        [System.Serializable]
        private class ResponseData
        {
            public string id;
            public string type;
            public string role;
            public ResponseContent[] content;
            public string model;
            public string stop_reason;
            public string stop_sequence;
            public UsageData usage;
        }

        [System.Serializable]
        private class UsageData
        {
            public int input_tokens;
            public int output_tokens;
        }
    }
}
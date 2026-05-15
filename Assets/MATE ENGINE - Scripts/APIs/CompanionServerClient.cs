using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MateEngine
{
    public class CompanionServerClient : MonoBehaviour
    {
        public static CompanionServerClient Instance { get; private set; }

        public string serverUrl = "http://127.0.0.1:5000";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public async Task<string> SearchAsync(string query)
        {
            string jsonData = "{\"query\": \"" + query + "\"}";
            string result = await SendRequestAsync("/search", "POST", jsonData);
            return result;
        }

        public async Task<string> ReadClipboardAsync()
        {
            string result = await SendRequestAsync("/clipboard/read", "POST", "{}");
            return result;
        }

        public async Task<string> WriteClipboardAsync(string text)
        {
            string jsonData = "{\"text\": \"" + text + "\"}";
            string result = await SendRequestAsync("/clipboard/write", "POST", jsonData);
            return result;
        }

        public async Task<string> TypeTextAsync(string text)
        {
            string jsonData = "{\"text\": \"" + text + "\"}";
            string result = await SendRequestAsync("/type", "POST", jsonData);
            return result;
        }

        public async Task<string> NotifyAsync(string title, string message)
        {
            string jsonData = "{\"title\": \"" + title + "\", \"message\": \"" + message + "\"}";
            string result = await SendRequestAsync("/notify", "POST", jsonData);
            return result;
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                string result = await SendRequestAsync("/health", "GET", "");
                bool success = result.Contains("\"success\":true");
                return success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<string> SendRequestAsync(string endpoint, string method, string jsonData)
        {
            var tcs = new TaskCompletionSource<string>();

            // Use Task.Yield to ensure we can switch to the main thread for Unity operations
            await Task.Yield();

            using (UnityWebRequest request = new UnityWebRequest(serverUrl + endpoint, method))
            {
                if (method == "POST" && !string.IsNullOrEmpty(jsonData))
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                request.downloadHandler = new DownloadHandlerBuffer();

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();

                // Wait for the request to complete
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    return "Error: " + request.error;
                }
                else
                {
                    return request.downloadHandler.text;
                }
            }
        }
    }
}
using System.Threading.Tasks;
using UnityEngine;

namespace MateEngine
{
    public interface IAIChatProvider
    {
        Task<string> SendMessageAsync(string userMessage, string systemPrompt);
        void ClearHistory();
        bool IsAvailable();
        string ProviderName { get; }
    }
}
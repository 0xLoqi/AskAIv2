using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core;

public class Msg
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ChatClient
{
    public async Task<string> SendAsync(List<Msg> messages)
    {
        await Task.Delay(100); // Simulate async work
        return "This is a dummy reply.";
    }
} 
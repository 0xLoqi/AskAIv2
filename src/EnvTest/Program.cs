using Core;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Test .env loader
        Class1.TestEnvLoader();

        // Test ChatClient
        var client = new ChatClient();
        var messages = new List<Msg> { new Msg { Role = "user", Content = "Hello!" } };
        var reply = await client.SendAsync(messages);
        System.Console.WriteLine($"ChatClient reply: {reply}");
    }
}

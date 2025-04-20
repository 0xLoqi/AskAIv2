namespace Core;

using dotenv.net;

public class Class1
{
    public static void TestEnvLoader()
    {
        DotEnv.Load();
        var key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Console.WriteLine($"OPENAI_API_KEY: {key}");
    }
}

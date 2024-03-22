using SpotifySongSync_Server.Services;

Console.Title = "SpotifySongSync Server | ver: 1.0.0 | by Fl0rixn";

try
{
    Console.WriteLine("Starting Server...");

    new TcpService("45.145.41.236:8337");

    Console.WriteLine("Listening on: 45.145.41.236:8337");
    Console.WriteLine("Server started!");
    Console.WriteLine("Press enter to close.");
} catch(Exception ex)
{
    Console.WriteLine($"{ex}");
}

Console.ReadLine();
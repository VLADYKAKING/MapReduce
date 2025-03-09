using Grpc.Core;
using MapReduce;

namespace Master
{
    class Program
    {
        static int Port = 50010;
        static void Main(string[] args)
        {
            if (args.Any())
                Port = int.Parse(args[0]);

            Server server = new Server
            {
                Services = { MasterService.BindService(new MasterServiceImpl()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine($"Master server запущен - localhost:{Port}");
            Console.WriteLine("Нажмите любую клавишу для остановки сервера...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}

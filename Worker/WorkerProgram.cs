using Grpc.Core;
using MapReduce;
using Channel = Grpc.Core.Channel;

namespace Worker
{

    class Program
    {
        static int Port = 50021;
        static int MasterPort = 50010;
        static WorkerType WorkerType = WorkerType.Map;
        static void Main(string[] args)
        {
            if (args.Any())
            {
                Port = int.Parse(args[0]);
                MasterPort = int.Parse(args[1]);
                Enum.TryParse(args[2], true, out WorkerType);
            }

            Register(WorkerType);

            Server server = new Server
            {
                Services = { WorkerService.BindService(new WorkerServiceImpl(WorkerType)) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine($"Worker-������ ������� � �������� ����� localhost:{Port}");
            Console.WriteLine("������� ����� ������� ��� ���������...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }

        /// <summary>
        /// ����������� ������� ��������� ���� �� master �������
        /// </summary>
        static void Register(WorkerType workerType)
        {
            Channel channel = new Channel($"localhost:{MasterPort}", ChannelCredentials.Insecure);
            var client = new MasterService.MasterServiceClient(channel);
            var reply = client.RegisterWorker(new WorkerRegistrationRequest
            {
                WorkerId = Guid.NewGuid().ToString(),
                WorkerType = workerType,
                Address = $"localhost:{Port}"
            });
            Console.WriteLine($"������ �����������: {reply.Status}");
            channel.ShutdownAsync().Wait();
        }
    }
}

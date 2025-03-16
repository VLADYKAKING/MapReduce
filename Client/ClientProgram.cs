using System.Runtime.Serialization;
using System.Xml;
using Google.Protobuf;
using Grpc.Core;
using MapReduce;
using Shared;

namespace Client
{
    class Program
    {
        static int MasterPort = 50010;
        static string? FilePath;

        static void Main(string[] args)
        {
            if (args.Any())
            {
                MasterPort = int.Parse(args[0]);
                FilePath = args[1];
            }

            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".vs"))
                          && !File.Exists(Path.Combine(dir.FullName, "*.sln")))
            {
                dir = dir.Parent;
            }
            if (dir != null)
            {
                FilePath = Path.Combine(dir.FullName, "sample.txt");
            }

            Console.WriteLine("������� ����� ������� ��� ������...");
            Console.ReadKey();

            byte[] mapFunction = SerializeFunction(new WordCountMapFunction());
            byte[] reduceFunction = SerializeFunction(new WordCountReduceFunction());

            Channel channel = new Channel($"localhost:{MasterPort}", ChannelCredentials.Insecure);
            var client = new MasterService.MasterServiceClient(channel);

            Console.WriteLine("�������� ������� �� Master-������...");
            var reply = client.SubmitJob(new JobSubmissionRequest
            {
                MapFunction = ByteString.CopyFrom(mapFunction),
                ReduceFunction = ByteString.CopyFrom(reduceFunction),
                FilePath = FilePath
            });
            Console.WriteLine($"���������: {reply.Result} ���� � ������");

            channel.ShutdownAsync().Wait();
            Console.WriteLine("������� ����� ������� ��� ������...");
            Console.ReadKey();
        }

        /// <summary>
        /// ������������ ������� � ����-������
        /// </summary>
        static byte[] SerializeFunction<T>(T func)
        {
            var serializer = new DataContractSerializer(typeof(T));
            using var ms = new MemoryStream();
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(ms))
            {
                serializer.WriteObject(writer, func);
            }
            return ms.ToArray();
        }
    }


}

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
        static string FilePath = "C:\\Users\\V\\Desktop\\MapReduce\\sample.txt";

        static void Main(string[] args)
        {
            if (args.Any())
            {
                MasterPort = int.Parse(args[0]);
                FilePath = args[1];
            }

            Console.WriteLine("Нажмите любую клавишу для начала...");
            Console.ReadKey();

            byte[] mapFunction = SerializeFunction(new WordCountMapFunction());
            byte[] reduceFunction = SerializeFunction(new WordCountReduceFunction());

            Channel channel = new Channel($"localhost:{MasterPort}", ChannelCredentials.Insecure);
            var client = new MasterService.MasterServiceClient(channel);

            Console.WriteLine("Отправка задания на Master-сервер...");
            var reply = client.SubmitJob(new JobSubmissionRequest
            {
                MapFunction = ByteString.CopyFrom(mapFunction),
                ReduceFunction = ByteString.CopyFrom(reduceFunction),
                FilePath = FilePath
            });
            Console.WriteLine($"Результат: {reply.Result} слов в тексте");

            channel.ShutdownAsync().Wait();
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        /// <summary>
        /// Сериализация функции в байт-массив
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

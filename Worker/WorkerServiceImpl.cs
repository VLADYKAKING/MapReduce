using System.Runtime.Serialization;
using System.Text.Json;
using System.Xml;
using Grpc.Core;
using MapReduce;
using Shared;

namespace Worker
{
    public class WorkerServiceImpl : WorkerService.WorkerServiceBase
    {
        private readonly WorkerType workerType;

        public WorkerServiceImpl(WorkerType workerType)
        {
            this.workerType = workerType;
        }

        public override async Task<TaskRequestReply> ExecuteTask(TaskRequest request, ServerCallContext context)
        {
            Console.WriteLine($"Worker типа '{workerType}' получил задание с контекстом: {request.ContextData.Substring(0, 50)}...");

            if (workerType == WorkerType.Map)
            {
                WordCountMapFunction mapFunc = DeserializeFunction<WordCountMapFunction>(request.FunctionData.ToByteArray());
                Dictionary<string, int> resultDict = mapFunc.Map(request.ContextData);
                string jsonResult = JsonSerializer.Serialize(resultDict);
                Console.WriteLine("Map-задача выполнена.");
                return new TaskRequestReply { ResultData = jsonResult };
            }
            else if (workerType == WorkerType.Reduce)
            {
                WordCountReduceFunction reduceFunc = DeserializeFunction<WordCountReduceFunction>(request.FunctionData.ToByteArray());
                Dictionary<string, int>[] partialResults = JsonSerializer.Deserialize<Dictionary<string, int>[]>(request.ContextData);
                Dictionary<string, int> finalResult = reduceFunc.Reduce(partialResults);
                Console.WriteLine("Reduce-задача выполнена.");
                return new TaskRequestReply { ResultData = JsonSerializer.Serialize(finalResult) };
            }
            else
            {
                Console.WriteLine("Неизвестный тип воркера.");
                return new TaskRequestReply { ResultData = "Ошибка: неизвестный тип воркера" };
            }
        }

        /// <summary>
        /// Десериализация функции из байт-массива
        /// </summary>
        private T DeserializeFunction<T>(byte[] data)
        {
            var serializer = new DataContractSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(data))
            using (XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(ms, XmlDictionaryReaderQuotas.Max))
            {
                return (T)serializer.ReadObject(reader);
            }
        }
    }

}

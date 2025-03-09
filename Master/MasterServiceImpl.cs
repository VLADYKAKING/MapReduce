using Grpc.Core;
using MapReduce;

namespace Master
{
    public class MasterServiceImpl : MasterService.MasterServiceBase
    {
        private List<WorkerInfo> registeredMapWorkers = new List<WorkerInfo>();
        private List<WorkerInfo> registeredReduceWorkers = new List<WorkerInfo>();

        /// <summary>
        /// Регистрация воркера
        /// </summary>
        public override Task<WorkerRegistrationReply> RegisterWorker(WorkerRegistrationRequest request, ServerCallContext context)
        {
            Console.WriteLine($"Регистрируем Worker-а типа '{request.WorkerType}'");
            WorkerInfo info = new WorkerInfo { WorkerId = request.WorkerId, WorkerType = request.WorkerType, Address = request.Address };

            if (request.WorkerType == WorkerType.Map)
            {
                registeredMapWorkers.Add(info);
            }
            else if (request.WorkerType == WorkerType.Reduce)
            {
                registeredReduceWorkers.Add(info);
            }

            return Task.FromResult(new WorkerRegistrationReply { Status = "Registered" });
        }

        /// <summary>
        /// Запуск задачи
        /// </summary>
        public override async Task<JobSubmissionReply> SubmitJob(JobSubmissionRequest request, ServerCallContext context)
        {
            Console.WriteLine("Получена задача от клиента.");

            // Читаем файл
            string fileContent;
            try
            {
                fileContent = File.ReadAllText(request.FilePath);
                Console.WriteLine("Файл успешно прочитан.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
                return new JobSubmissionReply { Result = "Ошибка чтения файла" };
            }

            // Проверяем Map воркеры
            int mapWorkerCount = registeredMapWorkers.Count;
            if (mapWorkerCount == 0)
            {
                Console.WriteLine("Нет зарегистрированных Map воркеров.");
                return new JobSubmissionReply { Result = "Ошибка: Нет доступных Map воркеров." };
            }

            // Разбиваем текст на части
            int partLength = fileContent.Length / mapWorkerCount;
            List<string> parts = new List<string>();
            for (int i = 0; i < mapWorkerCount; i++)
            {
                int start = i * partLength;
                int length = (i == mapWorkerCount - 1) ? fileContent.Length - start : partLength;
                parts.Add(fileContent.Substring(start, length));
            }

            // Рассылаем Map задачи всем Map воркерам
            List<Task<string>> mapTasks = new List<Task<string>>();
            for (int i = 0; i < mapWorkerCount; i++)
            {
                var worker = registeredMapWorkers[i];
                var channel = new Channel(worker.Address, ChannelCredentials.Insecure);
                var workerClient = new WorkerService.WorkerServiceClient(channel);

                // Формируем запрос для Map задачи:
                // ContextData – часть текста, FunctionData – сериализованная функция Map (переданная клиентом)
                var taskRequest = new TaskRequest
                {
                    ContextData = parts[i],
                    FunctionData = request.MapFunction
                };

                Console.WriteLine($"Отправка Map задачи на воркер {worker.WorkerId}.");
                // Запуск и регистрация задачи
                var task = workerClient.ExecuteTaskAsync(taskRequest).ResponseAsync.ContinueWith(t =>
                {
                    channel.ShutdownAsync().Wait();
                    return t.Result.ResultData;
                });
                mapTasks.Add(task);
            }

            // Ожидаем завершения всех Map задач
            string[] mapResults = await Task.WhenAll(mapTasks);
            Console.WriteLine("Все Map задачи завершены.");

            // Проверяем Reduce воркеры
            int reduceWorkerCount = registeredReduceWorkers.Count;
            if (reduceWorkerCount == 0)
            {
                Console.WriteLine("Нет зарегистрированных Reduce воркеров.");
                return new JobSubmissionReply { Result = "Ошибка: Нет доступных Reduce воркеров." };
            }

            // Если результатов Map задач больше одного, то распределяем их по группам для каждого reduce воркера
            // (Каждый результат – JSON строка, представляющая Dictionary<string, int>)
            List<string> partialReduceResults = null;
            if (mapResults.Length > 1)
            {
                List<List<string>> groups = new List<List<string>>();
                for (int i = 0; i < reduceWorkerCount; i++)
                {
                    groups.Add(new List<string>());
                }

                for (int i = 0; i < mapResults.Length; i++)
                {
                    groups[i % reduceWorkerCount].Add(mapResults[i]);
                }

                // Отправляем каждую группу на reduce воркера
                List<Task<string>> partialReduceTasks = new List<Task<string>>();
                for (int i = 0; i < reduceWorkerCount; i++)
                {
                    var reduceWorker = registeredReduceWorkers[i];
                    var reduceChannel = new Channel(reduceWorker.Address, ChannelCredentials.Insecure);
                    var reduceClient = new WorkerService.WorkerServiceClient(reduceChannel);

                    // Формируем JSON массив из map-результатов для данной группы
                    string groupJson = "[" + string.Join(",", groups[i]) + "]";
                    var reduceTaskRequest = new TaskRequest
                    {
                        ContextData = groupJson,
                        FunctionData = request.ReduceFunction
                    };

                    Console.WriteLine($"Отправка Reduce задачи на воркер {reduceWorker.WorkerId}");
                    var task = reduceClient.ExecuteTaskAsync(reduceTaskRequest).ResponseAsync.ContinueWith(t =>
                    {
                        reduceChannel.ShutdownAsync().Wait();
                        return t.Result.ResultData;
                    });
                    partialReduceTasks.Add(task);
                }

                // Ожидаем выполнения всех Reduce задач
                partialReduceResults = (await Task.WhenAll(partialReduceTasks)).ToList();
                Console.WriteLine("Reduce задачи завершены.");
            }

            // Объединяем частичные результаты и отправляем их на одного reduce воркера для окончательной агрегации результатов
            var finalReduceWorker = registeredReduceWorkers[0];
            var finalReduceChannel = new Channel(finalReduceWorker.Address, ChannelCredentials.Insecure);
            var finalReduceClient = new WorkerService.WorkerServiceClient(finalReduceChannel);

            string finalGroupJson = "[" + string.Join(",", partialReduceResults?.ToArray() ?? mapResults) + "]";
            var finalReduceTaskRequest = new TaskRequest
            {
                ContextData = finalGroupJson,
                FunctionData = request.ReduceFunction
            };

            Console.WriteLine($"Отправка финальной Reduce задачи на воркер {finalReduceWorker.WorkerId}");
            var finalReduceReply = await finalReduceClient.ExecuteTaskAsync(finalReduceTaskRequest);
            await finalReduceChannel.ShutdownAsync();

            Console.WriteLine("Reduce задача завершена.");

            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(finalReduceReply.ResultData);
            var count = dict?.Sum(x => x.Value);

            return new JobSubmissionReply { Result = count.ToString() };
        }

    }

}

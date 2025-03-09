using System.Runtime.Serialization;

namespace Shared
{
    [DataContract]
    public class WordCountMapFunction
    {
        /// <summary>
        /// Подсчитывает количество каждого слова в заданной строке
        /// </summary>
        public Dictionary<string, int> Map(string text)
        {
            var result = new Dictionary<string, int>();
            string[] words = text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                if (result.ContainsKey(word))
                    result[word]++;
                else
                    result[word] = 1;
            }
            return result;
        }
    }

    [DataContract]
    public class WordCountReduceFunction
    {
        /// <summary>
        /// Объединяет несколько словарей в один
        /// </summary>
        public Dictionary<string, int> Reduce(Dictionary<string, int>[] partialResults)
        {
            Dictionary<string, int> finalResult = new Dictionary<string, int>();

            foreach (var dict in partialResults)
            {
                foreach (var kv in dict)
                {
                    if (finalResult.ContainsKey(kv.Key))
                        finalResult[kv.Key] += kv.Value;
                    else
                        finalResult[kv.Key] = kv.Value;
                }
            }
            return finalResult;
        }
    }
}

using MapReduce;

namespace Master
{
    public class WorkerInfo
    {
        public string WorkerId { get; set; }
        public WorkerType WorkerType { get; set; }
        public string Address { get; set; }
    }
}

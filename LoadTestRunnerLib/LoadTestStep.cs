using System;
using System.Threading.Tasks;

namespace xUnitLoadRunnerLib
{
    public class LoadTestStep
    {
        public string Name { get; set; }
        public Func<Task<bool>> Action { get; set; }
    }
    
    public class StepResultMessage
    {
        public bool IsSuccess { get; }

        public StepResultMessage(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }
    }
}

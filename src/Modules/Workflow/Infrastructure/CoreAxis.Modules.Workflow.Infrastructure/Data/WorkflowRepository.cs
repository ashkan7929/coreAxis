using CoreAxis.SharedKernel;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.Workflow.Infrastructure.Data
{
    public class WorkflowRepository<T> : Repository<T> where T : EntityBase
    {
        public WorkflowRepository(WorkflowDbContext context) : base(context)
        {
        }
    }
}

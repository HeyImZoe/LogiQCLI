using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Tools.Core.Interfaces
{
    public abstract class ITool
    {
        public abstract RegisteredTool GetToolInfo();
        public abstract Task<string> Execute(string args);

        public virtual string Category
        {
            get
            {
                var namespaceParts = GetType().Namespace?.Split('.') ?? new string[0];
                if (namespaceParts.Length >= 3)
                {
                    return namespaceParts[2];
                }
                return "General";
            }
        }

        public virtual List<string> Tags { get; } = new List<string>();

        public virtual List<string> RequiredServices { get; } = new List<string>();

        public virtual int Priority { get; } = 100;

        public virtual bool RequiresWorkspace { get; } = true;
    }
}
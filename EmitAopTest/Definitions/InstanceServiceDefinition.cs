using EmitAopTest.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmitAopTest.Definitions
{
    public sealed class InstanceServiceDefinition : ServiceDefinition
    {
        public InstanceServiceDefinition(Type serviceType, object implementationInstance) : base(serviceType)
        {
            ImplementationInstance = implementationInstance ?? throw new ArgumentNullException(nameof(implementationInstance));
        }

        public object ImplementationInstance { get; }
    }
}

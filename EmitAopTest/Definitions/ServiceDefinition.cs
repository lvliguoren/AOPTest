using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmitAopTest.Definitions
{
    public abstract class ServiceDefinition
    {
        public Type ServiceType { get; }

        public ServiceDefinition(Type serviceType)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        }

        internal Type GetImplementationType()
        {
            if (this is InstanceServiceDefinition instanceServiceDefinition)
            {
                return instanceServiceDefinition.ImplementationInstance.GetType();
            }

            return null;
        }
    }
}

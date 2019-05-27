using EmitAopTest.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmitAopTest.Utils
{
    public class ProxyInstanceGenratorUtils
    {
        public static TService CreatProxyInstance<TService>(TService implementationInstance)
        {
            Type proxyType = new ProxyGeneratorUtils().CreateClassProxyType(new InstanceServiceDefinition(typeof(TService), implementationInstance));
            if (proxyType != null)
                return (TService)System.Activator.CreateInstance(proxyType, implementationInstance);
            else
                return default(TService);
        }

        public static TService CreatProxyInstance<TService>()
        {
            TService implementationInstance = (TService)System.Activator.CreateInstance(typeof(TService));

            return CreatProxyInstance<TService>(implementationInstance);
        }
    }
}

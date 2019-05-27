using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EmitAopTest
{
    public class TestProxy : SampleClassA
    {
        private SampleClassA _implementation;
        public TestProxy(SampleClassA implementation)
        {
            _implementation = implementation;
        }

        public override Test Foo(int xx, ref string str, out string cmd, Test op, Action action, string msg = "MSG")
        {
            MethodInfo implementationMethod = typeof(SampleClassA).GetMethod("Foo");
            object[] parameters = new object[]
            {
                xx,
                str,
                op,
                action
            };

            object[] customAttributes = implementationMethod.GetCustomAttributes(typeof(AspectAttribute), true);

            AspectContext aspectContext = new AspectContext();
            AspectAttribute customAttr = customAttributes[0] as AspectAttribute;
            aspectContext.Instance = _implementation;
            aspectContext.ImplementationMethod = implementationMethod;
            aspectContext.ParameterArgs = parameters;
            Test m = (Test)customAttr.Invoke(aspectContext);
            object[] parameterArgs2 = aspectContext.ParameterArgs;
            str = (string)parameterArgs2[1];
            cmd = "kkk";
            return m;
        }
    }
}

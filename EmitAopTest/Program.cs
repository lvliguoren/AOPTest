using EmitAopTest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EmitAopTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Action action = new Action(() =>
            {
                Console.WriteLine("!!!!");
            });

            Test test = new Test();
            string kn = "c";
            string cmd = "LLLL";

            Test eo = ProxyInstanceGenratorUtils.CreatProxyInstance<SampleClassA>().Foo(99, ref kn, out cmd, test, action);

            //MethodAttributes methodAttributes = typeof(SampleClassA).GetMethod("Foo").Attributes;

            //SampleClassA si = new TestProxy(new SampleClassA());
            //si.Foo(99, ref kn, test, action);
            Console.ReadLine();
        }
    }

    public class Test
    {
        public string Name = "";
    }

    //public class SampleClass : SampleInterface
    //{
    //    public virtual void Foo(int xx)
    //    {
    //        Console.WriteLine("ee");
    //    }
    //}

    //public interface SampleInterface
    //{
    //    [LogAttribute]
    //    void Foo(int xx);
    //}

    public class SampleClassA : SampleClassBase
    {
        public override Test Foo(int xx, ref string kn, out string cmd, Test io, Action action, string msg = "MSG")
        {
            Console.WriteLine("ee");
            action();
            io.Name = "Sssssssssssss";
            kn = "MMMMMMM";
            cmd = "KKKKKKK";
            return new Test() { Name = "OOOOOOOOOO" };
        }

        public override Test PCT(int xx, ref string kn, out string cmd, Test io, Action action, string msg = "MSG")
        {
            Console.WriteLine("kk");
            action();
            io.Name = "Sssssssssssss";
            kn = "MMMMMMM";
            cmd = "KKKKKKK";
            return new Test() { Name = "OOOOOOOOOO" };
        }
    }

    public class SampleClassBase
    {
        [LogAttribute]
        public virtual Test Foo(int xx, ref string kn, out string cmd, Test io, Action action, string msg = "MSG")
        {
            cmd = "KKKKKKK";
            return new Test();
        }

        public virtual Test PCT(int xx, ref string kn, out string cmd, Test io, Action action, string msg = "MSG")
        {
            cmd = "KKKKKKK";
            return new Test();
        }
    }

    public abstract class AspectAttribute : Attribute
    {
        public abstract object Invoke(AspectContext aspectContext);

        public object Next(AspectContext aspectContext)
        {
            return aspectContext.ImplementationMethod.Invoke(aspectContext.Instance, aspectContext.ParameterArgs);
        }
    }

    public class AspectContext
    {
        public object Instance { get; set; }
        public MethodInfo ImplementationMethod { get; set; }
        public object[] ParameterArgs { get; set; }
    }


    public class LogAttribute : AspectAttribute
    {
        public override object Invoke(AspectContext aspectContext)
        {
            Console.WriteLine("aaa");
            object result = Next(aspectContext);
            Console.WriteLine("bb");
            return result;
        }
    }
}

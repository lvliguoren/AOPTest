using EmitAopTest.Definitions;
using EmitAopTest.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace EmitAopTest.Utils
{
    public class ProxyGeneratorUtils
    {
        private const string ProxyAssemblyName = "EmitAopTest.DynamicProxy.Generator";
        private ModuleBuilder _moduleBuilder;
        private AssemblyBuilder _assemblyBuilder;

        public ProxyGeneratorUtils()
        {
            //_assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(ProxyAssemblyName), AssemblyBuilderAccess.RunAndSave);
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(ProxyAssemblyName), AssemblyBuilderAccess.RunAndSave);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule("ProxyModule", ProxyAssemblyName + ".dll");
        }

        /// <summary>
        /// 生成代理类
        /// </summary>
        /// <param name="serviceDefinition"></param>
        /// <returns></returns>
        public Type CreateClassProxyType(ServiceDefinition serviceDefinition)
        {
            Type serviceType = serviceDefinition.ServiceType;
            Type parentType = serviceDefinition.GetImplementationType();

            TypeBuilderDesc typeBuilderDesc = TypeBuilderUtils.DefineType(serviceType, parentType, _moduleBuilder);

            //定义构造函数
            ConstructorBuilderUtils.DefineClassProxyConstructors(serviceType, parentType, typeBuilderDesc);

            //定义方法
            MethodBuilderUtils.DefineClassProxyMethods(serviceType, parentType, typeBuilderDesc);

            Type ret = typeBuilderDesc.TypeBuilder.CreateType();

            _assemblyBuilder.Save(ProxyAssemblyName + ".dll");
            return ret;
        }

        private class ConstructorBuilderUtils
        {
            /// <summary>
            /// 定义构造函数
            /// </summary>
            internal static void DefineClassProxyConstructors(Type serviceType, Type parentType, TypeBuilderDesc typeBuilderDesc)
            {
                var constructors = parentType.GetConstructors().Where(c => !c.IsStatic && (c.IsPublic || c.IsFamily || c.IsFamilyAndAssembly || c.IsFamilyOrAssembly)).ToArray();
                if (constructors.Length == 0)
                {
                    throw new InvalidOperationException("");
                }
                foreach (var constructor in constructors)
                {
                    var parameterTypes = new Type[] { parentType }.Concat(constructor.GetParameters().Select(p => p.ParameterType)).ToArray();

                    /* public TempClassProxy(ParentType implementation)
                     * {
                     *      this._implementation = implementation;
                     * }
                     */
                    ConstructorBuilder ctstBuilder = typeBuilderDesc.TypeBuilder.DefineConstructor(constructor.Attributes, constructor.CallingConvention, parameterTypes);

                    ILGenerator ctstGenerator = ctstBuilder.GetILGenerator();
                    ctstGenerator.EmitLoadArg(0);
                    ctstGenerator.Emit(OpCodes.Callvirt, constructor);
                    ctstGenerator.Emit(OpCodes.Nop);
                    ctstGenerator.Emit(OpCodes.Nop);
                    ctstGenerator.EmitLoadArg(0);
                    ctstGenerator.EmitLoadArg(1);
                    ctstGenerator.Emit(OpCodes.Stfld, typeBuilderDesc.FieldTable[FieldBuilderUtils.Implementation]);//给字段赋值   

                    ctstGenerator.Emit(OpCodes.Ret);
                }
            }
        }

        private class MethodBuilderUtils
        {
            /// <summary>
            /// 定义代理方法
            /// </summary>
            /// <param name="serviceType"></param>
            /// <param name="parentType"></param>
            /// <param name="typeBuilderDesc"></param>
            internal static void DefineClassProxyMethods(Type serviceType, Type parentType, TypeBuilderDesc typeBuilderDesc)
            {
                foreach (var method in serviceType.GetMethods().Where(x => !x.IsPropertyBinding()))
                {
                    #region 定义方法前缀
                    MethodAttributes attributes = MethodAttributes.HideBySig | MethodAttributes.Virtual;

                    if (method.Attributes.HasFlag(MethodAttributes.Public))
                    {
                        attributes = attributes | MethodAttributes.Public;
                    }

                    if (method.Attributes.HasFlag(MethodAttributes.Family))
                    {
                        attributes = attributes | MethodAttributes.Family;
                    }

                    if (method.Attributes.HasFlag(MethodAttributes.FamORAssem))
                    {
                        attributes = attributes | MethodAttributes.FamORAssem;
                    }
                    #endregion

                    var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
                    var methodBuilder = typeBuilderDesc.TypeBuilder.DefineMethod(method.Name, attributes, method.CallingConvention, method.ReturnType, parameterTypes);

                    #region 定义方法出入参

                    var parameters = method.GetParameters();
                    if (parameters.Length > 0)
                    {
                        var paramOffset = 1;   // 1
                        for (var i = 0; i < parameters.Length; i++)
                        {
                            var parameter = parameters[i];
                            var parameterBuilder = methodBuilder.DefineParameter(i + paramOffset, parameter.Attributes, parameter.Name);
                            if (parameter.IsOptional)
                            {
                                if (!(parameter.ParameterType.IsValueType && parameter.DefaultValue == null))
                                    parameterBuilder.SetConstant(parameter.DefaultValue);
                            }
                        }
                    }

                    var returnParamter = method.ReturnParameter;
                    var returnParameterBuilder = methodBuilder.DefineParameter(0, returnParamter.Attributes, returnParamter.Name);

                    #endregion

                    #region 定义方法体

                    if (method.GetCustomAttributes(typeof(AspectAttribute), true).Length <= 0)
                        DefineMethodBody(parentType, method, methodBuilder, typeBuilderDesc);
                    else
                        DefineProxyMethodBody(parentType, method, methodBuilder, typeBuilderDesc);

                    #endregion
                }
            }


            /*
             * public override Test Foo(int xx, ref string kn, out string cmd, Test io, Action action, string msg = "MSG")
                {
                    MethodInfo method = typeof(SampleClassA).GetMethod("Foo");
                    object[] parameterArgs = new object[]
                    {
                        xx,
                        kn,
                        io,
                        action
                    };
                    object[] customAttributes = method.GetCustomAttributes(typeof(AspectAttribute), true);
                    AspectContext aspectContext = new AspectContext();
                    AspectAttribute aspectAttribute = customAttributes[0] as AspectAttribute;
                    aspectContext.Instance = this._implementation;
                    aspectContext.ImplementationMethod = method;
                    aspectContext.ParameterArgs = parameterArgs;
                    Test result = (Test)aspectAttribute.Invoke(aspectContext);
                    object[] parameterArgs2 = aspectContext.ParameterArgs;
                    kn = (string)parameterArgs2[1];
                    cmd = (string)parameterArgs2[2];
                    return result;
                 }
             */
            /// <summary>
            /// 定义代理方法体
            /// </summary>
            private static void DefineProxyMethodBody(Type parentType, MethodInfo method, MethodBuilder methodBuilder, TypeBuilderDesc typeBuilderDesc)
            {
                var methodILGen = methodBuilder.GetILGenerator();
                var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();

                #region 定义局部变量
                var implementationMethod = methodILGen.DeclareLocal(typeof(MethodInfo));
                var implementationMethodParameters = methodILGen.DeclareLocal(typeof(object[]));
                var customAttributes = methodILGen.DeclareLocal(typeof(object[]));
                var apectContext = methodILGen.DeclareLocal(typeof(AspectContext));
                var customAttr = methodILGen.DeclareLocal(typeof(AspectAttribute));
                #endregion

                methodILGen.Emit(OpCodes.Nop);
                methodILGen.Emit(OpCodes.Ldtoken, parentType);
                //静态方法用Call
                methodILGen.Emit(OpCodes.Call, MethodUtils.GetMethod<Func<RuntimeTypeHandle, Type>>(p => Type.GetTypeFromHandle(p)));
                methodILGen.Emit(OpCodes.Ldstr, method.Name);
                //实例方法用Callvirt
                methodILGen.Emit(OpCodes.Callvirt, MethodUtils.GetMethod<Func<Type, string, MethodInfo>>((h1, h2) => h1.GetMethod(h2)));
                methodILGen.Emit(OpCodes.Stloc_0);

                methodILGen.EmitInt(parameterTypes.Length);
                methodILGen.Emit(OpCodes.Newarr, typeof(object));
                for (var i = 0; i < parameterTypes.Length; i++)
                {
                    methodILGen.Emit(OpCodes.Dup);
                    methodILGen.EmitInt(i);
                    methodILGen.EmitLoadArg(i + 1);
                    if (parameterTypes[i].IsByRef)
                    {
                        methodILGen.EmitLdRef(parameterTypes[i]);
                        methodILGen.EmitConvertToObject(parameterTypes[i].GetElementType());
                    }
                    else
                    {
                        methodILGen.EmitConvertToObject(parameterTypes[i]);
                    }
                    methodILGen.Emit(OpCodes.Stelem_Ref);
                }

                methodILGen.Emit(OpCodes.Stloc_1);
                methodILGen.Emit(OpCodes.Ldloc_0);
                methodILGen.Emit(OpCodes.Ldtoken, typeof(AspectAttribute));
                methodILGen.Emit(OpCodes.Call, MethodUtils.GetMethod<Func<RuntimeTypeHandle, Type>>(p => Type.GetTypeFromHandle(p)));
                methodILGen.EmitInt(1);
                methodILGen.Emit(OpCodes.Callvirt, MethodUtils.GetMethod<Func<MethodInfo, Type, bool, object[]>>((h1, h2, h3) => h1.GetCustomAttributes(h2, h3)));
                methodILGen.Emit(OpCodes.Stloc_2);

                methodILGen.Emit(OpCodes.Newobj, typeof(AspectContext).GetConstructors().First());
                methodILGen.Emit(OpCodes.Stloc_S, 3);

                methodILGen.Emit(OpCodes.Ldloc_2);
                methodILGen.EmitInt(0);
                methodILGen.Emit(OpCodes.Ldelem_Ref);
                methodILGen.Emit(OpCodes.Isinst, typeof(AspectAttribute));
                methodILGen.Emit(OpCodes.Stloc_S, 4);

                methodILGen.Emit(OpCodes.Ldloc_S, 3);
                methodILGen.EmitLoadArg(0);
                methodILGen.Emit(OpCodes.Ldfld, typeBuilderDesc.FieldTable[FieldBuilderUtils.Implementation]);
                methodILGen.Emit(OpCodes.Callvirt, MethodUtils.GetMethod<AspectContext>("set_Instance"));

                methodILGen.Emit(OpCodes.Ldloc_S, 3);
                methodILGen.Emit(OpCodes.Ldloc_0);
                methodILGen.Emit(OpCodes.Callvirt, MethodUtils.GetMethod<AspectContext>("set_ImplementationMethod"));

                methodILGen.Emit(OpCodes.Ldloc_S, 3);
                methodILGen.Emit(OpCodes.Ldloc_1);
                methodILGen.Emit(OpCodes.Callvirt, MethodUtils.GetMethod<AspectContext>("set_ParameterArgs"));

                methodILGen.Emit(OpCodes.Ldloc_S, 4);
                methodILGen.Emit(OpCodes.Ldloc_S, 3);
                methodILGen.Emit(OpCodes.Callvirt, MethodUtils.GetMethod<AspectAttribute>("Invoke"));

                var returnValue = default(LocalBuilder);
                if (method.ReturnType != typeof(void))
                {
                    methodILGen.EmitConvertFromObject(method.ReturnType);
                    returnValue = methodILGen.DeclareLocal(method.ReturnType);
                    methodILGen.Emit(OpCodes.Stloc, returnValue);
                }
                else
                {
                    methodILGen.Emit(OpCodes.Pop);
                }

                if (parameterTypes.Any(x => x.IsByRef))
                {
                    var parameters = methodILGen.DeclareLocal(typeof(object[]));
                    methodILGen.Emit(OpCodes.Ldloc_3);
                    methodILGen.Emit(OpCodes.Callvirt, MethodUtils.GetMethod<AspectContext>("get_ParameterArgs"));
                    methodILGen.Emit(OpCodes.Stloc, parameters);
                    for (var i = 0; i < parameterTypes.Length; i++)
                    {
                        if (parameterTypes[i].IsByRef)
                        {
                            methodILGen.EmitLoadArg(i + 1);
                            methodILGen.Emit(OpCodes.Ldloc, parameters);
                            methodILGen.EmitInt(i);
                            methodILGen.Emit(OpCodes.Ldelem_Ref);
                            methodILGen.EmitConvertFromObject(parameterTypes[i].GetElementType());
                            methodILGen.EmitStRef(parameterTypes[i]);
                        }
                    }
                }

                if (returnValue != null)
                {
                    methodILGen.Emit(OpCodes.Ldloc, returnValue);
                }
                methodILGen.Emit(OpCodes.Ret);
            }


            /*
             * public override void Foo(int xx)
             * {
             *    base.Foo(xx);
             * }
             */
            /// <summary>
            /// 定义方法体
            /// </summary>
            /// <param name="serviceType"></param>
            /// <param name="parentType"></param>
            /// <param name="method"></param>
            /// <param name="methodBuilder"></param>
            private static void DefineMethodBody(Type parentType, MethodInfo method, MethodBuilder methodBuilder, TypeBuilderDesc typeBuilderDesc)
            {
                var methodILGen = methodBuilder.GetILGenerator();
                var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();

                methodILGen.Emit(OpCodes.Nop);
                methodILGen.EmitLoadArg(0);
                methodILGen.Emit(OpCodes.Ldfld, typeBuilderDesc.FieldTable[FieldBuilderUtils.Implementation]);

                for (var i = 0; i < parameterTypes.Length; i++)
                    methodILGen.EmitLoadArg(i + 1);

                methodILGen.Emit(OpCodes.Callvirt, parentType.GetMethod(method.Name));
                methodILGen.Emit(OpCodes.Nop);
                methodILGen.Emit(OpCodes.Ret);
            }
        }

        private class FieldBuilderUtils
        {
            public const string Implementation = "_implementation";

            public static FieldTable DefineFields(Type targetType, TypeBuilder typeBuilder)
            {
                var fieldTable = new FieldTable();
                //private ServiceType _implementation;
                fieldTable[Implementation] = typeBuilder.DefineField(Implementation, targetType, FieldAttributes.Private);
                return fieldTable;
            }
        }

        private class FieldTable
        {
            private readonly Dictionary<string, FieldBuilder> _table = new Dictionary<string, FieldBuilder>();

            public FieldBuilder this[string fieldName]
            {
                get
                {
                    return _table[fieldName];
                }
                set
                {
                    _table[value.Name] = value;
                }
            }
        }

        private class TypeBuilderUtils
        {
            public static TypeBuilderDesc DefineType(Type serviceType, Type parentType, ModuleBuilder moduleBuilder)
            {
                //创建类型(其实就是一个类)   
                StringBuilder sbClassName = new StringBuilder("EmitAopTest.");
                sbClassName.Append(parentType.Name);
                sbClassName.Append("_Proxy");

                TypeBuilder typeBuidler = moduleBuilder.DefineType(sbClassName.ToString(), TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed, parentType);

                //定义私有变量
                var fieldTable = FieldBuilderUtils.DefineFields(serviceType, typeBuidler);

                return new TypeBuilderDesc(typeBuidler, fieldTable);
            }
        }

        private class TypeBuilderDesc
        {
            public TypeBuilder TypeBuilder { get; }

            public FieldTable FieldTable { get; }

            public TypeBuilderDesc(TypeBuilder typeBuilder, FieldTable fieldTable)
            {
                TypeBuilder = typeBuilder;
                FieldTable = fieldTable;
            }
        }
    }
}

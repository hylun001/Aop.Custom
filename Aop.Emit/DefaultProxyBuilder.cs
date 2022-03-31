using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Aop.Emit
{
    public static class DefaultProxyBuilder
    {
        private static readonly Type VoidType = Type.GetType("System.Void");

        /// <summary>
        /// 生成程序集
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="interceptorType"></param>
        /// <returns></returns>
        public static T CreateProxy<T>(Type interceptorType)
        {
            Type classType = typeof(T);

            string name = classType.Namespace + ".Aop";
            string fileName = name + ".dll";


            var assemblyName = new AssemblyName(name);
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
                                                                                AssemblyBuilderAccess.RunAndSave);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(name, fileName);
            var aopType = BulidType(classType, interceptorType, moduleBuilder);

            assemblyBuilder.Save(fileName);
            return (T)Activator.CreateInstance(aopType);
        }

        /// <summary>
        /// 生成代理类
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="interceptorType"></param>
        /// <param name="moduleBuilder"></param>
        /// <returns></returns>
        private static Type BulidType(Type classType, Type interceptorType, ModuleBuilder moduleBuilder)
        {
            string className = classType.Name + "_Proxy";

            //定义类型
            var typeBuilder = moduleBuilder.DefineType(className,
                                                       TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class,
                                                       classType);
            //定义字段 _inspector
            var inspectorFieldBuilder = typeBuilder.DefineField("_inspector", typeof(IInterceptor),
                                                                FieldAttributes.Private | FieldAttributes.InitOnly);
            //构造函数
            BuildCtor(classType, interceptorType, inspectorFieldBuilder, typeBuilder);

            //构造方法
            BuildMethod(classType, inspectorFieldBuilder, typeBuilder);
            Type aopType = typeBuilder.CreateType();
            return aopType;
        }

        /// <summary>
        /// 生成方法
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="inspectorFieldBuilder"></param>
        /// <param name="typeBuilder"></param>
        private static void BuildMethod(Type classType, FieldBuilder inspectorFieldBuilder, TypeBuilder typeBuilder)
        {
            var methodInfos = classType.GetMethods();
            foreach (var methodInfo in methodInfos)
            {
                if (!methodInfo.IsVirtual && !methodInfo.IsAbstract) continue;
                if (methodInfo.Name == "ToString") continue;
                if (methodInfo.Name == "GetHashCode") continue;
                if (methodInfo.Name == "Equals") continue;

                var parameterInfos = methodInfo.GetParameters();
                var parameterTypes = parameterInfos.Select(p => p.ParameterType).ToArray();
                var parameterLength = parameterTypes.Length;
                var hasResult = methodInfo.ReturnType != VoidType;

                var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                                                             MethodAttributes.Public | MethodAttributes.Final |
                                                             MethodAttributes.Virtual
                                                             , methodInfo.ReturnType
                                                             , parameterTypes);

                var il = methodBuilder.GetILGenerator();

                //局部变量
                il.DeclareLocal(typeof(object)); //correlationState
                il.DeclareLocal(typeof(object)); //result
                il.DeclareLocal(typeof(object[])); //parameters

                //BeforeCall(string operationName, object[] inputs);
                il.Emit(OpCodes.Ldarg_0);

                il.Emit(OpCodes.Ldfld, inspectorFieldBuilder);//获取字段_inspector
                il.Emit(OpCodes.Ldstr, methodInfo.Name);//参数operationName

                if (parameterLength == 0)//判断方法参数长度
                {
                    il.Emit(OpCodes.Ldnull);//null -> 参数 inputs
                }
                else
                {
                    //创建new object[parameterLength];
                    il.Emit(OpCodes.Ldc_I4, parameterLength);
                    il.Emit(OpCodes.Newarr, typeof(Object));
                    il.Emit(OpCodes.Stloc_2);//压入局部变量2 parameters

                    for (int i = 0, j = 1; i < parameterLength; i++, j++)
                    {
                        //object[i] = arg[j]
                        il.Emit(OpCodes.Ldloc_2);
                        il.Emit(OpCodes.Ldc_I4, 0);
                        il.Emit(OpCodes.Ldarg, j);
                        if (parameterTypes[i].IsValueType) il.Emit(OpCodes.Box, parameterTypes[i]);//对值类型装箱
                        il.Emit(OpCodes.Stelem_Ref);
                    }
                    il.Emit(OpCodes.Ldloc_2);//取出局部变量2 parameters-> 参数 inputs
                }

                il.Emit(OpCodes.Callvirt, typeof(IInterceptor).GetMethod("BeforeCall"));//调用BeforeCall
                il.Emit(OpCodes.Stloc_0);//建返回压入局部变量0 correlationState

                //Call methodInfo
                il.Emit(OpCodes.Ldarg_0);
                //获取参数表
                for (int i = 1, length = parameterLength + 1; i < length; i++)
                {
                    il.Emit(OpCodes.Ldarg_S, i);
                }
                il.Emit(OpCodes.Call, methodInfo);
                //将返回值压入 局部变量1result void就压入null
                if (!hasResult) il.Emit(OpCodes.Ldnull);
                else if (methodInfo.ReturnType.IsValueType) il.Emit(OpCodes.Box, methodInfo.ReturnType);//对值类型装箱
                il.Emit(OpCodes.Stloc_1);

                //AfterCall(string operationName, object returnValue, object correlationState);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, inspectorFieldBuilder);//获取字段_inspector
                il.Emit(OpCodes.Ldstr, methodInfo.Name);//参数 operationName
                il.Emit(OpCodes.Ldloc_1);//局部变量1 result
                il.Emit(OpCodes.Ldloc_0);// 局部变量0 correlationState
                il.Emit(OpCodes.Callvirt, typeof(IInterceptor).GetMethod("AfterCall"));

                //result
                if (!hasResult)
                {
                    il.Emit(OpCodes.Ret);
                    return;
                }
                il.Emit(OpCodes.Ldloc_1);//非void取出局部变量1 result
                if (methodInfo.ReturnType.IsValueType) il.Emit(OpCodes.Unbox_Any, methodInfo.ReturnType);//对值类型拆箱
                il.Emit(OpCodes.Ret);
            }
        }

        /// <summary>
        /// 生成构造函数
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="interceptorType"></param>
        /// <param name="inspectorFieldBuilder"></param>
        /// <param name="typeBuilder"></param>
        private static void BuildCtor(Type classType, Type interceptorType, FieldBuilder inspectorFieldBuilder, TypeBuilder typeBuilder)
        {
            {
                var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis,
                                                                Type.EmptyTypes);
                var il = ctorBuilder.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, classType.GetConstructor(Type.EmptyTypes));//调用base的默认ctor
                il.Emit(OpCodes.Ldarg_0);
                //将typeof(classType)压入计算堆
                il.Emit(OpCodes.Ldtoken, interceptorType);
                il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new[] { typeof(RuntimeTypeHandle) }));
                //调用DefaultInterceptorFactory.Create(type)
                il.Emit(OpCodes.Call, typeof(DefaultInterceptorFactory).GetMethod("Create", new[] { typeof(Type) }));
                //将结果保存到字段_inspector
                il.Emit(OpCodes.Stfld, inspectorFieldBuilder);
                il.Emit(OpCodes.Ret);
            }
        }
    }





}

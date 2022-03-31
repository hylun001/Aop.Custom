using System;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace Aop.TransparentProxy
{
    public class DynamicProxy : RealProxy
    {
        readonly Type _interceptorType;

        public DynamicProxy(Type t) : base(t)
        {
            _interceptorType = t;
        }

        public override IMessage Invoke(IMessage msg)
        {
            Console.WriteLine("Begin Call");
            var methodCall = (IMethodCallMessage)msg;
            object objInterceptorInstance = Activator.CreateInstance(_interceptorType);

            object result = null;
            try
            {
                result = methodCall.MethodBase.Invoke(objInterceptorInstance, methodCall.Args);
                Console.WriteLine("After Call");

                return new ReturnMessage(result, null, 0, methodCall.LogicalCallContext, methodCall);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("invoke exception");
                throw ex;
            }         
        }

    }

}

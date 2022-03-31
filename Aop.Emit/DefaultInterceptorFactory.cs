using System;

namespace Aop.Emit
{

    public class DefaultInterceptorFactory
    {
        public static IInterceptor Create(Type type)
        {
            return (IInterceptor)Activator.CreateInstance(type);
        }
    }

}

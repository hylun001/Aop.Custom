using Aop.Emit;
using System;

namespace Test
{

    public class MessageInterceptor : IInterceptor
    {
        public void AfterCall(string operationName, object returnValue, object correlationState)
        {
            Console.WriteLine("After call");
        }

        public object BeforeCall(string operationName, object[] inputs)
        {
            Console.WriteLine("Before call");
            return null;
        }
    }
}

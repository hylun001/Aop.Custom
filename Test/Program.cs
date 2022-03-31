using System;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            ////Emit
            //var message = Aop.Emit.DefaultProxyBuilder.CreateProxy<Message>(typeof(MessageInterceptor));
            //message.SaySomething();
            //Console.WriteLine();

            //TransparentProxy
            var proxy = new Aop.TransparentProxy.DynamicProxy(typeof(Plane));
            Plane plane = (Plane)proxy.GetTransparentProxy();
            plane.Fly();


            Console.ReadLine();
        }
    }


}

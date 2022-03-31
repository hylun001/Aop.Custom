using System;

namespace Test
{
    public class Plane : MarshalByRefObject
    {
        public void Fly()
        {
            Console.WriteLine("I can fly");
        }
    }
}

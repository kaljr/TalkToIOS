using System;

namespace TalkToIOS
{
    class Program
    {
        static void Main(string[] args)
        {
            Connection.Initialize();
            Connection.StartListening();
            Console.ReadLine();
        }
    }
}

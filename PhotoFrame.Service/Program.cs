using System;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static Task<int> Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            return Task.FromResult(0);
        }
    }
}
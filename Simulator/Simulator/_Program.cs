using System;

namespace Simulator
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Simulator sim = new Simulator())
            {
                sim.Run();
            }
        }
    }
#endif
}


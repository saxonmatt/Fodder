#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace Fodder.Mono.Linux
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        private static Fodder game;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            game = new Fodder();
            game.Run();
        }
    }
}

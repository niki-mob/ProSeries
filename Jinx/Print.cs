using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;

namespace Jinx
{
    static class Print
    {
        public static void PrintMsg(string msg)
        {
            var callstack = new StackFrame(1, true);
            Console.WriteLine("[Debug] Line: " + callstack.GetFileLineNumber() +  " / " + msg);
        }
    }
}

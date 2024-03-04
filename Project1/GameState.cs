using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1
{
    //The Gamestate enumerator keeps track of what state the game is in. In the future, menus will
    //be added and the running/paused distinction will be a necessary in a public variable.
    //Enumerators are good for this for being programmer-friendly integer values usable in functions
    //like switch-case scenarios.
    public enum Gamestate
    {
        Running,
        Paused,
        Exit,
        Init,
        Input,
        Menu
    };
}

﻿using DeNES_ClassLibrary;

namespace DeNES_Console
{
    public class Program
    {
        static void Main(string[] args)
        {
            DeNES emulator = new DeNES();
            emulator.Load("D:/nes roms/Mario Bros. (World).nes"); //EXAMPLE
            //emulator.Load("D:/nes roms/Ice Climber (USA, Europe).nes");
            //emulator.Load("D:/nes roms/Tetris (USA).nes");
            while (true)
            {
                emulator.Tick();
            }
            
        }
    }
}
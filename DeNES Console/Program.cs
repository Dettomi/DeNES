﻿using DeNES_ClassLibrary;

namespace DeNES_Console
{
    public class Program
    {
        static void Main(string[] args)
        {
            DeNES emulator = new DeNES();
            emulator.Run("D:/nes roms/Mario Bros. (World).nes"); //EXAMPLE
        }
    }
}
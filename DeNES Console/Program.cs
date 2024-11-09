using DeNES_ClassLibrary;

namespace DeNES_Console
{
    public class Program
    {
        static void Main(string[] args)
        {
            DeNES emulator = new DeNES();
            emulator.Run("Mario Bros. (World).nes");
        }
    }
}
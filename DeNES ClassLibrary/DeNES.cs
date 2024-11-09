using DeNES_ClassLibrary.Components;

namespace DeNES_ClassLibrary
{
    public class DeNES
    {
        ROM rom;
        CPU cpu;
        int cycle;
        public DeNES()
        {
            rom = new ROM();
            cpu = new CPU();
            cycle = 0;
        }
        public void Run(string romPath)
        {
            rom.Load(romPath);
            while(true)
            {
                Console.WriteLine("---------\nCycle: " + cycle);
                cpu.instruction(rom.Data);
                //ppu();
                //apu();
                //input();
                tick();
            }
        }
        void tick()
        {
            cycle++;
        }
    }
}

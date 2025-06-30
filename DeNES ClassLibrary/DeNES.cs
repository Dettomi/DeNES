using DeNES_ClassLibrary.Components;

namespace DeNES_ClassLibrary
{
    public class DeNES
    {
        ROM rom;
        CPU cpu;
        PPU ppu;
        int cycle;
        int cpu_cycle;
        public int Cycle { get => cycle; }

        public void Load(string romPath)
        {
            rom = new ROM();
            cpu = new CPU();
            rom.Load(romPath);
            cycle = 0;
        }
        public void Tick()
        {
            Console.WriteLine("---------\nCycle: " + cycle);
            cpu_cycle = cpu.instruction(rom.Data);
            for(int i = 0; i < cpu_cycle; i++)
            {
                ppu.Tick();
            }
            //apu();
            //input();
            cycle += cpu_cycle;
        }
    }
}

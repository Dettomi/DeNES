using DeNES_ClassLibrary.Components;

namespace DeNES_ClassLibrary
{
    public class DeNES
    {
        ROM rom;
        CPU cpu;
        int cycle;
        int cpu_cycle;
        public DeNES()
        {
            rom = new ROM();
            cpu = new CPU();
            cycle = 0;
            cpu_cycle = 0;
        }
        public void Run(string romPath)
        {
            rom.Load(romPath);
            while(true)
            {
                Console.WriteLine("---------\nCycle: " + cycle);
                cpu_cycle = cpu.instruction(rom.Data);
                for(int i = 0; i < cpu_cycle; i++)
                {
                    //ppu()
                }
                //apu();
                //input();
                cycle += cpu_cycle;
            }
        }
    }
}

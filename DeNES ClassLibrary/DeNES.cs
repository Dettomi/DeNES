using DeNES_ClassLibrary.Components;

namespace DeNES_ClassLibrary
{
    public class DeNES
    {
        ROM rom;
        CPU cpu;
        PPU ppu;
        Memory memory;
        int cycle;
        int cpu_cycle;
        public int Cycle { get => cycle; }
        public byte[] GetFramebuffer { get => ppu.Framebuffer; }
        public void Load(string romPath)
        {
            rom = new ROM();
            rom.Load(romPath);

            memory = new Memory();
            LoadMemory();

            cpu = new CPU(memory);
            ppu = new PPU(rom.GetChrRom());

            cpu.Ppu = ppu;
            memory.Ppu = ppu;

            cycle = 0;
        }
        public void Tick()
        {
            Console.WriteLine("---------\nCycle: " + cycle);
            cpu_cycle = cpu.instruction();
            for(int i = 0; i < cpu_cycle * 3; i++)
            {
                ppu.Tick();
            }
            //apu();
            //input();
            cycle += cpu_cycle;
        }
        public void LoadMemory()
        {
            byte[] prg_rom = rom.GetPrgRom();
            int prg_size = prg_rom.Length;

            //First 16kb to $8000
            for (int i = 0; i < 16 * 1024; i++)
            {
                memory.Write((ushort)(0x8000 + i), prg_rom[i]);
            }
            //If 16kb mirror to $c000
            if (prg_size == 16 * 1024)
            {
                for (int i = 0; i < 16 * 1024; i++)
                {
                    memory.Write((ushort)(0xc000 + i), prg_rom[i]);
                }
            }
            //If 32kb copy the second half
            else
            {
                for (int i = 0; i < 16 * 1024; i++)
                {
                    memory.Write((ushort)(0xc000 + i), prg_rom[i+(16*1024)]);
                }
            }
        }
    }
}

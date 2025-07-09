using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace DeNES_ClassLibrary.Components
{
    public class CPU
    {
        PPU ppu;
        byte[] prg_rom;
        int programCounter;
        bool C; //Carry
        bool Z; //Zero
        bool I; //Interrupt Disable
        bool D; //Decimal
        bool V; //Overflow
        bool N; //Negative

        byte A;
        byte X;
        byte Y;

        public CPU(byte[] prgRom)
        {
            this.prg_rom = prgRom;
            programCounter = 0;
            C = false;
            Z = false;
            I = false;
            D = false;
            V = false;
            N = false;
        }
        public PPU Ppu { set => ppu = value; }

        public int instruction()
        {
            if (programCounter < prg_rom.Length)
            {
                int cycle = 0;
                byte opcode = prg_rom[programCounter];
                Console.WriteLine("Opcode: " + opcode+"(0x"+opcode.ToString("X2")+")");
                programCounter++;

                switch (opcode)
                {
                    //ACCESS:
                    case 0xa9: //LDA Immediate
                        A = prg_rom[programCounter++];
                        Z = (A == 0);
                        N = (A & 0x80) != 0; //7th bit
                        Console.WriteLine("Executing LDA Immediate: Load A");
                        cycle = 2;
                        break;
                    case 0x8d: //STA Absolute (STORE A)
                        byte low = prg_rom[programCounter++];
                        byte high = prg_rom[programCounter++];
                        ushort address = (ushort)((high << 8) | low); //16 bit
                        WriteToMemory(address, A);
                        Console.WriteLine("Executing STA Absolute: Store A");
                        cycle = 4;
                        break;
                    //FLAGS:
                    case 0x18: //CLC (Clear Carry)
                        C = false;
                        Console.WriteLine("Executing CLC: Clear Carry");
                        cycle = 2;
                        break;
                    case 0x38: //SEC (Set Carry)
                        C = true;
                        Console.WriteLine("Executing SEC: Set Carry");
                        cycle = 2;
                        break;

                    case 0x58: //CLI (Clear Interrupt Disable)
                        I = false;
                        Console.WriteLine("Executing CLI: Clear Interrupt Disable");
                        cycle = 2;
                        break;
                    case 0x78: //SEI (Set Interrupt Disable)
                        I = true;
                        Console.WriteLine("Executing SEI: Set Interrupt Disable");
                        cycle = 2;
                        break;

                    case 0xD8: //CLD (Clear Decimal)
                        D = false;
                        Console.WriteLine("Executing CLD: Clear Decimal");
                        cycle = 2;
                        break;
                    case 0xF8: //SED (Set Decimal)
                        D = true;
                        Console.WriteLine("Executing SED: Set Decimal");
                        cycle = 2;
                        break;

                    case 0xB8: //CLV (Clear Overflow)
                        V = false;
                        Console.WriteLine("Executing CLV: Clear Overflow");
                        cycle = 2;
                        break;
                    //Other:
                    case 0xEA: //NOP (No Operation)
                        Console.WriteLine("Executing NOP: No Operation");
                        cycle = 2;
                        break;
                    default:
                        Console.WriteLine("Unknown opcode: " + opcode + "(0x" + opcode.ToString("X2") + ")");
                        break;
                }
                return cycle;
            }
            else
            {
                Console.WriteLine("End of ROM Data!");
                return 0;
            }
        }
        void WriteToMemory(ushort address, byte value)
        {
            switch (address)
            {
                case 0x2000:
                    ppu.SETPPUCTRL(value);
                    break;
                case 0x2006:
                    ppu.SETPPUADDR(value);
                    break;
                case 0x2007:
                    ppu.WritePPUDATA(value);
                    break;
                default:
                    Console.WriteLine($"Write to {address} failed");
                    break;
            }
        }
    }
}

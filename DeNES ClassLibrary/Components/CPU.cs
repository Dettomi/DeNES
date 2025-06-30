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
        byte[] prg_rom;
        int programCounter;
        bool C; //Carry
        bool Z; //Zero
        bool I; //Interrupt Disable
        bool D; //Decimal
        bool V; //Overflow
        bool N; //Negative

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
    }
}

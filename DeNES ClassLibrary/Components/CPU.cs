﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace DeNES_ClassLibrary.Components
{
    public class CPU
    {
        int programCounter;
        public CPU()
        {
            programCounter = 16;
        }
        public void instruction(byte[] data)
        {
            if (programCounter < data.Length)
            {
                byte opcode = data[programCounter];
                Console.WriteLine("Opcode: " + opcode+"(0x"+opcode.ToString("X2")+")");
                programCounter++;

                switch (opcode)
                {
                    case 0x78: // SEI (Set Interrupt Disable)
                        Console.WriteLine("Executing SEI: Set Interrupt Disable");
                        break;
                    case 0xD8: // CLD (Clear Decimal Mode)
                        Console.WriteLine("Executing CLD: Clear Decimal Mode");
                        break;
                    default:
                        Console.WriteLine("Unknown opcode: " + opcode + "(0x" + opcode.ToString("X2") + ")");
                        break;
                }
            }
            else
            {
                Console.WriteLine("End of ROM Data!");
            }
        }
    }
}

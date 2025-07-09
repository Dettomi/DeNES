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
        Memory memory;
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
        byte SP; //Stack pointer
        public CPU(Memory memo)
        {
            memory = memo;
            programCounter = (ushort)(memory.Read(0xFFFC) | (memory.Read(0xFFFD) << 8)); //NES starts at this address
            SP = 0xFD; //NES default value
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
            int cycle = 0;
            byte opcode = memory.Read((ushort)(programCounter++));
            Console.WriteLine("Opcode: " + opcode+"(0x"+opcode.ToString("X2")+")");

            switch (opcode)
            {
                //ACCESS:
                case 0xAD: //LDA Absolute
                    byte lda_low = memory.Read((ushort)(programCounter++));
                    byte lda_high = memory.Read((ushort)(programCounter++));
                    ushort lda_address = (ushort)((lda_high << 8) | lda_low); //16 bit
                    A = memory.Read(lda_address);
                    Z = (A == 0);
                    N = (A & 0x80) != 0; //7th bit
                    Console.WriteLine($"Executing LDA Absolute: Load A {lda_address:X4} Value A: {A}");
                    cycle = 4;
                    break;
                case 0xa9: //LDA Immediate
                    A = memory.Read((ushort)(programCounter++));
                    Z = (A == 0);
                    N = (A & 0x80) != 0; //7th bit
                    Console.WriteLine("Executing LDA Immediate: Load A");
                    cycle = 2;
                    break;
                case 0x8d: //STA Absolute (STORE A)
                    byte low = memory.Read((ushort)(programCounter++));
                    byte high = memory.Read((ushort)(programCounter++));
                    ushort address = (ushort)((high << 8) | low); //16 bit
                    WriteToMemory(address, A);
                    Console.WriteLine("Executing STA Absolute: Store A");
                    cycle = 4;
                    break;
                case 0x81: //STA Indirect X (STORE A) ZP + X => 16 bit address
                    byte stax_zeroPage = memory.Read((ushort)programCounter++);
                    byte stax_low = memory.Read((byte)((stax_zeroPage + X) & 0xFF));
                    byte stax_high = memory.Read((byte)((stax_zeroPage + X + 1) & 0xFF));
                    ushort stax_address = (ushort)((stax_high << 8) | stax_low);
                    WriteToMemory(stax_address, A);
                    Console.WriteLine("Executing STA Indirect X: Store A");
                    cycle = 6;
                    break;
                case 0x91: //STA Indirect Y (STORE A) ZP => 16 bit address + Y
                    byte stay_zeroPage = memory.Read((ushort)programCounter++);
                    byte stay_low = memory.Read(stay_zeroPage);
                    byte stay_high = memory.Read((byte)((stay_zeroPage + 1) & 0xFF));
                    ushort stay_address = (ushort)((stay_high << 8) | stay_low);
                    WriteToMemory((ushort)(stay_address+Y), A);
                    Console.WriteLine("Executing STA Indirect Y: Store A");
                    cycle = 6;
                    break;
                case 0xA2: //LDX - Load X
                    X = memory.Read((ushort)(programCounter++));
                    Z = (X == 0);
                    N = (X & 0x80) != 0; //7th bit
                    Console.WriteLine("Executing LDX Immediate: Load X");
                    cycle = 2;
                    break;
                case 0xA0: //LDY - Load Y
                    Y = memory.Read((ushort)(programCounter++));
                    Z = (Y == 0);
                    N = (Y & 0x80) != 0; //7th bit
                    Console.WriteLine("Executing LDY Immediate: Load Y");
                    cycle = 2;
                    break;
                case 0x8E: //STX Absolute (STORE X)
                    byte x_low = memory.Read((ushort)(programCounter++));
                    byte x_high = memory.Read((ushort)(programCounter++));
                    ushort x_address = (ushort)((x_high << 8) | x_low); //16 bit
                    WriteToMemory(x_address, X);
                    Console.WriteLine("Executing STX Absolute: Store X");
                    cycle = 4;
                    break;
                case 0x8C: //STY Absolute (STORE Y)
                    byte y_low = memory.Read((ushort)(programCounter++));
                    byte y_high = memory.Read((ushort)(programCounter++));
                    ushort y_address = (ushort)((y_high << 8) | y_low); //16 bit
                    WriteToMemory(y_address, Y);
                    Console.WriteLine("Executing STY Absolute: Store Y");
                    cycle = 4;
                    break;
                //ARITHMETIC:
                case 0xE8: //INX Increment X
                    X = (byte)(X+1);
                    Z = (X == 0);
                    N = (X & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing INX: Increment X");
                    break;
                case 0xC8: //INY Increment Y
                    Y = (byte)(Y + 1);
                    Z = (Y == 0);
                    N = (Y & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing INY: Increment Y");
                    break;
                case 0xCA: //DEX Decrement X
                    X = (byte)(X - 1);
                    Z = (X == 0);
                    N = (X & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing DEX: Decrement X");
                    break;
                case 0x88: //DEY Decrement Y
                    Y = (byte)(Y - 1);
                    Z = (Y == 0);
                    N = (Y & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing DEY: Decrement Y");
                    break;
                //BRANCH:
                case 0x90: //BCC Branch if Carry Clear
                    sbyte bcc_offset = (sbyte)memory.Read((ushort)programCounter++);
                    if(!C)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += bcc_offset;
                        cycle = 3;
                        if((prevPC & 0xFF00) != (programCounter & 0xFF00)){ //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BCC: Branch if Carry Clear");
                    break;
                case 0xB0: //BCS Branch if Carry Set
                    sbyte offset = (sbyte)memory.Read((ushort)programCounter++);
                    if (C)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += offset;
                        cycle = 3;
                        if ((prevPC & 0xFF00) != (programCounter & 0xFF00))
                        { //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BCS: Branch if Carry Set");
                    break;
                case 0xF0: //BEQ Branch if Equal (Z true)
                    sbyte beq_offset = (sbyte)memory.Read((ushort)programCounter++);
                    if (Z)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += beq_offset;
                        cycle = 3;
                        if ((prevPC & 0xFF00) != (programCounter & 0xFF00))
                        { //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BEQ: Branch if Equal");
                    break;
                case 0xD0: //BNE Branch if Not Equal (Z false)
                    sbyte bne_offset = (sbyte)memory.Read((ushort)programCounter++);
                    if (!Z)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += bne_offset;
                        cycle = 3;
                        if ((prevPC & 0xFF00) != (programCounter & 0xFF00))
                        { //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BNE: Branch if Not Equal");
                    break;
                case 0x10: //BPL Branch if Plus (N false)
                    sbyte bpl_offset = (sbyte)memory.Read((ushort)programCounter++);
                    if (!N)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += bpl_offset;
                        cycle = 3;
                        if ((prevPC & 0xFF00) != (programCounter & 0xFF00))
                        { //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BPL: Branch if Plus");
                    break;
                case 0x30: //BMI Branch if Minus (N true)
                    sbyte bmi_offset = (sbyte)memory.Read((ushort)programCounter++);
                    if (N)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += bmi_offset;
                        cycle = 3;
                        if ((prevPC & 0xFF00) != (programCounter & 0xFF00))
                        { //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BMI: Branch if Minus");
                    break;
                case 0x50: //BVC Branch if Overflow Clear (V false)
                    sbyte bvc_offset = (sbyte)memory.Read((ushort)programCounter++);
                    if (!V)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += bvc_offset;
                        cycle = 3;
                        if ((prevPC & 0xFF00) != (programCounter & 0xFF00))
                        { //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BVC: Branch if Overflow Clear");
                    break;
                case 0x70: //BVS Branch if Overflow Set (V true)
                    sbyte bvs_offset = (sbyte)memory.Read((ushort)programCounter++);
                    if (V)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += bvs_offset;
                        cycle = 3;
                        if ((prevPC & 0xFF00) != (programCounter & 0xFF00))
                        { //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BVS: Branch if Overflow Set");
                    break;
                //JUMP:
                case 0x4C: //JMP Absolute
                    byte jmp_low = memory.Read((ushort)(programCounter++));
                    byte jmp_high = memory.Read((ushort)(programCounter++));
                    ushort jmp_address = (ushort)((jmp_high << 8) | jmp_low); //16 bit
                    programCounter = jmp_address;

                    Console.WriteLine("Executing JMP Absolute");
                    cycle = 3;
                    break;
                case 0x20: //JSR Absolute - Store and set
                    byte jsr_low = memory.Read((ushort)(programCounter++));
                    byte jsr_high = memory.Read((ushort)(programCounter++));

                    ushort return_address = (ushort)(programCounter - 1);
                    PushWord(return_address);

                    programCounter = (ushort)((jsr_high << 8) | jsr_low);
                    Console.WriteLine("Executing JSR Absolute: Jump to subroutine");
                    cycle = 6;
                    break;
                case 0x60: //RTS Implied - Return to Subroutine
                    programCounter = PopWord() + 1;
                    Console.WriteLine("Executing RTS Implied: Return to subroutine");
                    cycle = 6;
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
        void WriteToMemory(ushort address, byte value)
        {
            if (address >= 0x2000 && address <= 0x3FFF)
            {
                // $2000–$2007 tükrözve 8 byte-onként
                ushort reg = (ushort)(address & 0x2007);
                switch (reg)
                {
                    case 0x2000: 
                        ppu.SETPPUCTRL(value); 
                        break;
                    case 0x2001: 
                        ppu.SETMASK(value); 
                        break;
                    case 0x2006: 
                        ppu.SETPPUADDR(value); 
                        break;
                    case 0x2007: 
                        ppu.WritePPUDATA(value); 
                        break;
                    default:
                        Console.WriteLine($"PPU Unsupported register: {reg:X4} at address: {address:X4})");
                        break;
                }
                return;
            }

            memory.Write(address, value);
        }
        //STACK METHODS:
        void PushByte(byte value)
        {
            memory.Write((ushort)(0x0100 + SP), value);
            SP--;
        }
        void PushWord(ushort value)
        {
            PushByte((byte)((value >> 8) & 0xFF));
            PushByte((byte)(value & 0xFF));
        }
        byte PopByte()
        {
            SP++;
            return memory.Read((ushort)(0x0100 + SP));
        }
        ushort PopWord()
        {
            byte low = PopByte();
            byte high = PopByte();
            return (ushort)((high << 8) | low);
        }
    }
}

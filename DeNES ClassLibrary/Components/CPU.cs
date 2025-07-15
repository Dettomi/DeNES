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

        bool nmi_triggered = false;
        public CPU(Memory memo, PPU ppu)
        {
            memory = memo;
            this.ppu = ppu;
            programCounter = (ushort)(memory.Read(0xFFFC) | (memory.Read(0xFFFD) << 8)); //NES starts at this address
            SP = 0xFD; //NES default value
            C = false;
            Z = false;
            I = false;
            D = false;
            V = false;
            N = false;
        }
        //public PPU Ppu { set => ppu = value; }

        public int instruction()
        {
            //TODO Implement ReadWord to do little endian nice
            int cycle = 0;
            byte opcode = memory.Read((ushort)(programCounter++));
            Console.WriteLine("Opcode: " + opcode+"(0x"+opcode.ToString("X2")+")");
            Console.WriteLine($"PC: {programCounter:X4}");
            switch (opcode)
            {
                #region ACCESS
                #region LDA 100%
                case 0xA9: //LDA #Immediate (Example.: A9 64 -> A = 64)
                    A = memory.Read((ushort)(programCounter++));
                    SetZN(A);

                    Console.WriteLine($"Executing LDA #Immediate: A = {A}");
                    cycle = 2;
                    break;
                case 0xA5: //LDA Zero Page (Example.: A5 $ZZ -> A = $ZZ)
                    byte ldaZP_address = memory.Read((ushort)(programCounter++));

                    A = memory.Read(ldaZP_address);
                    SetZN(A);

                    Console.WriteLine($"Executing LDA Zero Page: A = {A} [{ldaZP_address:X2}]");
                    cycle = 3;
                    break;
                case 0xB5: //LDA Zero Page,X (Example.: ZP + X)
                    byte ldaZPX_ZpAddress = memory.Read((ushort)(programCounter++));
                    byte ldaZPX_address = (byte)((ldaZPX_ZpAddress + X)); // <= $FF = Zero Page

                    A = memory.Read(ldaZPX_address);
                    SetZN(A);

                    Console.WriteLine($"Executing LDA Zero Page,X: A = {A:X2} [{ldaZPX_address:X2}]");
                    cycle = 4;
                    break;
                case 0xAD: //LDA Absolute (Example.: AD 34 12 -> $1234
                    byte ldaA_low = memory.Read((ushort)(programCounter++));
                    byte ldaA_high = memory.Read((ushort)(programCounter++));
                    ushort ldaA_address = (ushort)((ldaA_high << 8) | ldaA_low); //16 bit

                    A = memory.Read(ldaA_address);
                    SetZN(A);

                    Console.WriteLine($"Executing LDA Absolute: A = {A} [{ldaA_address:X4}]");
                    cycle = 4;
                    break;
                case 0xBD: //LDA Absolute,X ($1234 + X)
                    byte ldaAX_low = memory.Read((ushort)(programCounter++));
                    byte ldaAX_high = memory.Read((ushort)(programCounter++));
                    ushort ldaAX_baseAddress = (ushort)((ldaAX_high << 8) | ldaAX_low);
                    ushort ldaAX_address = (ushort)(ldaAX_baseAddress + X);

                    A = memory.Read(ldaAX_address);
                    SetZN(A);

                    Console.WriteLine($"Executing LDA Absolute,X: A = {A:X2} [{ldaAX_address:X4}]");
                    if (PageCrossed(ldaAX_baseAddress, ldaAX_address))
                    {
                        cycle = 5;
                    }
                    else
                    {
                        cycle = 4;
                    }
                    break;
                case 0xB9: //LDA Absolute,Y
                    byte ldaAY_low = memory.Read((ushort)(programCounter++));
                    byte ldaAY_high = memory.Read((ushort)(programCounter++));
                    ushort ldaAY_baseAddress = (ushort)((ldaAY_high << 8) | ldaAY_low);
                    ushort ldaAY_address = (ushort)(ldaAY_baseAddress + Y);

                    A = memory.Read(ldaAY_address);
                    SetZN(A);

                    Console.WriteLine($"Executing LDA Absolute,Y: A = {A:X2} [{ldaAY_address:X4}]");
                    if (PageCrossed(ldaAY_baseAddress, ldaAY_address))
                    {
                        cycle = 5;
                    }
                    else
                    {
                        cycle = 4;
                    }
                    break;
                case 0xA1: //LDA (Indirect,X) (Example.: $20 + X -> $24)
                    byte ldaIX_zp = memory.Read((ushort)(programCounter++));
                    byte ldaIX_baseAddress = (byte)(ldaIX_zp + X); //Wrap around 
                    byte ldaIX_low = memory.Read((ldaIX_baseAddress));
                    byte ldaIX_high = memory.Read((byte)(ldaIX_baseAddress + 1)); //Wrap around $FF + 1 would be wrong
                    ushort ldaIX_address = (ushort)((ldaIX_high << 8) | ldaIX_low);
                    

                    A = memory.Read(ldaIX_address);
                    SetZN(A);

                    Console.WriteLine($"Executing LDA (Indirect,X): A = {A:X2} [{ldaIX_address:X4}]");
                    cycle = 6;
                    break;
                case 0xB1: //LDA (Indirect),Y
                    byte ldaIY_zp = memory.Read((ushort)(programCounter++));
                    byte ldaIY_low = memory.Read(ldaIY_zp);
                    byte ldaIY_high = memory.Read((byte)((ldaIY_zp + 1)));
                    ushort ldaIY_baseAddress = (ushort)((ldaIY_high << 8) | ldaIY_low);
                    ushort ldaIY_address = (ushort)(ldaIY_baseAddress + Y);

                    A = memory.Read(ldaIY_address);
                    SetZN(A);

                    Console.WriteLine($"Executing LDA (Indirect),Y: A = {A:X2} [{ldaIY_address:X4}]");
                    if (PageCrossed(ldaIY_baseAddress, ldaIY_address))
                    {
                        cycle = 6;
                    }
                    else
                    {
                        cycle = 5;
                    }
                    break;
                #endregion
                #region STA 100%
                case 0x85: //STA Zero Page (STORE A)
                    byte staZP_address = memory.Read((ushort)(programCounter++));

                    memory.Write(staZP_address, A);

                    Console.WriteLine($"Executing STA Zero Page: A = {A:X2} [{staZP_address:X2}]");
                    cycle = 3;
                    break;
                case 0x95: //STA Zero Page,X
                    byte staZPX_baseAddress = memory.Read((ushort)(programCounter++));
                    byte staZPX_address = (byte)(staZPX_baseAddress + X);

                    memory.Write(staZPX_address, A);

                    Console.WriteLine($"Executing STA Zero Page,X: A = {A:X2} [{staZPX_address:X2}]");
                    cycle = 4;
                    break;
                case 0x8D: //STA Absolute
                    byte staA_low = memory.Read((ushort)(programCounter++));
                    byte staA_high = memory.Read((ushort)(programCounter++));
                    ushort staA_address = (ushort)((staA_high << 8) | staA_low); //16 bit

                    memory.Write(staA_address, A);

                    Console.WriteLine($"Executing STA Absolute: A = {A:X2} [{staA_address:X4}]");
                    cycle = 4;
                    break;
                case 0x9D: //STA Absolute,X
                    byte staAX_low = memory.Read((ushort)(programCounter++));
                    byte staAX_high = memory.Read((ushort)(programCounter++));
                    ushort staAX_address = (ushort)(((staAX_high << 8) | staAX_low) + X);

                    memory.Write(staAX_address, A);

                    Console.WriteLine($"Executing STA Absolute,X: A = {A:X2} [{staAX_address:X4}]");
                    cycle = 5;
                    break;
                case 0x99: //STA Absolute,Y
                    byte staAY_low = memory.Read((ushort)(programCounter++));
                    byte staAY_high = memory.Read((ushort)(programCounter++));
                    ushort staAY_address = (ushort)(((staAY_high << 8) | staAY_low) + Y);

                    memory.Write(staAY_address, A);

                    Console.WriteLine($"Executing STA Absolute,Y: A = {A:X2} [{staAY_address:X4}]");
                    cycle = 5;
                    break;
                case 0x81: //STA (Indirect,X) (ZP + X => 16 bit address)
                    byte staIX_zpAddress = memory.Read((ushort)programCounter++);
                    byte staIX_low = memory.Read((byte)((staIX_zpAddress + X) & 0xFF));
                    byte staIX_high = memory.Read((byte)((staIX_zpAddress + X + 1) & 0xFF));
                    ushort staIX_address = (ushort)((staIX_high << 8) | staIX_low);

                    memory.Write(staIX_address, A);

                    Console.WriteLine($"Executing STA (Indirect,X): A = {A:X2} [{staIX_address:X4}]");
                    cycle = 6;
                    break;
                case 0x91: //STA (Indirect,Y)
                    byte staIY_zpAddress = memory.Read((ushort)programCounter++);
                    byte staIY_low = memory.Read(staIY_zpAddress);
                    byte staIY_high = memory.Read((byte)((staIY_zpAddress + 1) & 0xFF));
                    ushort staIY_address = (ushort)((staIY_high << 8) | staIY_low);

                    memory.Write((ushort)(staIY_address+Y), A);

                    Console.WriteLine($"Executing STA Indirect Y: Store A {staIY_address + Y:X4} = {A:X2}");
                    cycle = 6;
                    break;
                
                #endregion
                #region LDX 100%
                case 0xA2: //LDX #Immediate - Load X
                    X = memory.Read((ushort)(programCounter++));
                    SetZN(X);

                    Console.WriteLine($"Executing LDX #Immediate: X = {X}");
                    cycle = 2;
                    break;
                case 0xA6: //LDX Zero Page
                    byte ldxZP_address = memory.Read((ushort)(programCounter++));

                    X = memory.Read(ldxZP_address);
                    SetZN(X);

                    Console.WriteLine($"Executing LDX Zero Page: X = {X:X2} [{ldxZP_address:X2}]");
                    cycle = 3;
                    break;
                case 0xB6: //LDX Zero Page,Y
                    byte ldxZPY_zpAddress = memory.Read((byte)(programCounter++));
                    byte ldxZPY_address = (byte)(ldxZPY_zpAddress + Y);

                    X = memory.Read(ldxZPY_address);
                    SetZN(X);

                    Console.WriteLine($"Executing LDX Zero Page,Y: X = {X:X2} [{ldxZPY_address:X2}]");
                    cycle = 4;
                    break;
                case 0xAE: //LDX Absolute
                    byte ldxA_low = memory.Read((ushort)programCounter++);
                    byte ldxA_high = memory.Read((ushort)programCounter++);
                    ushort ldxA_address = (ushort)((ldxA_high << 8) | ldxA_low);

                    X = memory.Read(ldxA_address);
                    SetZN(X);

                    Console.WriteLine($"Executing LDX Absolute: X = {X:X2} [{ldxA_address:X4}]");
                    break;
                case 0xBE: //LDX Absolute,Y
                    byte ldxAY_low = memory.Read((ushort)(programCounter++));
                    byte ldxAY_high = memory.Read((ushort)(programCounter++));
                    ushort ldxAY_baseAddress = (ushort)((ldxAY_high << 8) | ldxAY_low);
                    ushort ldxAY_address = (ushort)(ldxAY_baseAddress + Y);

                    X = memory.Read(ldxAY_address);
                    SetZN(X);

                    Console.WriteLine($"Executing LDX Absolute,Y: X = {X:X2} [{ldxAY_address:X4}]");
                    if (PageCrossed(ldxAY_baseAddress, ldxAY_address))
                    {
                        cycle = 5;
                    }
                    else
                    {
                        cycle = 4;
                    }
                    break;
                #endregion
                #region STX 100%
                case 0x86: //STX Zero Page
                    byte stxZP_address = memory.Read((ushort)(programCounter++));

                    memory.Write(stxZP_address, X);

                    Console.WriteLine($"Executing STX Zero Page: X = {X:X2} [{stxZP_address:X2}]");
                    cycle = 3;
                    break;
                case 0x96: //STX Zero Page,Y
                    byte stxZPY_zpAddress = memory.Read((ushort)(programCounter++));
                    byte stxZPY_address = (byte)(stxZPY_zpAddress + Y);

                    memory.Write(stxZPY_address, X);

                    Console.WriteLine($"Executing STX Zero Page,Y: X = {X:X2} [{stxZPY_address:X2}]");
                    cycle = 4;
                    break;
                case 0x8E: //STX Absolute
                    byte stxA_low = memory.Read((ushort)(programCounter++));
                    byte stxA_high = memory.Read((ushort)(programCounter++));
                    ushort stxA_address = (ushort)((stxA_high << 8) | stxA_low);

                    memory.Write(stxA_address, X);

                    Console.WriteLine($"Executing STX Absolute: X = {X:X2} [{stxA_address:X4}]");
                    cycle = 4;
                    break;
                #endregion
                #region LDY 100%
                case 0xA0: //LDY #Immediate - Load Y
                    Y = memory.Read((ushort)(programCounter++));
                    SetZN(Y);

                    Console.WriteLine($"Executing LDY #Immediate: Y = {Y}");
                    cycle = 2;
                    break;
                case 0xA4: //LDY Zero Page
                    byte ldyZP_zpAddress = memory.Read((ushort)(programCounter++));

                    Y = memory.Read((ushort)(ldyZP_zpAddress));
                    SetZN(Y);

                    Console.WriteLine($"Executing LDY Zero Page: Y = {Y} [{ldyZP_zpAddress:x2}]");
                    cycle = 3;
                    break;
                case 0xB4: //LDY Zero Page,X
                    byte ldyZPX_zpAddress = memory.Read((ushort)(programCounter++));
                    byte ldyZPX_address = (byte)(ldyZPX_zpAddress + X); //No page crossing allowed (I think)

                    Y = memory.Read((ushort)(ldyZPX_address));
                    SetZN(Y);

                    Console.WriteLine($"Executing LDY Zero Page,X: Y = {Y} [{ldyZPX_address:x2}]");
                    cycle = 4;
                    break;
                case 0xAC: //LDY Absolute
                    byte ldyA_low = memory.Read((ushort)(programCounter++));
                    byte ldyA_high = memory.Read((ushort)(programCounter++));
                    ushort ldyA_address = (ushort)((ldyA_high << 8) | ldyA_low); 

                    Y = memory.Read(ldyA_address);
                    SetZN(Y);

                    Console.WriteLine($"Executing LDY Absolute: Y = {Y} [{ldyA_address:x4}]");
                    cycle = 4;
                    break;
                case 0xBC: //LDY Absolute,X
                    byte ldyAX_low = memory.Read((ushort)(programCounter++));
                    byte ldyAX_high = memory.Read((ushort)(programCounter++));
                    ushort ldyAX_baseAddress = (ushort)((ldyAX_high << 8) | ldyAX_low);
                    ushort ldyAX_address = (ushort)(ldyAX_baseAddress + X);

                    Y = memory.Read(ldyAX_address);
                    SetZN(Y);

                    Console.WriteLine($"Executing LDY Absolute,X: Y = {Y} [{ldyAX_address:x4}]");
                    if(PageCrossed(ldyAX_baseAddress, ldyAX_address))
                    {
                        cycle = 5;
                    }
                    else
                    {
                        cycle = 4;
                    }
                    break;
                #endregion
                #region STY
                case 0x84: //STY Zero Page (STORE Y)
                    byte styZP_address = memory.Read((ushort)(programCounter++));

                    memory.Write(styZP_address, Y);

                    Console.WriteLine($"Executing STY Zero Page: Y = {Y:X2} [{styZP_address:X2}]");
                    cycle = 3;
                    break;
                case 0x94: //STY Zero Page,X
                    byte styZPX_address = memory.Read((ushort)(programCounter++));

                    memory.Write((byte)(styZPX_address+X), Y);

                    Console.WriteLine($"Executing STY Zero Page,X: Y = {Y:X2} [{styZPX_address:X2}]");
                    cycle = 4;
                    break;
                case 0x8C: //STY Absolute 
                    byte styA_low = memory.Read((ushort)(programCounter++));
                    byte styA__high = memory.Read((ushort)(programCounter++));
                    ushort styA_address = (ushort)((styA__high << 8) | styA_low);

                    memory.Write(styA_address, Y);

                    Console.WriteLine($"Executing STY Absolute: Y = {Y:X2} [{styA_address:X4}]");
                    cycle = 4;
                    break;
                #endregion
                #endregion
                #region TRANSFER
                //TRANSFER:
                case 0xAA: //TAX: Transfer A to X
                    X = A;
                    Z = (X == 0);
                    N = (X & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing TAX: Transfer A to X");
                    break;
                case 0xA8: //TAY: Transfer A to Y
                    Y = A;
                    Z = (Y == 0);
                    N = (Y & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing TAY: Transfer A to Y");
                    break;
                case 0xBA: //TSX: Transfer SP to X
                    X = SP;
                    Z = (X == 0);
                    N = (X & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing TSX: Transfer SP to X");
                    break;
                case 0x8A: //TXA: Transfer X to A
                    A = X;
                    Z = (A == 0);
                    N = (A & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing TXA: Transfer X to A");
                    break;
                case 0x9A: //TXS: Transfer X to SP
                    SP = X;
                    cycle = 2;
                    Console.WriteLine("Executing TXS: Transfer X to SP");
                    break;
                case 0x98: //TYA: Transfer Y to A
                    A = Y;
                    Z = (A == 0);
                    N = (A & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing TYA: Transfer Y to A");
                    break;
                #endregion
                #region ARITHMETIC
                //ARITHMETIC:
                case 0x69: // ADC Immediate
                    byte adc_value = memory.Read((ushort)(programCounter++));
                    int sum = A + adc_value + (C ? 1 : 0);
                    C = sum > 0xFF;
                    byte result = (byte)(sum & 0xFF);

                    V = ((A ^ result) & (adc_value ^ result) & 0x80) != 0;
                    A = result;
                    Z = (A == 0);
                    N = (A & 0x80) != 0;
                    cycle = 2;
                    Console.WriteLine($"Executing ADC Immediate: A + {adc_value:X2} + C → {A:X2}");
                    break;
                #region INC 100%
                case 0xE6: //INC Zero Page - Increment Memory (memory = memory + 1)
                    byte incZP_zpAddress = memory.Read((ushort)(programCounter++));
                    byte incZP_value = memory.Read((ushort)incZP_zpAddress);
                    incZP_value++;

                    memory.Write(incZP_zpAddress, incZP_value);
                    SetZN(incZP_value);

                    Console.WriteLine($"Executing INC Zero Page: VALUE: {incZP_value} [{incZP_zpAddress}]");
                    cycle = 5;
                    break;
                case 0xF6: //INC Zero Page,X
                    byte incZPX_zpAddress = memory.Read((ushort)(programCounter++));
                    byte incZPX_address = (byte)(incZPX_zpAddress + X);
                    byte incZPX_value = memory.Read(incZPX_address);
                    incZPX_value++;

                    memory.Write(incZPX_address, incZPX_value);
                    SetZN(incZPX_value);

                    Console.WriteLine($"Executing INC Zero Page,X: VALUE: {incZPX_value} [{incZPX_address}]");
                    cycle = 6;
                    break;
                case 0xEE: //INC Absolute
                    byte incA_low = memory.Read((ushort)(programCounter++));
                    byte incA_high = memory.Read((ushort)(programCounter++));
                    ushort incA_address = (ushort)((incA_high << 8) | incA_low);
                    byte incA_value = memory.Read(incA_address);
                    incA_value++;

                    memory.Write(incA_address, incA_value);
                    SetZN(incA_value);

                    Console.WriteLine($"Executing INC Absolute: VALUE: {incA_value:X2} [{incA_address:X2}]");
                    cycle = 6;
                    break;
                case 0xFE: //INC Absolute,X
                    byte incAX_low = memory.Read((ushort)(programCounter++));
                    byte incAX_high = memory.Read((ushort)(programCounter++));
                    ushort incAX_baseAddress = (ushort)((incAX_high << 8) | incAX_low);
                    ushort incAX_address = (ushort)(incAX_baseAddress + X);
                    byte incAX_value = memory.Read(incAX_address);
                    incAX_value++;

                    memory.Write(incAX_address, incAX_value);
                    SetZN(incAX_value);

                    Console.WriteLine($"Executing INC Absolute,X: VALUE: {incAX_value:X2} [{incAX_address:X2}]");
                    cycle = 7;
                    break;
                #endregion
                #region INX 100%
                case 0xE8: //INX Increment X
                    X = (byte)(X+1);
                    SetZN(X);

                    Console.WriteLine("Executing INX: Increment X");
                    cycle = 2;
                    break;
                #endregion
                #region INY 100%
                case 0xC8: //INY Increment Y
                    Y = (byte)(Y + 1);
                    SetZN(Y);

                    Console.WriteLine("Executing INY: Increment Y");
                    cycle = 2;
                    break;
                #endregion
                case 0xC6: //DEC Decrement Memory Zero Page
                    byte zeroPage_Address = memory.Read((ushort)(programCounter++));
                    byte value = memory.Read(zeroPage_Address);
                    value-=1;
                    memory.Write(zeroPage_Address, value);
                    Z = (value == 0);
                    N = (value & 0x80) != 0;
                    cycle = 5;
                    Console.WriteLine($"Executing DEC Zero Page address: {zeroPage_Address:X2}");
                    break;
                case 0xD6: // DEC Zero Page,X
                    byte deczpx_baseAddr = memory.Read((ushort)(programCounter++));
                    byte deczpx_addr = (byte)((deczpx_baseAddr + X) & 0xFF); // Zero-page wraparound
                    byte deczpx_value = memory.Read(deczpx_addr);
                    deczpx_value--;
                    memory.Write(deczpx_addr, deczpx_value);
                    Z = (deczpx_value == 0);
                    N = (deczpx_value & 0x80) != 0;
                    cycle = 6;
                    Console.WriteLine($"Executing DEC Zero Page,X: [{deczpx_addr:X2}] → {deczpx_value:X2}");
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
                #endregion
                #region SHIFT
                //SHIFT
                case 0x4A: //LSR A Accumulator
                    C = (A & 0x01) != 0;
                    A >>= 1;
                    Z = (A == 0);
                    N = false;
                    cycle = 2;
                    Console.WriteLine($"Executing LSR Accumulator: A = {A:X2}, C = {C}");
                    break;
                #region ROR 100%
                case 0x6A: //ROR Accumulator
                    bool rorAC_newCarry = (A & 0x01) != 0;
                    A = (byte)((A >> 1) | (C ? 0x80 : 0x00));
                    C = rorAC_newCarry;

                    SetZN(A);

                    Console.WriteLine($"Executing ROR Accumulator: VALUE: {A:X2} C={C}");
                    cycle = 2;
                    break;
                case 0x66: //ROR Zero Page
                    byte rorZP_zpAddress = memory.Read((ushort)(programCounter++));
                    byte rorZP_value = memory.Read(rorZP_zpAddress);

                    bool rorZP_newCarry = (rorZP_value & 0x01) != 0;
                    rorZP_value = (byte)((rorZP_value >> 1) | (C ? 0x80 : 0x00));
                    C = rorZP_newCarry;

                    memory.Write(rorZP_zpAddress, rorZP_value);
                    SetZN(rorZP_value);

                    Console.WriteLine($"Executing ROR Zero Page: VALUE: {rorZP_value:X2} [{rorZP_zpAddress:X2}] C={C}");
                    cycle = 5;
                    break;
                case 0x76: //ROR Zero Page,X
                    byte rorZPX_zpAddress = memory.Read((ushort)(programCounter++));
                    byte rorZPX_address = (byte)(rorZPX_zpAddress + X);
                    byte rorZPX_value = memory.Read(rorZPX_address);

                    bool rorZPX_newCarry = (rorZPX_value & 0x01) != 0;
                    rorZPX_value = (byte)((rorZPX_value >> 1) | (C ? 0x80 : 0x00));
                    C = rorZPX_newCarry;

                    memory.Write(rorZPX_address, rorZPX_value);
                    SetZN(rorZPX_value);

                    Console.WriteLine($"Executing ROR Zero Page,X: VALUE: {rorZPX_value:X2} [{rorZPX_address:X2}] C={C}");
                    cycle = 6;
                    break;
                case 0x6E: //ROR Absolute
                    byte rorA_low = memory.Read((ushort)(programCounter++));
                    byte rorA_high = memory.Read((ushort)(programCounter++));
                    ushort rorA_address = (ushort)((rorA_high << 8) | rorA_low);
                    byte rorA_value = memory.Read(rorA_address);

                    bool rorA_newCarry = (rorA_value & 0x01) != 0;
                    rorA_value = (byte)((rorA_value >> 1) | (C ? 0x80 : 0x00));
                    C = rorA_newCarry;

                    memory.Write(rorA_address, rorA_value);
                    SetZN(rorA_value);

                    Console.WriteLine($"Executing ROR Absolute: VALUE: {rorA_value:X2} [{rorA_address:X4}] C={C}");
                    cycle = 6;
                    break;
                case 0x7E: //ROR Absolute,X
                    byte rorAX_low = memory.Read((ushort)(programCounter++));
                    byte rorAX_high = memory.Read((ushort)(programCounter++));
                    ushort rorAX_baseAddress = (ushort)((rorAX_high << 8) | rorAX_low);
                    ushort rorAX_address = (ushort)(rorAX_baseAddress + X);
                    byte rorAX_value = memory.Read(rorAX_address);

                    bool rorAX_newCarry = (rorAX_value & 0x01) != 0;
                    rorAX_value = (byte)((rorAX_value >> 1) | (C ? 0x80 : 0x00));
                    C = rorAX_newCarry;

                    memory.Write(rorAX_address, rorAX_value);
                    SetZN(rorAX_value);

                    Console.WriteLine($"Executing ROR Absolute,X: VALUE: {rorAX_value:X2} [{rorAX_address:X4}] C={C}");
                    cycle = 7;
                    break;
                #endregion
                #endregion
                #region BITWISE
                //BITWISE:
                case 0x29: // AND Immediate
                    byte and_value = memory.Read((ushort)(programCounter++));
                    A &= and_value;
                    Z = (A == 0);
                    N = (A & 0x80) != 0;
                    cycle = 2;
                    Console.WriteLine($"Executing AND Immediate: A &= {and_value:X2} → {A:X2}");
                    break;
                #endregion
                #region COMPARE
                //COMPARE:
                case 0xC9: // CMP Immediate
                    {
                        byte cmp_value = memory.Read((ushort)(programCounter++));
                        byte cmp_result = (byte)(A - cmp_value);
                        C = A >= cmp_value;
                        Z = (cmp_result == 0);
                        N = (cmp_result & 0x80) != 0;
                        cycle = 2;
                        Console.WriteLine($"Executing CMP Immediate: A = {A:X2}, value = {cmp_value:X2}, C = {C}, Z = {Z}, N = {N}");
                        break;
                    }
                #endregion
                #region BRANCH
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
                #endregion
                #region JUMP
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
                #endregion
                #region STACK
                #endregion
                #region FLAGS
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
                #endregion
                #region OTHER/UNOFFICAL
                //Other:
                case 0xEA: //NOP (No Operation)
                    Console.WriteLine("Executing NOP: No Operation");
                    cycle = 2;
                    break;
                case 0x2B: // ANC (Unofficial): AND + set Carry = bit 7
                    byte anc_value = memory.Read((ushort)(programCounter++));
                    A &= anc_value;
                    C = (A & 0x80) != 0;
                    Z = (A == 0);
                    N = (A & 0x80) != 0;
                    cycle = 2;
                    Console.WriteLine($"Executing ANC (unofficial): A &= {anc_value:X2} → {A:X2}, C = {C}");
                    break;
                //PLACE:
                case 0x0A: // ASL A (Accumulator)
                    {
                        C = (A & 0x80) != 0;   // Save bit 7 into Carry
                        A <<= 1;
                        Z = (A == 0);
                        N = (A & 0x80) != 0;
                        cycle = 2;
                        Console.WriteLine($"Executing ASL A: A = {A:X2}, C = {C}");
                        break;
                    }
                case 0x6C: // JMP (indirect)
                    {
                        // Fetch the indirect address location (pointer)
                        byte ptrLow = memory.Read((ushort)(programCounter++));
                        byte ptrHigh = memory.Read((ushort)(programCounter++));
                        ushort ptr = (ushort)((ptrHigh << 8) | ptrLow);

                        // Emulate 6502 bug: if the low byte is 0xFF, the high byte wraps around in the same page
                        byte addrLow = memory.Read(ptr);
                        byte addrHigh;
                        if ((ptr & 0x00FF) == 0x00FF)
                        {
                            // Simulate page boundary bug
                            addrHigh = memory.Read((ushort)(ptr & 0xFF00));
                        }
                        else
                        {
                            addrHigh = memory.Read((ushort)(ptr + 1));
                        }

                        ushort jmpTarget = (ushort)((addrHigh << 8) | addrLow);
                        programCounter = jmpTarget;

                        Console.WriteLine($"Executing JMP (indirect): Jump to [{jmpTarget:X4}] from pointer [{ptr:X4}]");
                        cycle = 5;
                        break;
                    }
                case 0x68: // PLA
                    {
                        A = PopByte();
                        Z = (A == 0);
                        N = (A & 0x80) != 0;
                        cycle = 4;
                        Console.WriteLine($"Executing PLA: Pulled A = {A:X2}");
                        break;
                    }
                case 0x14: // Unofficial NOP Zero Page,X
                    {
                        byte baseAddr = memory.Read((ushort)(programCounter++));
                        byte addr = (byte)((baseAddr + X) & 0xFF); // zero-page wraparound
                        _ = memory.Read(addr); // discard value
                        cycle = 4;
                        Console.WriteLine($"Executing NOP (unofficial 0x14) at [{addr:X2}]");
                        break;
                    }
                case 0x48: // PHA
                    {
                        PushByte(A);
                        cycle = 3;
                        Console.WriteLine($"Executing PHA: Pushed A = {A:X2}");
                        break;
                    }
                case 0x65: // ADC Zero Page
                    {
                        byte adczp_addr = memory.Read((ushort)(programCounter++));
                        byte adczp_value = memory.Read(adczp_addr);
                        int adczp_sum = A + adczp_value + (C ? 1 : 0);

                        C = adczp_sum > 0xFF;
                        byte adczp_result = (byte)(adczp_sum & 0xFF);
                        V = ((A ^ adczp_result) & (adczp_value ^ adczp_result) & 0x80) != 0;

                        A = adczp_result;
                        Z = (A == 0);
                        N = (A & 0x80) != 0;

                        cycle = 3;
                        Console.WriteLine($"Executing ADC Zero Page: A + {adczp_value:X2} + C → {A:X2}");
                        break;
                    }
                case 0x12: // Unofficial NOP (Indirect)
                    {
                        byte zpn = memory.Read((ushort)(programCounter++));
                        byte addr_lo = memory.Read((byte)((zpn + X) & 0xFF));
                        byte addr_hi = memory.Read((byte)((zpn + X + 1) & 0xFF));
                        ushort effectiveAddr = (ushort)((addr_hi << 8) | addr_lo);
                        _ = memory.Read(effectiveAddr); // discard result
                        cycle = 5;
                        Console.WriteLine($"Executing unofficial NOP 0x12: (Indirect,X) addr = {effectiveAddr:X4}");
                        break;
                    }
                case 0x24: // BIT Zero Page
                    {
                        byte zpb = memory.Read((ushort)(programCounter++));
                        byte v = memory.Read(zpb);
                        Z = (A & v) == 0;
                        V = (v & 0x40) != 0;
                        N = (v & 0x80) != 0;
                        cycle = 3;
                        Console.WriteLine($"Executing BIT ZeroPage: A&[{zpb:X2}] -> Z={Z}, V={V}, N={N}");
                        break;
                    }
                case 0x2C: // BIT Absolute
                    {
                        byte lo = memory.Read((ushort)(programCounter++));
                        byte hi = memory.Read((ushort)(programCounter++));
                        ushort addr = (ushort)((hi << 8) | lo);
                        byte v = memory.Read(addr);
                        Z = (A & v) == 0;
                        V = (v & 0x40) != 0;
                        N = (v & 0x80) != 0;
                        cycle = 4;
                        Console.WriteLine($"Executing BIT Absolute: A&[{addr:X4}] -> Z={Z}, V={V}, N={N}");
                        break;
                    }
                case 0xEC: // CPX Absolute
                    {
                        byte cpx_low = memory.Read((ushort)(programCounter++));
                        byte cpx_high = memory.Read((ushort)(programCounter++));
                        ushort cpx_addr = (ushort)((cpx_high << 8) | cpx_low);
                        byte cpx_value = memory.Read(cpx_addr);

                        byte cpx_result = (byte)(X - cpx_value);
                        C = X >= cpx_value;
                        Z = cpx_result == 0;
                        N = (cpx_result & 0x80) != 0;

                        Console.WriteLine($"Executing CPX Absolute: X={X:X2}, mem[{cpx_addr:X4}]={cpx_value:X2}, C={C}, Z={Z}, N={N}");
                        cycle = 4;
                        break;
                    }
                case 0x09: // ORA #Immediate
                    {
                        byte ora_value = memory.Read((ushort)(programCounter++));
                        A |= ora_value;
                        Z = (A == 0);
                        N = (A & 0x80) != 0;
                        Console.WriteLine($"Executing ORA Immediate: A |= {ora_value:X2} → {A:X2}");
                        cycle = 2;
                        break;
                    }
                case 0x45: // EOR Zero Page
                    {
                        byte eorZP_addr = memory.Read((ushort)(programCounter++));
                        byte eorvalue = memory.Read(eorZP_addr);
                        A ^= eorvalue;
                        Z = (A == 0);
                        N = (A & 0x80) != 0;
                        Console.WriteLine($"Executing EOR Zero Page: A ^= [{eorZP_addr:X2}] = {eorvalue:X2} → {A:X2}");
                        cycle = 3;
                        break;
                    }
                case 0x40: // RTI - Return from Interrupt
                    byte flags = PopByte();
                    SetStatusFlags(flags);
                    programCounter = PopWord();
                    cycle = 6;
                    Console.WriteLine("Executing RTI: Return from Interrupt");
                    break;
                case 0x05: // ORA Zero Page
                    byte zpAddr = memory.Read((ushort)(programCounter++));
                    byte oraZPvalue = memory.Read(zpAddr);
                    A |= oraZPvalue;
                    SetZN(A);
                    Console.WriteLine($"Executing ORA Zero Page: A |= [{zpAddr:X2}] = {oraZPvalue:X2} → {A:X2}");
                    cycle = 3;
                    break;
                case 0x2A: // ROL A (Accumulator)
                    bool newCarry = (A & 0x80) != 0;
                    A = (byte)((A << 1) | (C ? 1 : 0));
                    C = newCarry;
                    SetZN(A);
                    Console.WriteLine($"Executing ROL A: A = {A:X2}, C = {C}");
                    cycle = 2;
                    break;
                case 0x06: // ASL Zero Page
                    byte aslZP_zpAddress = memory.Read((ushort)(programCounter++));
                    byte aslZP_value = memory.Read(aslZP_zpAddress);

                    C = (aslZP_value & 0x80) != 0;
                    aslZP_value = (byte)(aslZP_value << 1);

                    memory.Write(aslZP_zpAddress, aslZP_value);
                    SetZN(aslZP_value);

                    Console.WriteLine($"Executing ASL Zero Page: [{aslZP_zpAddress:X2}] ← {aslZP_value:X2}, C = {C}");
                    cycle = 5;
                    break;
                case 0x19: // ORA Absolute,Y
                    byte oraAY_low = memory.Read((ushort)(programCounter++));
                    byte oraAY_high = memory.Read((ushort)(programCounter++));
                    ushort oraAY_baseAddress = (ushort)((oraAY_high << 8) | oraAY_low);
                    ushort oraAY_address = (ushort)(oraAY_baseAddress + Y);

                    byte oraAY_value = memory.Read(oraAY_address);
                    A |= oraAY_value;
                    SetZN(A);

                    Console.WriteLine($"Executing ORA Absolute,Y: A |= {oraAY_value:X2} from [{oraAY_address:X4}] → {A:X2}");

                    if (PageCrossed(oraAY_baseAddress, oraAY_address))
                        cycle = 5;
                    else
                        cycle = 4;
                    break;
                case 0x46: // LSR Zero Page
                    byte lsrZP_zpAddress = memory.Read((ushort)(programCounter++));
                    byte lsrZP_value = memory.Read(lsrZP_zpAddress);

                    C = (lsrZP_value & 0x01) != 0;
                    lsrZP_value = (byte)(lsrZP_value >> 1);

                    memory.Write(lsrZP_zpAddress, lsrZP_value);
                    SetZN(lsrZP_value);

                    Console.WriteLine($"Executing LSR Zero Page: [{lsrZP_zpAddress:X2}] → {lsrZP_value:X2}, C={C}");
                    cycle = 5;
                    break;
                case 0xE0: // CPX Immediate
                    byte cpxI_value = memory.Read((ushort)(programCounter++));
                    byte cpxI_result = (byte)(X - cpxI_value);

                    C = X >= cpxI_value;
                    Z = (cpxI_result == 0);
                    N = (cpxI_result & 0x80) != 0;

                    Console.WriteLine($"Executing CPX Immediate: X = {X:X2}, value = {cpxI_value:X2}, C = {C}, Z = {Z}, N = {N}");
                    cycle = 2;
                    break;
                case 0xC0: // CPY Immediate
                    byte cpy_value = memory.Read((ushort)(programCounter++));
                    byte cpy_result = (byte)(Y - cpy_value);

                    C = Y >= cpy_value;
                    Z = (cpy_result == 0);
                    N = (cpy_result & 0x80) != 0;

                    Console.WriteLine($"Executing CPY Immediate: Y = {Y:X2}, value = {cpy_value:X2}, C = {C}, Z = {Z}, N = {N}");
                    cycle = 2;
                    break;
                case 0x26: // ROL Zero Page
                    byte rolZP_zpAddress = memory.Read((ushort)(programCounter++));
                    byte rolZP_value = memory.Read(rolZP_zpAddress);

                    bool rolZP_newCarry = (rolZP_value & 0x80) != 0;
                    rolZP_value = (byte)((rolZP_value << 1) | (C ? 1 : 0));
                    C = rolZP_newCarry;

                    memory.Write(rolZP_zpAddress, rolZP_value);
                    SetZN(rolZP_value);

                    Console.WriteLine($"Executing ROL Zero Page: [{rolZP_zpAddress:X2}] → {rolZP_value:X2}, C={C}");
                    cycle = 5;
                    break;
                case 0x25: // AND Zero Page
                    byte andZP_zpAddress = memory.Read((ushort)(programCounter++));
                    byte andZP_value = memory.Read(andZP_zpAddress);

                    A &= andZP_value;
                    SetZN(A);

                    Console.WriteLine($"Executing AND Zero Page: A &= [{andZP_zpAddress:X2}] = {andZP_value:X2} → {A:X2}");
                    cycle = 3;
                    break;
                case 0x55: // EOR Zero Page,X
                    byte eorZPX_zpAddress = memory.Read((ushort)(programCounter++));
                    byte eorZPX_address = (byte)(eorZPX_zpAddress + X);
                    byte eorZPX_value = memory.Read(eorZPX_address);

                    A ^= eorZPX_value;
                    SetZN(A);

                    Console.WriteLine($"Executing EOR Zero Page,X: A ^= [{eorZPX_address:X2}] = {eorZPX_value:X2} → {A:X2}");
                    cycle = 4;
                    break;
                case 0x6D: // ADC Absolute
                    byte adcA_low = memory.Read((ushort)(programCounter++));
                    byte adcA_high = memory.Read((ushort)(programCounter++));
                    ushort adcA_address = (ushort)((adcA_high << 8) | adcA_low);
                    byte adcA_value = memory.Read(adcA_address);

                    int adcA_sum = A + adcA_value + (C ? 1 : 0);
                    C = adcA_sum > 0xFF;
                    byte adcA_result = (byte)(adcA_sum & 0xFF);
                    V = ((A ^ adcA_result) & (adcA_value ^ adcA_result) & 0x80) != 0;

                    A = adcA_result;
                    SetZN(A);

                    Console.WriteLine($"Executing ADC Absolute: A + {adcA_value:X2} + C → {A:X2}");
                    cycle = 4;
                    break;
                case 0x35: // AND Zero Page,X
                    byte andZPX_zpAddress = memory.Read((ushort)(programCounter++));
                    byte andZPX_address = (byte)(andZPX_zpAddress + X);
                    byte andZPX_value = memory.Read(andZPX_address);

                    A &= andZPX_value;
                    SetZN(A);

                    Console.WriteLine($"Executing AND Zero Page,X: A &= [{andZPX_address:X2}] = {andZPX_value:X2} → {A:X2}");
                    cycle = 4;
                    break;


                #endregion

                default:
                    Console.WriteLine("Unknown opcode: " + opcode + "(0x" + opcode.ToString("X2") + ")");
                    break;
                }
            return cycle;
        }
        void SetZN(byte value)
        {
            Z = (value == 0);
            N = (value & 0x80) != 0; //10000000
        }
        bool PageCrossed(ushort baseAddress, ushort address)
        {
            return (baseAddress & 0xFF00) != (address & 0xFF00);
        }
        //RENDERING:
        public void CheckNMI()
        {
            if ((ppu.READPPUSTATUS() & 0x80) != 0 && !nmi_triggered)
            {
                TriggerNMI();
                nmi_triggered = true;
            }

            if ((ppu.READPPUSTATUS() & 0x80) == 0)
            {
                nmi_triggered = false;
            }
        }
        public void TriggerNMI()
        {
            PushWord((ushort)(programCounter));
            PushByte(GetStatusFlags());
            I = true;
            programCounter = (ushort)(memory.Read(0xFFFA) | (memory.Read(0xFFFB) << 8));
        }
        byte GetStatusFlags()
        {
            byte flags = 0;
            if (N) flags |= 0x80;  // Negative
            if (V) flags |= 0x40;  // Overflow
            flags |= 0x20;         // Unused bit (always set)
                                   // Bit 4 is B (Break), should be set manually in BRK/interrupts only
            if (D) flags |= 0x08;  // Decimal
            if (I) flags |= 0x04;  // Interrupt Disable
            if (Z) flags |= 0x02;  // Zero
            if (C) flags |= 0x01;  // Carry
            return flags;
        }
        void SetStatusFlags(byte flags)
        {
            N = (flags & 0x80) != 0;
            V = (flags & 0x40) != 0;
            // Bit 0x20 (unused) is ignored
            D = (flags & 0x08) != 0;
            I = (flags & 0x04) != 0;
            Z = (flags & 0x02) != 0;
            C = (flags & 0x01) != 0;
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

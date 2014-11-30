﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Atomix.CompilerExt;
using Atomix.CompilerExt.Attributes;
using Atomix.Assembler;
using Atomix.Assembler.x86;
using Kernel_alpha.x86;
using Kernel_alpha.x86.Intrinsic;
using Core = Atomix.Assembler.AssemblyHelper;

namespace Kernel_alpha
{
    [Kernel(CPUArch.x86, "0x100000")]//Fixed Entrypoint, if you change it than i kill you :)
    public static class Kernel_x86
    {
        [Assembly, Plug("Kernel_Main")]
        public static void main()
        {
            /* Will do some assembly, because it can't be managed via C# :(
             * So, first task is to set Multiboot header
             */
            Core.DataMember.Add(new AsmData("MultibootSignature", BitConverter.GetBytes(0x1BADB002))); //0x100000
            Core.DataMember.Add(new AsmData("MultibootFlags", BitConverter.GetBytes(65539)));//0x100004
            Core.DataMember.Add(new AsmData("MultibootChecksum", BitConverter.GetBytes(-464433157)));//0x100008
            Core.DataMember.Add(new AsmData("MultibootHeaderAddr", "dd MultibootSignature"));//0x10000C
            Core.DataMember.Add(new AsmData("MultibootLoadAddr", "dd MultibootSignature"));//0x100010
            Core.DataMember.Add(new AsmData("MultibootLoadEndAddr", "dd Compiler_End"));//0x100014
            Core.DataMember.Add(new AsmData("MultibootBSSEndAddr", "dd Compiler_End"));//0x100018
            Core.DataMember.Add(new AsmData("MultibootEntryAddr", "dd Kernel_Main")); //0x10001C            
            Core.DataMember.Add(new AsmData("GDT_And_IDT_Content:", "TIMES 3000 db 0"));//0x100020 --> First IDT than GDT
            Core.DataMember.Add(new AsmData("Before_Kernel_Stack:", "TIMES 0x5000 db 0"));
            Core.DataMember.Add(new AsmData("Stack_Entrypoint:", string.Empty));

            /* Here is Entrypoint Method */
            Core.AssemblerCode.Add(new Cli()); //Clear interrupts first !!
            /*
            //SSE Init
            Core.AssemblerCode.Add(new Mov { DestinationReg = Registers.EAX, SourceReg = Registers.CR4 });
            Core.AssemblerCode.Add(new Or { DestinationReg = Registers.EAX, SourceRef = "0x100" });
            Core.AssemblerCode.Add(new Mov { DestinationReg = Registers.CR4, SourceReg = Registers.EAX });
            Core.AssemblerCode.Add(new Mov { DestinationReg = Registers.EAX, SourceReg = Registers.CR4 });

            Core.AssemblerCode.Add(new Or { DestinationReg = Registers.EAX, SourceRef = "0x200" });
            Core.AssemblerCode.Add(new Mov { DestinationReg = Registers.CR4, SourceReg = Registers.EAX });
            Core.AssemblerCode.Add(new Mov { DestinationReg = Registers.EAX, SourceReg = Registers.CR0 });

            Core.AssemblerCode.Add(new And { DestinationReg = Registers.EAX, SourceRef = "0xfffffffd" });
            Core.AssemblerCode.Add(new Mov { DestinationReg = Registers.CR0, SourceReg = Registers.EAX });
            Core.AssemblerCode.Add(new Mov { DestinationReg = Registers.EAX, SourceReg = Registers.CR0 });

            Core.AssemblerCode.Add(new And { DestinationReg = Registers.EAX, SourceRef = "0x1" });
            Core.AssemblerCode.Add(new Mov { DestinationReg = Registers.CR0, SourceReg = Registers.EAX });
            */
            //Setup Stack pointer, We do rest things later (i.e. Another method) because they are managed :)
            Core.AssemblerCode.Add(new Mov { DestinationReg = Registers.ESP, SourceRef = "Stack_Entrypoint" });
            Core.AssemblerCode.Add(new Push { DestinationReg = Registers.EAX });
            Core.AssemblerCode.Add(new Push { DestinationReg = Registers.EBX });//Push Multiboot Header Info Address
            Core.AssemblerCode.Add(new Call ("Kernel_Start"));
        }

        [Plug("Kernel_Start")]
        public static unsafe void Start (uint magic, uint address)
        {
            /* Placement Address */
            Heap.PlacementAddress = Native.EndOfKernel();

            /* Setup Multiboot */
            Multiboot.Setup(magic, address);
            
            /* Clear Interrupts */
            Native.ClearInterrupt();

            /* Setup PIC */
            PIC.Setup();
            
            /* Setup GDT & Enter into protected mode */
            GDT.Setup();
            
            /* Setup IDT */
            IDT.Setup();
            
            /* Enable Interrupts */
            Native.SetInterrupt();

            /* Call Compiler Flush : should be before any virtual class called */
            Native.CompilerFlush();

            x86.Paging.Setup(0x1000000);

            /* Setup Multitasking */
            Multitasking.CreateTask(0, true); //This is System Update thread            
            Multitasking.Init();//Start Multitasking
            
            /* Call our kernel instance now */
            try
            {
                Caller.Start();                
                while(true)
                {
                    Caller.Update();
                }
            }
            catch (Exception e)
            {
                //Kernel PANIC !!
                Console.WriteLine(e.Message);
            }

            while (true)  //Set CPU in Infinite loop DON'T REMOVE THIS ELSE I'll KILL YOU ^^
            {
                Native.ClearInterrupt();
                Native.Halt();
            };
        }
    }
}

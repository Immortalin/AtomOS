﻿/*
* PROJECT:          Atomix Development
* LICENSE:          BSD 3-Clause (LICENSE.md)
* PURPOSE:          Ldelem_I MSIL
* PROGRAMMERS:      Aman Priyadarshi (aman.eureka@gmail.com)
*/

using System;
using System.Reflection;

using Atomixilc.Machine;
using Atomixilc.Attributes;

namespace Atomixilc.IL
{
    [ILImpl(ILCode.Ldelem_I)]
    internal class Ldelem_I_il : MSIL
    {
        public Ldelem_I_il()
            : base(ILCode.Ldelem_I)
        {

        }

        /*
         * URL : https://msdn.microsoft.com/en-us/library/system.reflection.emit.opcodes.Ldelem_I(v=vs.110).aspx
         * Description : Loads the element with type native int at a specified array index onto the top of the evaluation stack as a native int.
         */
        internal override void Execute(Options Config, OpCodeType xOp, MethodBase method, Optimizer Optimizer)
        {
            if (Optimizer.vStack.Count < 2)
                throw new Exception("Internal Compiler Error: vStack.Count < 2");

            /* The stack transitional behavior, in sequential order, is:
             * An object reference array is pushed onto the stack.
             * An index value index is pushed onto the stack.
             * index and array are popped from the stack; the value stored at position index in array is looked up.
             * The value is pushed onto the stack.
             */

            var itemA = Optimizer.vStack.Pop();
            var itemB = Optimizer.vStack.Pop();

            switch (Config.TargetPlatform)
            {
                case Architecture.x86:
                    {
                        if (!itemA.SystemStack)
                            throw new Exception(string.Format("UnImplemented-RegisterType '{0}'", msIL));

                        if (!itemB.SystemStack)
                            throw new Exception(string.Format("UnImplemented-RegisterType '{0}'", msIL));

                        Ldelem_il.Executex86(4, true);
                        Optimizer.vStack.Push(new StackItem(typeof(int)));
                        Optimizer.SaveStack(xOp.NextPosition);
                    }
                    break;
                default:
                    throw new Exception(string.Format("Unsupported target platform '{0}' for MSIL '{1}'", Config.TargetPlatform, msIL));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace EmitAopTest.Extensions
{
    public static class ILGeneratorExtensions
    {
        public static void EmitLoadArg(this ILGenerator ilGenerator, int index)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }

            switch (index)
            {
                case 0:
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    ilGenerator.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    ilGenerator.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    if (index <= byte.MaxValue) ilGenerator.Emit(OpCodes.Ldarg_S, (byte)index);
                    else ilGenerator.Emit(OpCodes.Ldarg, index);
                    break;
            }
        }

        public static void EmitInt(this ILGenerator ilGenerator, int value)
        {
            OpCode c;
            switch (value)
            {
                case -1:
                    c = OpCodes.Ldc_I4_M1;
                    break;
                case 0:
                    c = OpCodes.Ldc_I4_0;
                    break;
                case 1:
                    c = OpCodes.Ldc_I4_1;
                    break;
                case 2:
                    c = OpCodes.Ldc_I4_2;
                    break;
                case 3:
                    c = OpCodes.Ldc_I4_3;
                    break;
                case 4:
                    c = OpCodes.Ldc_I4_4;
                    break;
                case 5:
                    c = OpCodes.Ldc_I4_5;
                    break;
                case 6:
                    c = OpCodes.Ldc_I4_6;
                    break;
                case 7:
                    c = OpCodes.Ldc_I4_7;
                    break;
                case 8:
                    c = OpCodes.Ldc_I4_8;
                    break;
                default:
                    if (value >= -128 && value <= 127)
                    {
                        ilGenerator.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        ilGenerator.Emit(OpCodes.Ldc_I4, value);
                    }
                    return;
            }
            ilGenerator.Emit(c);
        }

        public static void EmitLdRef(this ILGenerator ilGenerator, Type type)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (type == typeof(short))
            {
                ilGenerator.Emit(OpCodes.Ldind_I1);
            }
            else if (type == typeof(Int16))
            {
                ilGenerator.Emit(OpCodes.Ldind_I2);
            }
            else if (type == typeof(Int32))
            {
                ilGenerator.Emit(OpCodes.Ldind_I4);
            }
            else if (type == typeof(Int64))
            {
                ilGenerator.Emit(OpCodes.Ldind_I8);
            }
            else if (type == typeof(float))
            {
                ilGenerator.Emit(OpCodes.Ldind_R4);
            }
            else if (type == typeof(double))
            {
                ilGenerator.Emit(OpCodes.Ldind_R8);
            }
            else if (type == typeof(ushort))
            {
                ilGenerator.Emit(OpCodes.Ldind_U1);
            }
            else if (type == typeof(UInt16))
            {
                ilGenerator.Emit(OpCodes.Ldind_U2);
            }
            else if (type == typeof(UInt32))
            {
                ilGenerator.Emit(OpCodes.Ldind_U4);
            }
            else if (type.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Ldobj);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldind_Ref);
            }
        }

        public static void EmitStRef(this ILGenerator ilGenerator, Type type)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (type == typeof(short))
            {
                ilGenerator.Emit(OpCodes.Stind_I1);
            }
            else if (type == typeof(Int16))
            {
                ilGenerator.Emit(OpCodes.Stind_I2);
            }
            else if (type == typeof(Int32))
            {
                ilGenerator.Emit(OpCodes.Stind_I4);
            }
            else if (type == typeof(Int64))
            {
                ilGenerator.Emit(OpCodes.Stind_I8);
            }
            else if (type == typeof(float))
            {
                ilGenerator.Emit(OpCodes.Stind_R4);
            }
            else if (type == typeof(double))
            {
                ilGenerator.Emit(OpCodes.Stind_R8);
            }
            else if (type.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Stobj);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Stind_Ref);
            }
        }

        public static void EmitConvertToObject(this ILGenerator ilGenerator, Type typeFrom)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (typeFrom == null)
            {
                throw new ArgumentNullException(nameof(typeFrom));
            }

            ilGenerator.Emit(OpCodes.Box, typeFrom);
        }

        public static void EmitConvertFromObject(this ILGenerator ilGenerator, Type typeTo)
        {
            if (ilGenerator == null)
            {
                throw new ArgumentNullException(nameof(ilGenerator));
            }
            if (typeTo == null)
            {
                throw new ArgumentNullException(nameof(typeTo));
            }

            ilGenerator.Emit(OpCodes.Unbox_Any, typeTo);
        }
    }
}

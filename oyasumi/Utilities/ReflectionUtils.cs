using oyasumi.Database;
using oyasumi.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using FastExpressionCompiler;
using Microsoft.Extensions.Internal;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace oyasumi.Utilities
{
    public class ReflectionUtils
    {
        public delegate void HandleDelegate(Packet p, Presence pr);
        public static Action<Packet, Presence> CompilePacketHandler(MethodInfo meth)
        {
            var dynMethod = new DynamicMethod("Handle",
                typeof(void),
                new Type[] {
                    typeof(Packet),
                    typeof(Presence)
                },
                typeof(string).Module);

            var il = dynMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, meth);
            il.Emit(OpCodes.Ret);

            return (Action<Packet, Presence>)dynMethod.CreateDelegate(typeof(Action<Packet, Presence>));
        }

        public static ObjectMethodExecutorCompiledFast GetExecutor(MethodInfo meth)
        {
            return ObjectMethodExecutorCompiledFast.Create(meth, meth.DeclaringType.GetTypeInfo());
        }
    }
}

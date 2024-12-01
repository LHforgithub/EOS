using System;
using System.Reflection;

namespace EOS.Tiles
{
    internal sealed class EOSMethodData
    {
        public EOSMethodData(MethodInfo methodInfo, int priority)
        {
            Method = methodInfo;
            Priority = priority;
        }
        public MethodInfo Method { get; private set; }
        public bool IsStatic => Method?.IsStatic ?? false;
        public int Priority { get; private set; }



        public Type MethodDeclaringType
        {
            get
            {
                _MethodDeclaringType ??= Method?.ReflectedType;
                return _MethodDeclaringType;
            }
        }
        private Type _MethodDeclaringType = null;


        public override string ToString()
        {
            return $"[ReflectedType : {Method.ReflectedType}, Priority : {Priority}]";
        }
    }
}

using System;
using System.Reflection;

namespace EOS.Tiles
{
    internal class EOSMethod : IComparable<EOSMethod>
    {
        public Guid GUID = Guid.NewGuid();
        public EOSMethodData Data { get; set; }
        public object TargetObject { get; set; } = null;
        public int LayerPriority { get; set; } = 0;
        public Type TagrtObjectType
        {
            get
            {
                _TagrtType ??= TargetObject?.GetType();
                return _TagrtType;
            }
        }
        private Type _TagrtType = null;
        public MethodInfo Method
        {
            get
            {
                _method ??= Data.Method;
                if (TargetObject is not null && TagrtObjectType.AssemblyQualifiedName != Data.MethodDeclaringType.AssemblyQualifiedName)
                {
                    throw new InvalidOperationException($"Different Type between target object : {TagrtObjectType} and calling method : {Data.MethodDeclaringType}");
                }
                return _method;
            }
        }
        private MethodInfo _method = null;


        public int CompareTo(EOSMethod other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            return (-Data.Priority - LayerPriority).CompareTo(-other.Data.Priority - other.LayerPriority);
        }
        public override string ToString()
        {
            return $"EOSMethod : [GUID : {GUID}, Data : {Data.ToString()}]";
        }
    }
}

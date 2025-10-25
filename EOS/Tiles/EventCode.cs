using EOS.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
namespace EOS.Tiles
{
    /// <summary>事件码值</summary>
    public class EventCode
    {
        /// <summary>事件码。默认值为<see cref="CodeType"/>的<see cref="Type.AssemblyQualifiedName"/>。</summary>
        /// <remarks>
        /// 如果使用了<see cref="EventCodeAttribute"/>特性定义此类，请保持此项为空值。
        /// </remarks>
        public string Key { get; set; }
        /// <summary>定义事件码的类型。</summary>
        /// <remarks>
        /// 该项与<see cref="Key"/>仅需其中一边为非<see langword="null"/>。
        /// </remarks>
        public Type CodeType { get; set; }
        /// <summary>事件将广播的方法定义。</summary>
        /// <remarks>
        /// 该方法不能是构造函数或者未定义所有类型的泛型方法。
        /// <para>该项与<see cref="Parameters"/>、<see cref="ReturnType"/>
        /// 仅需其中一边为非<see langword="null"/>。</para>
        /// </remarks>
        public MethodInfo Method { get; set; }
        /// <summary>事件广播的方法定义的参数集合。列表顺序即为参数顺序。</summary>
        public List<ParameterInfo> Parameters { get; set; } = null;
        /// <summary>事件广播的方法定义的参数集合。用于动态创建<see cref="EventCode"/>的<see cref="Method"/></summary>
        public List<Type> ParametersType { get; set; } = null;
        /// <summary>事件广播的方法定义的返回值类型。</summary>
        public Type ReturnType { get; set; }
        ///// <summary>事件广播的方法定义的泛型参数类型（约束类型）。</summary>
        //public List<Type> GenericArguments { get; set; } = new();
    }
}

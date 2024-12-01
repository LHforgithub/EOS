using EOS.Tiles;
using System;
using System.Reflection;
namespace EOS.Attributes
{
    /// <summary>事件特性</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class EventListenerAttribute : Attribute
    {
        internal Type CodeType { get; set; }
        /// <summary>无参方式声明特性。用于在类上注明该<see cref="Type"/>可以作为接收<see cref="EventCode"/>广播的实例。</summary>
        public EventListenerAttribute()
        {

        }
        /// <summary>
        /// 用于在方法上注明该<see cref="MethodInfo"/>为对应<see cref="EventCode"/>的委托方法。
        /// </summary>
        /// <param name="type">继承了<see cref="EventCodeAttribute"/>特性的类。</param>
        /// <remarks>
        /// 在类上使用等同于无参构造函数<see cref="EventListenerAttribute()"/>
        /// </remarks>
        public EventListenerAttribute(Type type)
        {
            if (CheckParameter(type))
            {
                CodeType = type;
                return;
            }
            throw new ArgumentException($"{nameof(type)} can not use as an EventCode.");
        }
        internal bool CheckParameter(Type type)
        {
            //TODO 声明的事件与标注的方法的参数合法性判断
            return true;
        }
    }
}

using System;

namespace EOS.Attributes
{
    /// <summary> 用于标识事件委托在队列中的优先级。数值越大，优先级越高，越先被调用。</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class EventPriorityAttribute : Attribute
    {
        /// <summary> 创建优先等级。默认使用Normal等级。 </summary>
        public EventPriorityAttribute(int priority = (int)Attributes.Priority.Normal)
        {
            Priority = priority;
        }
        /// <summary> 创建优先等级。默认使用Normal等级。</summary>
        public EventPriorityAttribute(Priority priority = Attributes.Priority.Normal)
        {
            Priority = (int)priority;
        }
        internal int Priority { get; private set; } = 0;
    }
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
    /// <summary>
    /// 事件优先等级默认枚举
    /// </summary>
    public enum Priority
    {
        Last = -0xfffffff,
        VeryLow = -0xffff,
        Low = -0xf,
        Normal = 0,
        High = 0xf,
        VeryHigh = 0xffff,
        First = 0xfffffff,
    }
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}

﻿using EOS.Tiles;
using System;
using System.Reflection;
namespace EOS.Attributes
{
    /// <summary>用于继承了<see cref="EventCodeAttribute"/>特性的类中的方法，注明该方法为该<see cref="EventCode"/>中的<see cref="EventCode.Method"/></summary>
    /// <remarks>
    /// 该方法不能是构造函数或者未定义所有类型的泛型方法。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class EventCodeMethodAttribute : Attribute
    {
    }
    /// <summary>
    /// 用于类或接口上，注明该类相当于声明了一个<see cref="EventCode"/>。
    /// </summary>
    /// <remarks>
    /// 该类内部需要同时存在继承了<see cref="EventCodeMethodAttribute"/>特性的方法。
    /// 完全声明后，相当于以该类的<see cref="Type.AssemblyQualifiedName"/>，
    /// 和继承了<see cref="EventCodeMethodAttribute"/>特性的方法的<see cref="MethodInfo"/>，
    /// 定义了一个<see cref="EventCode"/>类实例。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public sealed class EventCodeAttribute : Attribute
    {
    }
    /// <summary>注明该类不能作为<see cref="EventCodeAttribute"/>类型的继承。</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public sealed class NoEventCodeClassAttribute : Attribute
    {

    }
}

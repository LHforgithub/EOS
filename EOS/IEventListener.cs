﻿using EOS.Attributes;

namespace EOS
{
    /// <summary>继承该接口的类等效继承了<see cref="EventListenerAttribute"/>特性，并可用作<see cref="EOSControler.AddListener{T}(T)"/>中的泛型。</summary>
    [EventListener]
    public interface IEventListener
    {
    }
}

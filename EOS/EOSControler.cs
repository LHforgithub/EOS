using EOS.Attributes;
using EOS.Tiles;
using EOS.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;


namespace EOS
{
    /// <summary>事件控制器</summary>
    [NoEventCodeClass]
    public class EOSControler
    {
        /// <summary>此<see cref="EOSControler"/>隶属的程序集 </summary>
        public Assembly ControlAssembly { get; internal set; }

        /// <summary>同时归属此<see cref="EOSControler"/>控制的<see cref="EOSControler"/>。
        /// 那些<see cref="EOSControler"/>的<see cref="MergeControlers"/>属性也应该有此<see cref="EOSControler"/>的实例。</summary>
        internal List<EOSControler> MergeControlers { get; } = new();

        /// <summary>事件与对应的事件委托</summary>
        internal Dictionary<EventCode, EOSDelegate> EventDelegatesDic { get; } = new();

        /// <summary>已定义事件委托的类以及它们允许添加的事件Code和对应的方法信息。</summary>
        internal Dictionary<Type, Dictionary<EventCode, EOSMethodData>> ListenerTypeCodeMethods { get; } = new();
        /// <summary>当前广播的事件的参数集</summary>
        public EventParams NowBroadCastParams { get; private set; } = null;

        /// <summary>
        /// 定义一个新的事件
        /// </summary>
        /// <param name="eventCode">事件信息</param>
        /// <exception cref="ArgumentException"/>
        public void AddNewCode(EventCode eventCode)
        {
            if (string.IsNullOrEmpty(eventCode.Key))
            {
                if (eventCode.CodeType is not null)
                {
                    eventCode.Key = eventCode.CodeType.AssemblyQualifiedName;
                }
                else
                {
                    throw new ArgumentException($"{nameof(AddNewCode)} : Key and CodeType cannot all be null or empty.");
                }
            }
            var controler = this;
            //获取类型对应程序集的控制者
            if (eventCode.CodeType is not null)
            {
                controler = (ControlAssembly.GetSuccessfullyLoadedTypes().Contains(eventCode.CodeType) ? this
                : MergeControlers.FirstOrDefault(x => x.ControlAssembly.GetSuccessfullyLoadedTypes().Contains(eventCode.CodeType)))
                ?? throw new InvalidOperationException($"{nameof(AddNewCode)} : This CodeType : {eventCode.CodeType} should not be add to this EOSControler.");
            }
            //事件码方法信息构造检查
            if (eventCode.Method is null)
            {
                if (eventCode.ParametersType is null || eventCode.ReturnType is null)
                {
                    throw new InvalidOperationException($"{nameof(AddNewCode)} : EventCode : {eventCode.Key} has not define a method.");
                }
                //可定义则定义新的事件方法信息
                var dynamicMethod = new DynamicMethod(eventCode.Key + "_Method", eventCode.ReturnType, eventCode.ParametersType.ToArray());
                eventCode.Method = dynamicMethod;
                eventCode.Parameters = dynamicMethod.GetParameters().ToList();
            }
            else
            {
                if (eventCode.Method.IsConstructorOrGeneric())
                {
                    throw new InvalidOperationException($"{nameof(AddNewCode)} : EventCode : {eventCode.Key} method cannot be a constructor or generic.");
                }
                eventCode.Parameters = eventCode.Method.GetParameters().ToList();
                eventCode.ReturnType = eventCode.Method.ReturnType;
            }
            var eosDelegate = new EOSDelegate
            {
                Controler = this,
                Code = eventCode
            };
            controler.EventDelegatesDic.AddOrUpdata(eventCode, eosDelegate);
        }
        /// <inheritdoc cref="AddNewCode(Type)"/>
        /// <typeparam name="T">泛型必须为继承了<see cref="IEventCode"/>接口的类型。</typeparam>
        public void AddNewCode<T>() where T : IEventCode
        {
            AddNewCode(typeof(T));
        }
        /// <inheritdoc cref="AddNewCode(EventCode)"/>
        /// <param name="type">必须为继承了<see cref="EventCodeAttribute"/>特性的类型。</param>
        /// <remarks>
        /// 使用的类型中，必须同时包含至少一个继承了<see cref="EventCodeMethodAttribute"/>特性的方法。
        /// 当有多个继承的方法时，使用找到的第一个方法作为该事件码的方法信息。
        /// </remarks>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidOperationException"/>
        public void AddNewCode(Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            if (type.GetCustomAttribute<EventCodeAttribute>(true, true) is null)
            {
                throw new InvalidOperationException($"{nameof(AddNewCode)} : Type : {type} has not use an EventCodeAttribute.");
            }
            var methodInfo = type.GetMethods(ReflectionTools.AllNoGetSet).FirstOrDefault(m => m.GetCustomAttribute<EventCodeMethodAttribute>(true) is not null)
                ?? throw new InvalidOperationException($"{nameof(AddNewCode)} : Cannot find a method define by attribute EventCodeMethod. Type : {type}");
            //检查是否为构造函数或泛型类型未完全定义的泛型方法
            if (methodInfo.IsConstructor)
            {
                throw new InvalidOperationException($"{nameof(AddNewCode)} : Cannot use a constructor method to be an EventMethod. Type : {type}, Method : {methodInfo.Name}.");
            }
            if (methodInfo.IsGenericMethod && methodInfo.ContainsGenericParameters)
            {
                throw new InvalidOperationException($"{nameof(AddNewCode)} : Cannot use a generic method to be an EventMethod. Type : {type}, Method : {methodInfo.Name}.");
            }
            var eventCode = new EventCode()
            {
                CodeType = type,
                Key = type.AssemblyQualifiedName,
                Method = methodInfo
            };
            AddNewCode(eventCode);
        }





        /// <summary>用于在运行时动态地添加新的事件的接收方法。</summary>
        /// <param name="type">必须是继承了<see cref="EventListenerAttribute"/>特性的类型。</param>
        /// <param name="eventCode">一个运行时动态创建的<see cref="EventCode"/>实例。</param>
        /// <param name="newMethod">尝试为<paramref name="eventCode"/>添加监听的新方法，必须隶属于<paramref name="type"/>。</param>
        /// <param name="priorityNum">该<paramref name="type"/>在此事件中的优先级。</param>
        /// <remarks>
        /// 如果是由<see cref="EventCodeAttribute"/>在编译时已定义的事件，不应该使用此方法。
        /// <para>参数<paramref name="eventCode"/>应该是运行时创建的，自定义<see cref="EventCode.Key"/>值的实例。
        /// 事件码约定的方法则由输入的<see cref="EventCode.Parameters"/>和<see cref="EventCode.ReturnType"/>决定。</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidOperationException"/>
        public void AddNewTypeCodeData(Type type, EventCode eventCode, MethodInfo newMethod = null, int priorityNum = 0)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = eventCode ?? throw new ArgumentNullException(nameof(eventCode));
            _ = type.GetCustomAttribute<EventListenerAttribute>(true, true) ??
                throw new InvalidOperationException($"{nameof(AddNewTypeCodeData)} : Type : {type} has no EventListener attribute, cannot add new EventMethod.");
            EOSMethodData methodData = null;
            if (newMethod is not null)
            {
                if (newMethod.ReflectedType.AssemblyQualifiedName != type.AssemblyQualifiedName)
                {
                    TempLog.Log($"{nameof(AddNewTypeCodeData)} : Input {nameof(newMethod)} {newMethod} is not declared by input Type : {type}.");
                }
                else
                {
                    if (newMethod.IsParamsAndReturnEquelsWith(eventCode.Method))
                    {
                        methodData = new EOSMethodData(
                            methodInfo: newMethod,
                            priority: priorityNum
                            );
                    }
                    else
                    {
                        TempLog.Log($"{nameof(AddNewTypeCodeData)} : Input {nameof(newMethod)} {newMethod} is not equel with EventCode method.");
                    }
                }
            }
            if (methodData is null)
            {
                var methods = type.GetNotGetSetMethods();
                foreach (var method in methods)
                {
                    if (!method.IsParamsAndReturnEquelsWith(eventCode.Method))
                    {
                        continue;
                    }
                    methodData = new EOSMethodData(
                        methodInfo: method,
                        priority: priorityNum
                        );
                }
            }
            if (methodData is null)
            {
                throw new InvalidOperationException($"{nameof(AddNewTypeCodeData)} : Create methodData faild.");
            }
            if (GetListenerTypeCodeMethods().TryGetValue(type, out var dic))
            {
                dic.AddOrUpdata(eventCode, methodData);
            }
            else
            {
                ListenerTypeCodeMethods.AddOrUpdata(type, new Dictionary<EventCode, EOSMethodData>() { { eventCode, methodData } });
            }
        }


        /// <summary>将另一个<see cref="EOSControler"/>附加到此<see cref="EOSControler"/>。会同时让那个<see cref="EOSControler"/>附加此<see cref="EOSControler"/></summary>
        /// <param name="eosControler">要合并的<see cref="EOSControler"/></param>
        /// <exception cref="ArgumentNullException"/>
        public void Merge(EOSControler eosControler)
        {
            if(IsDestroy) return;
            _ = eosControler ?? throw new ArgumentNullException(nameof(eosControler));
            if (MergeControlers.TryAddWithOutMultiple(eosControler))
            {
                eosControler.Merge(this);
            }
        }
        /// <summary>
        /// 将另一个<see cref="EOSControler"/>与此<see cref="EOSControler"/>分离。会同时让那个<see cref="EOSControler"/>分离此<see cref="EOSControler"/>。
        /// </summary>
        /// <param name="eosControler">要分离的<see cref="EOSControler"/></param>
        /// <exception cref="ArgumentNullException"/>
        public void Split(EOSControler eosControler)
        {
            _ = eosControler ?? throw new ArgumentNullException(nameof(eosControler));
            if (MergeControlers.Remove(eosControler))
            {
                eosControler.Split(this);
            }
        }
        /// <summary>
        /// 将另一个<see cref="EOSControler"/>与此<see cref="EOSControler"/>分离。会同时让那个<see cref="EOSControler"/>分离此<see cref="EOSControler"/>。
        /// </summary>
        /// <param name="controlAssembly">要分离的<see cref="EOSControler"/>所控制的对应的程序集</param>
        /// <exception cref="ArgumentNullException"/>
        public void Split(Assembly controlAssembly)
        {
            _ = controlAssembly ?? throw new ArgumentNullException(nameof(controlAssembly));
            var eosControler = MergeControlers.FirstOrDefault(x => x.ControlAssembly.FullName == controlAssembly.FullName);
            if (eosControler is not null)
            {
                MergeControlers.Remove(eosControler);
                eosControler.Split(this);
            }
        }

        /// <summary>
        /// 添加事件接收对象实例，或静态类中的方法。
        /// 对于相同的实例对象，不会重复添加。
        /// </summary>
        /// <param name="type">类型必须是继承了<see cref="EventListenerAttribute"/>特性的类型。</param>
        /// <param name="instance">对象实例。如果为<see langword="null"/>，则类型必须是静态类型。</param>
        /// <remarks>如果无法添加方法，此方法会抛出异常，可能中断程序。</remarks>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidOperationException"/>
        public void AddListener(Type type, object instance = null)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = type.GetCustomAttribute<EventListenerAttribute>(true, true) ??
                throw new InvalidOperationException($"{nameof(AddListener)} : Type : {type} has no EventListener attribute, cannot use as an Event Listener.");
            var codeMethodDic = GetTypeEOSMethods(type);
            if (codeMethodDic.Count < 1)
            {
                throw new InvalidOperationException($"{nameof(AddListener)} : Cannot use {(instance is null ? "Null" : instance)} with Type : {type} as an Event Listener. No method can be use as a delegate. Please cheak your class's EventListener attribute.");
            }
            var eosDelegateDic = GetEOSDelegates();
            foreach (var code in codeMethodDic.Keys)
            {
                if (!eosDelegateDic.TryGetValue(code, out var eosDelegate))
                {
                    AddNewCode(code);
                    eosDelegate = GetEOSDelegate(code);
                }
                eosDelegate.Add(instance, codeMethodDic[code]);
            }
        }
        /// <inheritdoc cref="AddListener(Type, object)"/>
        /// <param name="notNullInstance">对象实例，不能为空。</param>
        public void AddListener(object notNullInstance)
        {
            AddListener(notNullInstance?.GetType(), notNullInstance);
        }

        /// <summary>移除事件的接收者，或静态类中的方法。</summary>
        /// <param name="type">类型必须是继承了<see cref="EventListenerAttribute"/>特性的类型，否则会抛出<see cref="InvalidOperationException"/>异常。</param>
        /// <param name="instance">对象实例。如果为<see langword="null"/>，则类型必须是静态类型。</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidOperationException"/>
        public void RemoveListener(Type type, object instance = null)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = type.GetCustomAttribute<EventListenerAttribute>(true, true) ??
                throw new InvalidOperationException($"{nameof(RemoveListener)} : Type : {type} has no EventListener attribute, cannot use as an Event Listener.");
            var codeMethodDic = GetTypeEOSMethods(type);
            var delegateDic = GetEOSDelegates();
            foreach (var code in codeMethodDic.Keys)
            {
                if (!delegateDic.TryGetValue(code, out var eosDelegate))
                {
                    continue;
                }
                eosDelegate.Remove(instance, codeMethodDic[code]);
            }
        }

        /// <inheritdoc cref=" RemoveListener(Type, object)"/>
        /// <param name="notNullInstance">对象实例，不能为空。</param>
        public void RemoveListener(object notNullInstance)
        {
            RemoveListener(notNullInstance?.GetType(), notNullInstance);
        }

        /// <summary>
        /// 清空对应事件的事件码
        /// </summary>
        /// <param name="key">要清空事件的事件码的<see cref="EventCode.Key"/>值</param>
        public void ClearListener(string key)
        {
            if (!string.IsNullOrEmpty(key) && GetEOSDelegate(key) is EOSDelegate del)
            {
                del.Clear();
            }
        }
        /// <inheritdoc cref=" ClearListener(string)"/>
        /// <typeparam name="T">必须是继承<see cref="IEventCode"/>接口的类型。</typeparam>
        public void ClearListener<T>() where T : IEventCode
        {
            ClearListener(typeof(T).AssemblyQualifiedName);
        }
        /// <inheritdoc cref=" ClearListener(string)"/>
        /// <param name="type">必须是继承<see cref="EventCodeAttribute"/>特性的类型。</param>
        public void ClearListener(Type type)
        {
            ClearListener(type.AssemblyQualifiedName);
        }
        /// <inheritdoc cref=" ClearListener(string)"/>
        /// <param name="eventCode">要清空事件的事件码。只要<see cref="EventCode.Key"/>值不为空即可。</param>
        public void ClearListener(EventCode eventCode)
        {
            ClearListener(eventCode.Key);
        }

        /// <summary>
        /// 设置事件监听器实例的方法所在层级优先级属性。
        /// </summary>
        /// <param name="type">类型必须是继承了<see cref="EventListenerAttribute"/>特性的类型，否则会抛出<see cref="InvalidOperationException"/>异常。</param>
        /// <param name="instance">对象实例。如果为<see langword="null"/>，则类型必须是静态类型。</param>
        /// <param name="layerProperty">要设置的层级优先级属性值。值越小，优先级越小，越后被调用。</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidOperationException"/>
        public void SetListenerLayerProperty(int layerProperty, Type type, object instance = null)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = type.GetCustomAttribute<EventListenerAttribute>(true, true) ??
                throw new InvalidOperationException($"{nameof(RemoveListener)} : Type : {type} has no EventListener attribute, cannot use as an Event Listener.");
            var codeMethodDic = GetTypeEOSMethods(type);
            var delegateDic = GetEOSDelegates();
            foreach (var code in codeMethodDic.Keys)
            {
                if (!delegateDic.TryGetValue(code, out var eosDelegate))
                {
                    continue;
                }
                var method = eosDelegate.GetInstanceMethod(instance);
                if (method != null)
                {
                    method.LayerPriority = layerProperty;
                }
            }
        }
        /// <inheritdoc cref="SetListenerLayerProperty(int, Type, object)"/>
        public void SetListenerLayerProperty(object notNullInstance, int layerProperty)
        {
            SetListenerLayerProperty(layerProperty, notNullInstance?.GetType(), notNullInstance);
        }

        /// <summary> 广播事件。 </summary>
        /// <param name="key">必须为非空字符串，否则会抛出<see cref="ArgumentException"/>异常。</param>
        /// <param name="values">
        /// 事件对应要输入的参数。参数<see cref="Type"/>和位置必须与该<see cref="EventCode"/>定义的方法对应。
        /// <para>数量可以超出，但不应少于该方法中无默认值的参数数量。</para>
        /// <para>超出部分的参数将被忽略，有默认值且<paramref name="values"/>未提供对应值的参数将使用<see cref="EventCode"/>定义时的默认值。</para>
        /// <para>对使用了<see langword="ref"/>关键字的参数，请在输入<paramref name="values"/>前将<paramref name="values"/>
        /// 存储为一个您自己的<see langword="object[]"/>数组并保留引用，在广播结束后，
        /// 该数组对应<see langword="ref"/>关键字参数位置的索引的元素的值将被修改。</para>
        /// <para>对使用了<see langword="out"/>关键字的参数，参考<see langword="ref"/>关键字，
        /// 不同的是<see langword="out"/>关键字参数位置的元素，在输入时可以为<see langword="null"/>，它将在函数中被定义。</para>
        /// <para>使用了<see langword="params"/>关键字的参数，请将<paramref name="values"/>中最后一个参数替换为<see langword="object[]"/>数组。</para>
        /// </param>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="NullReferenceException"/>
        public void BroadCast(string key, params object[] values)
        {
            var eosDelegate = GetEOSDelegate(key);
            //声明新一层事件参数
            var eventParams = new EventParams(this, eosDelegate.Code, NowBroadCastParams, NowBroadCastParams?.BroadCastLevel ?? 0 + 1, values);
            NowBroadCastParams = eventParams;
            try
            {
                //调用
                eosDelegate.Invoke(values);
            }
            catch (Exception ex)
            {
                TempLog.Log($"{nameof(BroadCast)} : {ex.GetBaseException()}");
            }
            finally
            {
                //回到上一层事件参数
                NowBroadCastParams = eventParams?.Last;
            }
        }

        /// <inheritdoc cref="BroadCast(string, object[])"/>
        /// <param name="type">必须是继承了<see cref="EventCodeAttribute"/>特性的类，否则会抛出<see cref="ArgumentException"/>异常。</param>
        /// <exception cref= "ArgumentNullException"/>
        /// <exception cref= "ArgumentException"/>
        public void BroadCast(Type type, params object[] values)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = type.GetCustomAttribute<EventCodeAttribute>(true, true) ?? throw new ArgumentException();
            BroadCast(type.AssemblyQualifiedName, values);
        }
        /// <inheritdoc cref="BroadCast(Type, object[])"/>
        /// <typeparam name="T">泛型必须是带有<see cref="IEventCode"/>接口的类，以确保可为<see cref="EventCode"/>。</typeparam>
        public void BroadCast<T>(params object[] values) where T : IEventCode
        {
            BroadCast(typeof(T).AssemblyQualifiedName, values);
        }

        /// <summary>
        /// 清空控制器中所有事件码已添加的接收者。
        /// </summary>
        public void Clear()
        {
            foreach (var eosDeleget in GetEOSDelegates().Values)
            {
                eosDeleget.Clear();
            }
        }

        /// <summary>指示控制器是否已销毁。</summary>
        public bool IsDestroy { get; private set; } = false;
        /// <summary>等待广播完毕后销毁控制器。</summary>
        public void Destroy()
        {
            if (!IsDestroy)
            {
                IsDestroy = true;
                Task.Run(() =>
                {
                    while ((NowBroadCastParams?.BroadCastLevel ?? 0) > 0)
                    {
                        Task.Delay(15).Wait();
                    }
                    foreach (var controler in new List<EOSControler>(MergeControlers))
                    {
                        controler.Split(this);
                    }
                    MergeControlers.Clear();
                    foreach (var eosDeleget in new List<EOSDelegate>(EventDelegatesDic.Values))
                    {
                        eosDeleget.Clear();
                    }
                    ListenerTypeCodeMethods.Clear();
                    EOSManager.Instance.AssemblyControlerDic.Remove(ControlAssembly);
                    ControlAssembly = null;
                });
            }
        }




        /// <summary>
        /// 获取所有EventListener对应的许可事件码时使用。
        /// </summary>
        /// <returns></returns>
        internal Dictionary<Type, Dictionary<EventCode, EOSMethodData>> GetListenerTypeCodeMethods()
        {
            var dic = new Dictionary<Type, Dictionary<EventCode, EOSMethodData>>(ListenerTypeCodeMethods);
            foreach (var item in new List<EOSControler>(MergeControlers))
            {
                foreach (var dic2 in new Dictionary<Type, Dictionary<EventCode, EOSMethodData>>(item.ListenerTypeCodeMethods))
                {
                    if (dic.TryGetValue(dic2.Key, out var value))
                    {
                        dic[dic2.Key] = dic2.Value.Merge(value).ToDictionary();
                    }
                    else
                    {
                        dic[dic2.Key] = dic2.Value;
                    }
                }
            }
            return dic;
        }
        /// <summary>
        /// 获取事件时，需要用此方法获取，以获得所有合并的控制器。
        /// </summary>
        /// <returns></returns>
        internal Dictionary<EventCode, EOSDelegate> GetEOSDelegates()
        {
            var dic = new Dictionary<EventCode, EOSDelegate>(EventDelegatesDic);
            foreach (var item in new List<EOSControler>(MergeControlers))
            {
                dic.Merge(item.EventDelegatesDic);
            }
            return dic;
        }
        internal EOSDelegate GetEOSDelegate(EventCode eventCode)
        {
            return GetEOSDelegate(eventCode.Key);
        }
        internal EOSDelegate GetEOSDelegate(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            var dic = GetEOSDelegates();
            var code = dic.Keys.FirstOrDefault(x => x.Key == key)
                ?? throw new InvalidOperationException($"Cannot find EventCode with Key : {key}.");
            var eosDelegate = dic[code];
            return eosDelegate;
        }
        /// <summary>
        /// 获取类型对应的许可事件码时使用
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal Dictionary<EventCode, EOSMethodData> GetTypeEOSMethods(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (GetListenerTypeCodeMethods().TryGetValue(type, out var value))
            {
                return value;
            }
            return new Dictionary<EventCode, EOSMethodData>();
        }

        /// <summary>加载对应类型的事件码对应的方法。</summary>
        internal void SearchTypeCodeDataDic(Type type)
        {
            var codeDataDic = new Dictionary<EventCode, EOSMethodData>();
            var methodInfos = type.GetNotGetSetMethods();
            foreach (var method in methodInfos)
            {
                if (method.IsConstructorOrGeneric())
                {
                    continue;
                }
                if (method.GetCustomAttribute<EventListenerAttribute>(true, true) is EventListenerAttribute eventListener)
                {
                    if (eventListener.CodeType is null ||
                        eventListener.CodeType.GetCustomAttribute<EventCodeAttribute>(true, true) is not EventCodeAttribute eventCodeAttribute)
                    {
                        TempLog.Log($"{nameof(SearchTypeCodeDataDic)} : [Type : <{type}>] has a method that define a no EventCode EventListenerAttribute, Method Name : {method.Name}");
                        continue;
                    }
                    var code = TryGetEventCode(eventListener.CodeType);
                    if (code is null)
                    {
                        //对未加载的类型，尝试加载并自动合并。
                        var newControler = EOSManager.GetNewControler(eventListener.CodeType.Assembly, new List<EOSControler>() { this });
                        code = TryGetEventCode(eventListener.CodeType);
                        if (code is null)
                        {
                            TempLog.Log($"{nameof(SearchTypeCodeDataDic)} : [Type : <{type}>] with [Method : <{method.Name}>] use an undefined [EventCode : <{eventListener.CodeType}>].");
                            continue;
                        }
                    }
                    if (codeDataDic.ContainsKey(code))
                    {
                        TempLog.Log($"{nameof(SearchTypeCodeDataDic)} : [Type : <{type}>] with [Method : <{method.Name}>] use multiple [EventCode : <{eventListener.CodeType}>]. " +
                            $"There's already a method use this Code!");
                        continue;
                    }
                    if (!method.IsParamsAndReturnEquelsWith(code.Method))
                    {
                        TempLog.Log($"{nameof(SearchTypeCodeDataDic)} : [Type : <{type}>] with [Method : <{method.Name}>] has different parameters or returnType with [EventCode : <{eventListener.CodeType}>]. " +
                           $"Please maintain consistency!");
                        continue;
                    }
                    var methodData = new EOSMethodData(
                        methodInfo: method,
                        priority: method.GetCustomAttribute<EventPriorityAttribute>(true, true)?.Priority ?? (int)Priority.Normal
                        );
                    codeDataDic.Add(code, methodData);
                }
            }
            if (codeDataDic.Count > 0)
            {
                ListenerTypeCodeMethods.AddOrUpdata(type, codeDataDic);
            }
            //获取此类型中的嵌套类型并将其中的事件加入控制集
            var nestedTypes = type.GetNestedTypes(true);
            foreach (var nestedType in nestedTypes)
            {
                SearchTypeCodeDataDic(nestedType);
            }
        }



        /// <inheritdoc cref="TryGetEventCode(string)"/>
        /// <param name="type">继承了<see cref="EventCodeAttribute"/>特性的类型。</param>
        public EventCode TryGetEventCode(Type type)
        {
            return type?.GetCustomAttribute<EventCodeAttribute>(true, true) is null ? null : TryGetEventCode(type.AssemblyQualifiedName);
        }
        /// <summary>返回此<see cref="EOSControler"/>中可以找到的对应的<see cref="EventCode"/>实例。</summary>
        /// <param name="key"></param>
        /// <returns>查询到的<see cref="EventCode"/>实例。如果未找到或输入为空，返回<see langword="null"/>。</returns>
        public EventCode TryGetEventCode(string key)
        {
            return string.IsNullOrEmpty(key) ? null : GetEOSDelegates().Keys.FirstOrDefault(x => x.Key == key);
        }

    }
}

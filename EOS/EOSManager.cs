using EOS.Attributes;
using EOS.Tiles;
using EOS.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace EOS
{

    #region
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
    public abstract class Singleton<T> : object where T : Singleton<T>, new()
    {
        public Singleton()
        {
        }
        public static T Instance  => Nested.instance;
        public static bool IsInitialized => Nested.instance != null;
        class Nested
        {
            static Nested()
            {
            }
            internal static readonly T instance = new();
        }
    }
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
    #endregion


    /// <summary>
    /// 
    /// </summary>
    public class EOSManager : Singleton<EOSManager>
    {
        /// <summary></summary>
        public EOSControler SingleControler { get; internal set; }
        /// <summary></summary>
        public Dictionary<Assembly, EOSControler> AssemblyControlerDic { get; internal set; } = new();
        /// <summary>
        /// 当前单例控制器的当前触发事件的事件信息
        /// </summary>
        public static EventParams NowBroadCastParams => Instance.SingleControler?.NowBroadCastParams;

        /// <inheritdoc cref="MergeToSingleton(Assembly)"/>
        /// <remarks>无参数时使用调用该函数的域所处程序集。</remarks>
        public static void MergeToSingleton()
        {
            Assembly assembly = new StackTrace().GetFrame(1).GetMethod().ReflectedType.Assembly;
            MergeToSingleton(assembly);
        }
        /// <summary>
        /// 将该程序集对应的控制器添加至单例控制器。
        /// </summary>
        /// <param name="assembly">要添加的程序集</param>
        public static void MergeToSingleton(Assembly assembly)
        {
            if (Instance.SingleControler is null)
            {
                Instance.SingleControler = GetNewControler(assembly);
            }
            else
            {
                GetNewControler(assembly, new List<EOSControler>() { Instance.SingleControler });
            }
        }

        /// <inheritdoc cref="SpliteFromSingleton(Assembly)"/>
        /// <remarks>无参数时使用调用该函数的域所处程序集。</remarks>
        public static void SpliteFromSingleton()
        {
            Assembly assembly = new StackTrace().GetFrame(1).GetMethod().ReflectedType.Assembly;
            SpliteFromSingleton(assembly);
        }
        /// <summary>
        /// 将该程序集对应的控制器从单例控制器中分离。
        /// </summary>
        /// <param name="assembly">对应的程序集</param>
        public static void SpliteFromSingleton(Assembly assembly)
        {
            if (Instance.SingleControler is null)
            {
                return;
            }
            if (Instance.SingleControler.ControlAssembly == assembly)
            {
                var newControl = Instance.SingleControler.MergeControlers.FirstOrDefault(x => x.ControlAssembly != assembly);
                Instance.SingleControler.Destroy();
                Instance.AssemblyControlerDic.Remove(assembly);
                Instance.SingleControler = newControl;
                return;
            }
            Instance.SingleControler.Split(assembly);
        }
        /// <summary>
        /// 获取对应程序集的事件控制器。
        /// 会自动注册该程序集中所有由特性<see cref="EventCodeAttribute"/>标注的<see cref="EventCode"/>。
        /// 会自动解析所有由特性<see cref="EventListenerAttribute"/>标注的类和方法是否可以作为对应的事件接收者。
        /// </summary>
        /// <param name="assembly">要解析的程序集</param>
        /// <param name="mergeConrolers">要同步合并的程序集</param>
        /// <returns>返回解析后的控制器。如果已解析过，返回之前解析的控制器实例。</returns>
        public static EOSControler GetNewControler(Assembly assembly, IEnumerable<EOSControler> mergeConrolers = null)
        {
            if (Instance.AssemblyControlerDic.TryGetValue(assembly, out var controler) && !controler.IsDestroy)
            {
                if (mergeConrolers is not null)
                {
                    foreach (var c in mergeConrolers)
                    {
                        c.Merge(controler);
                    }
                }
                return controler;
            }
            var eosControler = new EOSControler();
            eosControler.ControlAssembly = assembly;
            Instance.AssemblyControlerDic.AddOrUpdata(assembly, eosControler);
            if (mergeConrolers is not null)
            {
                foreach (var c in mergeConrolers)
                {
                    c.Merge(eosControler);
                }
            }
            //所有类型获取
            var AllTypes = assembly.GetSuccessfullyLoadedTypes();
            //所有事件码获取
            var eventCodeList = AllTypes.Where(t => IsCanUseAsEventCode(t));
            foreach (var type in eventCodeList)
            {
                try
                {
                    eosControler.AddNewCode(type);
                }
                catch (Exception ex)
                {
                    TempLog.Log(ex.ToString());
                    continue;
                }
            }
            //所有许可类型获取
            var listenerTypeList = AllTypes.Where(t => IsListener(t));
            foreach (var type in listenerTypeList)
            {
                SearchTypeCodeDataDic(eosControler, type);
            }
            return eosControler;
        }
        /// <inheritdoc cref="GetNewControler(Assembly, IEnumerable{EOSControler})"/>
        public static EOSControler GetNewControler(IEnumerable<EOSControler> mergeConrolers = null)
        {
            Assembly assembly = new StackTrace().GetFrame(1).GetMethod().ReflectedType.Assembly;
            return GetNewControler(assembly, mergeConrolers);
        }

        /// <summary>加载对应类型的事件码对应的方法。</summary>
        internal static void SearchTypeCodeDataDic(EOSControler eosControler, Type type)
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
                    var code = eosControler.TryGetEventCode(eventListener.CodeType);
                    if (code is null)
                    {
                        //对未加载的类型，如果该程序集已和单例控制器合并，则尝试加载并自动合并。
                        if (eosControler.MergeControlers.Any(x => x == Instance.SingleControler))
                        {
                            var newControler = GetNewControler(eventListener.CodeType.Assembly, new List<EOSControler>() { eosControler });
                            code = eosControler.TryGetEventCode(eventListener.CodeType);
                            if (code is null)
                            {
                                TempLog.Log($"{nameof(SearchTypeCodeDataDic)} : [Type : <{type}>] with [Method : <{method.Name}>] use an undefined [EventCode : <{eventListener.CodeType}>].");
                                continue;
                            }
                        }
                        //否则，未加载类型无法使用，记录日志并跳过。
                        TempLog.Log($"{nameof(SearchTypeCodeDataDic)} : [Type : <{type}>] with [Method : <{method.Name}>] use an undefined [EventCode : <{eventListener.CodeType}>].");
                        continue;
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
                eosControler.ListenerTypeCodeMethods.AddOrUpdata(type, codeDataDic);
            }
            //获取此类型中的嵌套类型并将其中的事件加入控制集
            var nestedTypes = type.GetNestedTypes(true);
            foreach (var nestedType in nestedTypes)
            {
                SearchTypeCodeDataDic(eosControler, nestedType);
            }
        }

        /// <summary>
        /// 移除已解析的程序集控制器。
        /// </summary>
        /// <param name="assembly"></param>
        public static void RemoveControler(Assembly assembly)
        {
            if (Instance.AssemblyControlerDic.TryGetValue(assembly, out var eosControler))
            {
                eosControler.Destroy();
                Instance.AssemblyControlerDic.Remove(assembly);
            }
        }
        /// <inheritdoc cref="RemoveControler(Assembly)"/>
        public static void RemoveControler()
        {
            Assembly assembly = new StackTrace().GetFrame(1).GetMethod().ReflectedType.Assembly;
            RemoveControler(assembly);
        }



        /// <inheritdoc cref="EOSControler.AddListener(Type, object)" path="member/summary"/>
        /// <inheritdoc cref="EOSControler.AddListener(Type, object)" path="member/param"/>
        /// <remarks>
        /// 此方法向<see cref="EOSManager"/>中的<see cref="SingleControler"/>添加对象。
        /// <para>此静态方法不会直接抛出异常中断，而是将出现的异常记录至<see cref="TempLog"/>中。</para>
        /// </remarks>
        public static void AddListener(Type type, object instance = null)
        {
            try
            {
                Instance.SingleControler.AddListener(type, instance);
            }
            catch (Exception ex)
            {
                TempLog.Log(ex.ToString());
            }
        }
        /// <inheritdoc cref="EOSControler.AddListener(object)" path="member/summary"/>
        /// <inheritdoc cref="EOSControler.AddListener(object)" path="member/param"/>
        /// <inheritdoc cref="AddListener(Type, object)" path="member/remarks"/>
        public static void AddListener(object notNullInstance)
        {
            AddListener(notNullInstance?.GetType(), notNullInstance);
        }

        /// <inheritdoc cref="EOSControler.AddListener{T}" path="member/summary"/>
        /// <inheritdoc cref="EOSControler.AddListener{T}" path="member/typeparam"/>
        /// <inheritdoc cref="EOSControler.AddListener{T}" path="member/param"/>
        /// <remarks>
        /// <inheritdoc cref="EOSControler.AddListener{T}" path="member/remarks"/>
        /// <para><inheritdoc cref="AddListener(Type, object)" path="member/remarks"/></para>
        /// </remarks>
        public static void AddListener<T>(object notNullInstance, string methodName = "", Type[] parameterTypes = null)
        {
            try
            {
                Instance.SingleControler.AddListener<T>(notNullInstance, methodName, parameterTypes);
            }
            catch (Exception ex)
            {
                TempLog.Log(ex.ToString());
            }
        }

        /// <inheritdoc cref="EOSControler.RemoveListener(Type, object)" path="member/summary"/>
        /// <inheritdoc cref="EOSControler.RemoveListener(Type, object)" path="member/param"/>
        /// <remarks>
        /// 此方法从<see cref="EOSManager"/>中的<see cref="SingleControler"/>移除对象。
        /// <para>此静态方法不会直接抛出异常中断，而是将出现的异常记录至<see cref="TempLog"/>中。</para>
        /// </remarks>
        public static void RemoveListener(Type type, object instance = null)
        {
            try
            {
                Instance.SingleControler.RemoveListener(type, instance);
            }
            catch (Exception ex)
            {
                TempLog.Log(ex.ToString());
            }
        }
        /// <inheritdoc cref="EOSControler.RemoveListener(object)" path="member/summary"/>
        /// <inheritdoc cref="EOSControler.RemoveListener(object)" path="member/param"/>
        /// <inheritdoc cref="RemoveListener(Type, object)" path="member/remarks"/>
        public static void RemoveListener(object notNullInstance)
        {
            RemoveListener(notNullInstance?.GetType(), notNullInstance);
        }

        /// <inheritdoc cref="EOSControler.RemoveListener{T}" path="member/summary"/>
        /// <inheritdoc cref="EOSControler.RemoveListener{T}" path="member/typeparam"/>
        /// <inheritdoc cref="EOSControler.RemoveListener{T}" path="member/param"/>
        /// <remarks>
        /// <inheritdoc cref="EOSControler.RemoveListener{T}" path="member/remarks"/>
        /// <para><inheritdoc cref="RemoveListener(Type, object)" path="member/remarks"/></para>
        /// </remarks>
        public static void RemoveListener<T>(object notNullInstance)
        {
            try
            {
                Instance.SingleControler.RemoveListener<T>(notNullInstance);
            }
            catch (Exception ex)
            {
                TempLog.Log(ex.ToString());
            }
        }

        /// <inheritdoc cref="EOSControler.ClearListener(string)"/>
        /// <remarks>此方法从<see cref="EOSManager"/>中的<see cref="SingleControler"/>移除对象。</remarks>
        public static void ClearListener(string key)
        {
            try
            {
                Instance.SingleControler.ClearListener(key);
            }
            catch (Exception ex)
            {
                TempLog.Log(ex.ToString());
            }
        }
        /// <inheritdoc cref="EOSControler.ClearListener{T}()"/>
        public static void ClearListener<T>() where T : IEventCode
        {
            ClearListener(typeof(T).AssemblyQualifiedName);
        }
        /// <inheritdoc cref="EOSControler.ClearListener(Type)"/>
        public static void ClearListener(Type type)
        {
            ClearListener(type.AssemblyQualifiedName);
        }
        /// <inheritdoc cref="EOSControler.ClearListener(EventCode)"/>
        public static void ClearListener(EventCode eventCode)
        {
            ClearListener(eventCode.Key);
        }


        /// <inheritdoc cref="EOSControler.SetListenerLayerProperty(int, Type, object)"/>
        /// <remarks>此方法在<see cref="EOSManager"/>中的<see cref="SingleControler"/>中设置层级优先级。</remarks>
        public void SetListenerLayerProperty(int layerProperty, Type type, object instance = null)
        {
            try
            {
                Instance.SingleControler.SetListenerLayerProperty(layerProperty, type, instance);
            }
            catch (Exception ex)
            {
                TempLog.Log(ex.ToString());
            }
        }
        /// <inheritdoc cref="SetListenerLayerProperty(int, Type, object)"/>
        public void SetListenerLayerProperty(object notNullInstance, int layerProperty)
        {
            SetListenerLayerProperty(layerProperty, notNullInstance?.GetType(), notNullInstance);
        }


        /// <inheritdoc cref="EOSControler.BroadCast(string, object[])" path="member/summary"/>
        /// <inheritdoc cref="EOSControler.BroadCast(string, object[])" path="member/param"/>
        /// <remarks>
        /// 此方法从<see cref="EOSManager"/>中的<see cref="SingleControler"/>广播事件。
        /// <para>此静态方法不会抛出异常。</para>
        /// </remarks>
        public static void BroadCast(string key, params object[] values)
        {
            try
            {
                Instance.SingleControler.BroadCast(key, values);
            }
            catch (Exception ex)
            {
                TempLog.Log(ex.ToString());
            }
        }
        /// <inheritdoc cref="EOSControler.BroadCast(Type, object[])" path="member/summary"/>
        /// <inheritdoc cref="EOSControler.BroadCast(Type, object[])" path="member/param"/>
        /// <remarks>
        /// 此方法从<see cref="EOSManager"/>中的<see cref="SingleControler"/>广播事件。
        /// <para>此静态方法不会抛出异常。</para>
        /// </remarks>
        public static void BroadCast(Type type, params object[] values)
        {
            try
            {
                Instance.SingleControler.BroadCast(type, values);
            }
            catch (Exception ex)
            {
                TempLog.Log(ex.ToString());
            }
        }
        /// <inheritdoc cref="EOSControler.BroadCast{T}(object[])" path="member/summary"/>
        /// <inheritdoc cref="EOSControler.BroadCast{T}(object[])" path="member/param"/>
        /// <inheritdoc cref="EOSControler.BroadCast{T}(object[])" path="member/typeparam"/>
        /// <remarks>
        /// 此方法从<see cref="EOSManager"/>中的<see cref="SingleControler"/>广播事件。
        /// <para>此静态方法不会抛出异常。</para>
        /// </remarks>
        public static void BroadCast<T>(params object[] values)
        {
            BroadCast(typeof(T).AssemblyQualifiedName, values);
        }






        ///// <inheritdoc cref="EOSControler.TryGetEventCode(Type)"/>
        ///// <remarks>
        ///// 此函数搜索所有已加载程序集中的<see cref="EventCode"/>实例。
        ///// </remarks>
        //public static EventCode TryGetDefinedEventCode(Type type)
        //{
        //    var dic = new List<EOSControler>(Instance.AssemblyControlerDic.Values);
        //    foreach (var item in dic)
        //    {
        //        var result = item.TryGetEventCode(type);
        //        if (result is not null)
        //        {
        //            return result;
        //        }
        //    }
        //    return null;
        //}



        /// <summary>检查类型是否可以被视为定义了一个<see cref="EventCode"/>实例。</summary>
        public static bool IsCanUseAsEventCode(Type type)
        {
            return (type?.GetCustomAttribute<EventCodeAttribute>(false, false) is not null
                || type?.GetInterface(nameof(IEventCode)) is not null)
                && type.GetCustomAttribute<NoEventCodeClassAttribute>(true, true) is null;
        }
        /// <summary>检查类型是否可以作为事件的接收者。</summary>
        public static bool IsListener(Type type)
        {
            return type?.GetCustomAttribute<EventListenerAttribute>(true, true) is not null
                && type.IsClass && !type.IsAbstract && !type.IsGenericType && type.IsVisible;
        }


        //internal


    }
}

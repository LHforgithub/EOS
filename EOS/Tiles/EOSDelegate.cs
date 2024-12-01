using EOS.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EOS.Tiles
{
    /// <summary>
    /// 用于EOS事件的委托处理类型。仅用于处理，不应传播至程序集外。
    /// </summary>
    internal sealed class EOSDelegate
    {
        public EventCode Code { get; set; }
        public EOSControler Controler { get; set; }
        public List<ParameterInfo> Parameters => Code?.Parameters;
        public Type ReturnType => Code?.ReturnType;

        private Dictionary<object, EOSMethod> InstanceDelegateQueue = new Dictionary<object, EOSMethod>();
        public void Add(object instance, EOSMethodData methodFrom)
        {
            var eosMethod = new EOSMethod();
            eosMethod.Data = methodFrom;
            eosMethod.TargetObject = instance;
            if (instance is null)
            {
                if (!methodFrom.IsStatic)
                {
                    throw new InvalidOperationException($"[{nameof(Add)}] : Argument{nameof(instance)} is null, and there's no static method to call. Need instance object.");
                }
                InstanceDelegateQueue.AddOrUpdata(methodFrom.Method.ReflectedType, eosMethod);
                return;
            }
            InstanceDelegateQueue.AddOrUpdata(instance, eosMethod);
        }
        public void Remove(object instance, EOSMethodData methodFrom)
        {
            if (instance is null)
            {
                InstanceDelegateQueue.Remove(methodFrom.Method.ReflectedType);
            }
            else
            {
                InstanceDelegateQueue.Remove(instance);
            }
        }

        public void Invoke(params object[] values)
        {
            var list = new List<EOSMethod>(InstanceDelegateQueue.Values);
            if (list.Count < 1)
            {
                return;
            }
            list.Sort();
            var dif = values.Length - Parameters.Count;
            if (dif > 0)
            {
                values = values.Take(Parameters.Count).ToArray();
            }
            else if (dif < 0)
            {
                var defultValues = new List<object>();
                var @params = Parameters.FindAll(p => p.Position >= values.Length).OrderBy(p => p.Position);
                foreach (var param in @params)
                {
                    defultValues.Add(param.RawDefaultValue);
                }
                values = values.Concat(defultValues).ToArray();
            }
            foreach (var eosMethod in list)
            {
                eosMethod.Method.Invoke(eosMethod.TargetObject, values);
            }
        }
        public void Clear()
        {
            InstanceDelegateQueue.Clear();
        }
    }
}

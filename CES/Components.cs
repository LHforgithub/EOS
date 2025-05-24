using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CES
{
    public abstract class TriggerBase : ICESTrigger, ICESTargetable
    {
        public SingleEffect Owner { get; set; }
        public virtual List<float> UsingNumber => [];
        public virtual bool IsUsingNumber { get; }
        public virtual int UsingNumberCount { get; }
        public int SelfIndex { get; set; } = 0;
        public virtual List<Type> ProvideParamTypes => [];
        public List<ICESParamable> ProvideParams { get; } = [];
        public abstract IDescribeProcessor DescribeProcessor { get; }
        public abstract string ChangeDescription(string originalDesc);
        public abstract void OnTrigger();
        public abstract void Init();
        public abstract void Destroy();
        public int CompareTo(ICESComponent other)
        {
            return SelfIndex.CompareTo(other?.SelfIndex ?? 0);
        }
    }
    public abstract class FreeParamBase<T> : ICESFreeParam where T : ICESParamable
    {
        public SingleEffect Owner { get; set; }
        public virtual List<float> UsingNumber => [];
        public virtual bool IsUsingNumber { get; }
        public virtual int UsingNumberCount { get; }
        public int SelfIndex { get; set; } = 0;
        public Type ProvideParamType => typeof(T);
        public abstract IDescribeProcessor DescribeProcessor { get; }
        public abstract string ChangeDescription(string originalDesc);
        public ICESParamable TryGetParam() => GetParam();
        public abstract T GetParam();
        public abstract void Init();
        public abstract void Destroy();
        public int CompareTo(ICESComponent other)
        {
            return SelfIndex.CompareTo(other?.SelfIndex ?? 0);
        }
    }
    public abstract class ParamProcessorBase<T, U> : ICESParamProcessor where T :ICESParamable where U : ICESParamable
    {
        public SingleEffect Owner { get; set; }
        public virtual List<float> UsingNumber => [];
        public virtual bool IsUsingNumber { get; }
        public virtual int UsingNumberCount { get; }
        public int SelfIndex { get; set; } = 0;
        public virtual Type RequireParamType => typeof(T);
        public int RequireParamIndex { get; set; }
        public virtual Type ProvideParamType => typeof(U);
        public abstract IDescribeProcessor DescribeProcessor { get; }
        public abstract string ChangeDescription(string originalDesc);
        public ICESParamable Process(ICESParamable param) => ProcessParam((T)param);
        public abstract U ProcessParam(T param);
        public abstract void Init();
        public abstract void Destroy();
        public int CompareTo(ICESComponent other)
        {
            return SelfIndex.CompareTo(other?.SelfIndex ?? 0);
        }
    }
    public abstract class ConditionBase : ICESCondition, ICESTargetable
    {
        public SingleEffect Owner { get; set; }
        public virtual List<float> UsingNumber => [];
        public virtual bool IsUsingNumber { get; }
        public virtual int UsingNumberCount { get; }
        public int SelfIndex { get; set; } = 0;
        public int AffectComponentIndex { get; set; }
        public virtual List<Type> RequireParamTypes { get; } = [];
        public List<int> RequireParamIndexes { get; } = [];
        public abstract IDescribeProcessor DescribeProcessor { get; }
        public abstract string ChangeDescription(string originalDesc);
        public abstract bool Check(List<ICESParamable> param);
        public abstract void Init();
        public abstract void Destroy();
        public int CompareTo(ICESComponent other)
        {
            return SelfIndex.CompareTo(other?.SelfIndex ?? 0);
        }
    }
    public abstract class TargetSearchBase<T> : ICESTargetSearch, ICESTargetable where T : ICESTargetable
    {
        public SingleEffect Owner { get; set; }
        public virtual List<float> UsingNumber => [];
        public virtual bool IsUsingNumber { get; }
        public virtual int UsingNumberCount { get; }
        public int SelfIndex { get; set; } = 0;
        public virtual Type ProvideTargetType => typeof(T);
        public abstract IDescribeProcessor DescribeProcessor { get; }
        public abstract string ChangeDescription(string originalDesc);
        public List<ICESTargetable> GetAll() => [.. GetAllTarget().OfType<ICESTargetable>()];
        public List<ICESTargetable> Search(List<ICESTargetable> filterTarget) => [.. SearchTarget([.. filterTarget.OfType<T>()]).OfType<ICESTargetable>()];
        public abstract List<T> GetAllTarget();
        public abstract List<T> SearchTarget(List<T> filterTarget);
        public abstract void Init();
        public abstract void Destroy();
        public int CompareTo(ICESComponent other)
        {
            return SelfIndex.CompareTo(other?.SelfIndex ?? 0);
        }
    }
    public abstract class ActivityBase : ICESActivity
    {
        public SingleEffect Owner { get; set; }
        public virtual List<float> UsingNumber => [];
        public virtual bool IsUsingNumber { get; }
        public virtual int UsingNumberCount { get; }
        public int SelfIndex { get; set; } = 0;
        public IDescribeProcessor DescribeProcessor => null;
        public virtual string ChangeDescription(string originalDesc)
        {
            return "";
        }
        
        private Dictionary<ICESCondition, List<ICESParamable>> ConditionParamsSearch(int affectIndex, List<ICESParamable> plusParams = null)
        {
            var result = new Dictionary<ICESCondition, List<ICESParamable>>();
            plusParams ??= [];
            List<ICESCondition> conditions = [.. Owner.Conditions];
            for (int i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];
                if (condition == null || condition.AffectComponentIndex != affectIndex)
                {
                    continue;
                }
                var paramList = new List<ICESParamable>();
                for (int j = 0; j < condition.RequireParamTypes.Count; j++)
                {
                    var index = condition.RequireParamIndexes[j];
                    var comp = Owner.SearchParamComponentByIndex(index);
                    if (comp == null)
                    {
                        LogTool.Instance.Log($"Invalid Activity. Condition's parameters error. Param index: {j}, Require Index: {index}");
                        continue;
                    }
                    paramList.Add(Owner.GetFinalParam(comp));
                }
                paramList = [.. paramList.Where(x => x != null)];
                if (paramList.Count + plusParams.Count != condition.RequireParamTypes.Count)
                {
                    LogTool.Instance.Log($"Invalid Activity. Condition's parameters count error. Need param num: {condition.RequireParamTypes.Count}, recent count: {paramList.Count + plusParams.Count}.");
                    continue;
                }
                result.Add(condition, [.. paramList, .. plusParams]);
            }
            return result;
        }
        private List<ICESTargetable> TargetConditionCheckGet(ICESTargetSearch searchFunc)
        {
            var allTargets = searchFunc.GetAll();
            var result = new List<ICESTargetable>();
            foreach (var target in allTargets)
            {
                if (ConditionCheck(ConditionParamsSearch(searchFunc.SelfIndex, [target])))
                {
                    result.Add(target);
                }
            }
            return searchFunc.Search(result);
        }
        public void Action()
        {
            if (Owner == null || Owner.Activity != this)
            {
                LogTool.Instance.Log($"Invalid Activity. Owner info error.");
                return;
            }
            if (ConditionCheck(ConditionParamsSearch(Owner.Trigger.SelfIndex)))
            {
                var dic = new Dictionary<ICESEffect, KeyValuePair<List<ICESParamable>, List<ICESTargetable>[]>>();
                for (int i = 0; i < Owner.Effects.Count; i++)
                {
                    if (i >= Owner.Effects.Count)
                    {
                        break;
                    }
                    var effect = Owner.Effects[i];
                    if (effect != null)
                    {
                        var effectParams = new List<ICESParamable>();
                        for (int j = 0; j < effect.RequireParamTypes.Count; j++)
                        {
                            if (j >= effect.RequireParamIndexes.Count)
                            {
                                break;
                            }
                            var index = effect.RequireParamIndexes[j];
                            var param = Owner.SearchParamComponentByIndex(index);
                            if (param == null)
                            {
                                break;
                            }
                            effectParams.InsertOrUpdateAt(j, Owner.GetFinalParam(param, index));
                        }
                        if (effectParams.Count != effect.RequireParamTypes.Count)
                        {
                            LogTool.Instance.Log($"Invalid Activity. Effect's parameters count error. Need param num: {effect.RequireParamTypes.Count}, recent count: {effectParams.Count}.");
                            continue;
                        }
                        var effectTargets = new List<ICESTargetable>[effect.RequireTargetTypes.Count];
                        for (int j = 0; j < effect.RequireTargetTypes.Count; j++)
                        {
                            if (j >= effect.RequireTargetIndexes.Count)
                            {
                                break;
                            }
                            var index = effect.RequireTargetIndexes[j];
                            var target = Owner.SearchParamComponentByIndex(index);
                            if (target is ICESTargetSearch tar)
                            {
                                effectTargets[j] = TargetConditionCheckGet(tar);
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (effectTargets.Count() != effect.RequireTargetTypes.Count)
                        {
                            LogTool.Instance.Log($"Invalid Activity. Effect's parameters count error. Need param num: {effect.RequireTargetTypes.Count}, recent count: {effectTargets.Count()}.");
                            continue;
                        }
                        dic.Add(effect, new KeyValuePair<List<ICESParamable>, List<ICESTargetable>[]>(effectParams, effectTargets));
                    }
                }
                EffectAction(dic);
            }
        }
        public abstract bool ConditionCheck(Dictionary<ICESCondition, List<ICESParamable>> triggerConditions);
        public abstract void EffectAction(Dictionary<ICESEffect, KeyValuePair<List<ICESParamable>, List<ICESTargetable>[]>> effectActionDic);
        public abstract void Init();
        public abstract void Destroy();
        public int CompareTo(ICESComponent other)
        {
            return SelfIndex.CompareTo(other?.SelfIndex ?? 0);
        }
    }
    public abstract class EffectBase : ICESEffect, ICESTargetable
    {
        public SingleEffect Owner { get; set; }
        public virtual List<float> UsingNumber => [];
        public virtual bool IsUsingNumber { get; }
        public virtual int UsingNumberCount { get; }
        public int SelfIndex { get; set; } = 0;
        public abstract IDescribeProcessor DescribeProcessor { get; }
        public virtual List<Type> RequireParamTypes => [];
        public List<int> RequireParamIndexes { get; } = [];
        public virtual List<Type> RequireTargetTypes => [];
        public List<int> RequireTargetIndexes { get; } = [];
        public abstract string ChangeDescription(string originalDesc);
        public abstract int Effect(List<ICESParamable> @params, List<ICESTargetable>[] targets);
        public abstract void Init();
        public abstract void Destroy();
        public int CompareTo(ICESComponent other)
        {
            return SelfIndex.CompareTo(other?.SelfIndex ?? 0);
        }
    }
}
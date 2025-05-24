using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES
{
    public sealed class ComponentReference
    {
        public ICESComponent SourceComponent { get; set; }
        public ICESComponent ReferenceComponent { get; set; }
        public List<ComponentReference> SameSourceReferences { get; } = [];
        public bool IsParam { get; set; } = false;
        public bool IsTarget { get; set; } = false;
        public bool IsAffectTarget { get; set; } = false;
        public int TriggerParamIndex { get; set; } = 0;
        public int MultipleRequireIndex { get; set; } = 0;
        public int ErrorCode { get; set; } = 0;
        public void SplitAll(ComponentReference other = null)
        {
            other ??= this;
            foreach (var item in new List<ComponentReference>(SameSourceReferences))
            {
                item.SpliteOther(other);
            }
        }
        public void SpliteOther(ComponentReference other)
        {
            if (SameSourceReferences.Remove(other))
            {
                other.SpliteOther(this);
            }
        }
        public void MergeOther(ComponentReference other)
        {
            if (SameSourceReferences.TryAdd(other))
            {
                other.MergeOther(this);
            }
        }
        public bool IsSameTarget(ComponentReference other)
        {
            if (other == null)
            {
                return false;
            }
            if (SourceComponent == other.SourceComponent)
            {
                if (IsAffectTarget)
                {
                    return IsAffectTarget == other.IsAffectTarget;
                }
                if (IsParam)
                {
                    return IsParam == other.IsParam && TriggerParamIndex == other.TriggerParamIndex && MultipleRequireIndex == other.MultipleRequireIndex;
                }
                if (IsTarget)
                {
                    return IsTarget == other.IsTarget && MultipleRequireIndex == other.MultipleRequireIndex;
                }
            }
            return false;
        }

        public bool InvalidCheck()
        {
            if (SourceComponent == null)
            {
                return true;
            }
            if (SourceComponent is not ICESParamProcessor && SourceComponent is not ICESCondition && SourceComponent is not ICESEffect)
            {
                return true;
            }
            if (IsParam == true && IsTarget == true)
            {
                return true;
            }
            if (TriggerParamIndex < 0 || MultipleRequireIndex < 0)
            {
                return true;
            }
            return false;
        }
        public bool IsError()
        {
            return ErrorCode != 0;
        }
        public void Dispose()
        {
            SplitAll();
            SourceComponent = null;
            ReferenceComponent = null;
            SameSourceReferences.Clear();
        }
        public List<ComponentReference> GetAllSameTarget(IEnumerable<ComponentReference> componentReferences = null)
        {
            var result = new List<ComponentReference>();
            componentReferences ??= SameSourceReferences;
            var item = componentReferences.FirstOrDefault();
            if (item == null)
            {
                return result;
            }
            result.AddRange(componentReferences.Where(i => i.IsSameTarget(this)));
            result.AddRange(item.GetAllSameTarget(componentReferences.Where(x => x != item)));
            return result;
        }
        public override string ToString()
        {
            return $"Source : <{SourceComponent}>,\n" +
                $"Reference : <{ReferenceComponent}>,\n" +
                $"MergeCount : <{SameSourceReferences?.Count}>,\n" +
                $"IsParam : <{IsParam}>,\n" +
                $"IsTarget: <{IsTarget}>,\n" +
                $"IsAffectTarget: {IsAffectTarget},\n" +
                $"ParamIndex : <{TriggerParamIndex}>,\n" +
                $"ErrorCode : <{CES.ErrorCode.Code.FirstOrDefault(x => x.Key == ErrorCode)}>";
        }
    }

    public static class ErrorCode
    {
        public static Dictionary<int, string> Code { get; } = new()
        {
            // ProcessCombination.AddComponent
            { -1, "ProcessCombination.AddComponent: 输入的组件无效" },
            { 2, "ProcessCombination.AddComponent: 输入的自由参数类型已经存在" },
            { 3, "ProcessCombination.AddComponent: 输入的参数处理器类型已经存在" },
            { 4, "ProcessCombination.AddComponent: 输入的条件类型已经存在" },
            { 5, "ProcessCombination.AddComponent: 输入的目标搜索器类型已经存在" },
            { 6, "ProcessCombination.AddComponent: 输入的效果类型已经存在" },
            { 7, "ProcessCombination.RemoveComponent: 输入的组件类型是触发器，但不是该实例当前触发器" },
            { 8, "ProcessCombination.RemoveComponent: 输入的组件类型是自由参数，但不是该实例当前持有的参数" },
            { 9, "ProcessCombination.RemoveComponent: 输入的组件类型是参数处理器，但不是该实例当前持有的处理器" },
            { 10, "ProcessCombination.RemoveComponent: 输入的组件类型是条件，但不是该实例当前持有的条件" },
            { 11, "ProcessCombination.RemoveComponent: 输入的组件类型是目标搜索器，但不是该实例当前持有的搜索器" },
            { 12, "ProcessCombination.RemoveComponent: 输入的组件类型是效果，但不是该实例当前持有的效果" },
            { 13, "ProcessCombination.RemoveComponent: 输入的组件类型是活动，但不是该实例当前持有的活动" },

            // ProcessCombination.AddReference
            { 5001, "ProcessCombination.AddReference: 输入的影响目标组件无效" },
            { 5002, "ProcessCombination.AddReference: 输入的引用组件无效" },
            { 5003, "ProcessCombination.AddReference: 影响目标组件未加入此实例" },
            { 5004, "ProcessCombination.AddReference: 引用资源未加入此实例" },
            { 5005, "ProcessCombination.AddReference: 输入的影响目标组件不是参数处理器、条件或效果" },
            { 5006, "ProcessCombination.AddReference: IsParam和IsTarget不能同时为true" },
            { 5007, "ProcessCombination.AddReference: 引用资源提供的是触发器，但给出的触发器参数索引越界" },
            { 5008, "ProcessCombination.AddReference: 引用资源指示为影响目标，但提供的不是触发器或目标搜索器" },
            { 5009, "ProcessCombination.AddReference: 影响目标组件存在多个参数或目标需求，引用资源需要提供参数的目标需求索引" },
            { 5010, "ProcessCombination.AddReference: 现有的引用资源中已经存在相同指向的资源" },
            { 5011, "ProcessCombination.AddReference: 无意义的引用资源"},
            { 5100, "ProcessCombination.AddReference: 输入的引用资源无效" },

            // ProcessCombination.CheckResult
            { 9001, "ProcessCombination.CheckResult: 已输入的触发器无效" },
            { 9002, "ProcessCombination.CheckResult: 已输入的活动无效" },
            { -1000, "ProcessCombination.CheckResult: 存在错误的组件" },

            // ProcessCombination.CheckParamProcessor
            { 1003, "ProcessCombination.CheckParamProcessor: 该参数处理器未定义任何引用资源" },
            { 1004, "ProcessCombination.CheckParamProcessor: 该参数处理器的引用资源引用组件为空" },
            { 1005, "ProcessCombination.CheckParamProcessor: 该参数处理器的引用资源引用组件未加入此实例" },
            { 1006, "ProcessCombination.CheckParamProcessor: 该参数处理器的引用资源不是合理的参数提供器" },
            { 1007, "ProcessCombination.CheckParamProcessor: 该参数处理器的引用资源为触发器，但提供的索引越界" },
            { 1008, "ProcessCombination.CheckParamProcessor: 引用资源提供的触发器中的参数索引对应的参数类型不匹配" },
            { 1009, "ProcessCombination.CheckParamProcessor: 引用资提供的自由参数的类型不匹配" },
            { 1010, "ProcessCombination.CheckParamProcessor: 引用资源提供的参数处理器的类型不匹配" },
            { 1011, "ProcessCombination.CheckParamProcessor: 引用资源提供的参数处理器为需求自身" },
            { 1012, "ProcessCombination.CheckParamProcessor: 引用资源提供的参数处理器所处位置在该处理器后" },
            { 1013, "ProcessCombination.CheckParamProcessor: 引用资源中存在参数处理器列表中的循环引用" },

            // ProcessCombination.CheckCondition
            { 1103, "ProcessCombination.CheckCondition: 该条件未定义任何引用资源" },
            { 1104, "ProcessCombination.CheckCondition: 该条件的引用资源引用组件为空" },
            { 1105, "ProcessCombination.CheckCondition: 该条件的引用资源引用组件未加入此实例" },
            { 1106, "ProcessCombination.CheckCondition: 引用资源指示为影响目标，但提供的不是触发器或目标搜索器" },
            { 1110, "ProcessCombination.CheckCondition: 引用资源指示为参数提供但提供的不是合法的参数提供器" },
            { 1111, "ProcessCombination.CheckCondition: 引用资源提供的触发器参数索引越界" },
            { 1112, "ProcessCombination.CheckCondition: 引用资源提供的需求类型索引越界" },
            { 1113, "ProcessCombination.CheckCondition: 引用资源提供的触发器参数类型与需求的参数类型不匹配" },
            { 1121, "ProcessCombination.CheckCondition: 引用资源提供的需求类型索引越界" },
            { 1122, "ProcessCombination.CheckCondition: 引用资源提供的自由参数类型与需求的参数类型不匹配" },
            { 1131, "ProcessCombination.CheckCondition: 引用资源提供的需求类型索引越界" },
            { 1132, "ProcessCombination.CheckCondition: 引用资源提供的自由参数类型与需求的参数类型不匹配" },
            { 1200, "ProcessCombination.CheckCondition: 该条件的引用资源不是合法的条件的参数提供器" },
            { 1301, "ProcessCombination.CheckCondition: 引用资源提供的条件的影响触发器对象提供的参数索引越界" },
            { 1302, "ProcessCombination.CheckCondition: 引用资源提供的条件的需求参数索引越界" },
            { 1303, "ProcessCombination.CheckCondition: 引用资源提供的条件的影响触发器对象提供的参数类型与需求的参数类型不匹配" },
            { 1311, "ProcessCombination.CheckCondition: 引用资源提供的条件的需求参数索引越界" },
            { 1312, "ProcessCombination.CheckCondition: 条件的该需求参数未提供索引" },
            { 1313, "ProcessCombination.CheckCondition: 条件的该需求参数索引越界" },
            { 1323, "ProcessCombination.CheckCondition: 条件的该需求参数索引指向目标搜索器，但是该目标搜索器不是当前实例的目标搜索器" },
            { 1324, "ProcessCombination.CheckCondition: 条件的该需求参数索引指向目标处理器，但是该索引对应的条件的需求参数类型与目标处理器的提供的目标类型不匹配" },
            { 1325, "ProcessCombination.CheckCondition: 条件的该需求参数索引指向目标处理器，但是该条件的参数需求索引中没有指向该目标处理器提供的目标类型的索引" },
            { 1331, "ProcessCombination.CheckCondition: 条件的该需求参数索引指向触发器，但是该触发器不是当前实例的触发器" },
            { 1332, "ProcessCombination.CheckCondition: 条件的该需求参数索引指向触发器，但是该索引对应的条件的需求参数类型与触发器提供的参数类型不匹配" },
            { 1333, "ProcessCombination.CheckCondition: 条件的该需求参数索引指向触发器，但是索引超出该触发器提供的参数类型的数量" },
            { 1341, "ProcessCombination.CheckCondition: 条件的该需求参数索引指向自由参数，但是该自由参数不是当前实例的自由参数" },
            { 1342, "ProcessCombination.CheckCondition: 条件的该需求参数索引指向自由参数，但是该索引对应的条件的需求参数类型与自由参数提供的参数类型不匹配" },
            { 1351, "ProcessCombination.CheckCondition: 条件的该需求参数索引指向参数处理器，但是该参数处理器不是当前实例的参数处理器" },
            { 1352, "ProcessCombination.CheckCondition: 条件的该需求参数索引指向参数处理器，但是该索引对应的条件的需求参数类型与参数处理器提供的参数类型不匹配" },

            // ProcessCombination.CheckEffect
            { 2005, "ProcessCombination.CheckEffect: 该效果未定义任何引用资源" },
            { 2006, "ProcessCombination.CheckEffect: 该效果的引用资源引用组件为空" },
            { 2007, "ProcessCombination.CheckEffect: 该效果的引用资源引用组件未加入此实例" },
            { 2012, "ProcessCombination.CheckEffect: 引用资源指示为目标提供但是指向的不是合法的目标搜索器" },
            { 2021, "ProcessCombination.CheckEffect: 引用资源提供的目标搜索器不是当前实例的目标搜索器" },
            { 2022, "ProcessCombination.CheckEffect: 引用资源提供的需求目标类型索引越界" },
            { 2023, "ProcessCombination.CheckEffect: 引用资源提供的目标搜索器的目标类型与需求的目标类型不匹配" },
            { 2312, "ProcessCombination.CheckEffect: 效果的该需求参数未提供索引" },
            { 2313, "ProcessCombination.CheckEffect: 效果的该需求参数索引越界" },
            { 2331, "ProcessCombination.CheckEffect: 效果的该需求参数索引指向触发器，但是该触发器不是当前实例的触发器" },
            { 2332, "ProcessCombination.CheckEffect: 效果的该需求参数索引指向触发器，但是该索引对应的效果的需求参数类型与触发器提供的参数类型不匹配" },
            { 2333, "ProcessCombination.CheckEffect: 效果的该需求参数索引指向触发器，但是索引超出该触发器提供的参数类型的数量" },
            { 2341, "ProcessCombination.CheckEffect: 效果的该需求参数索引指向自由参数，但是该自由参数不是当前实例的自由参数" },
            { 2342, "ProcessCombination.CheckEffect: 效果的该需求参数索引指向自由参数，但是该索引对应的效果的需求参数类型与自由参数提供的参数类型不匹配" },
            { 2351, "ProcessCombination.CheckEffect: 效果的该需求参数索引指向参数处理器，但是该参数处理器不是当前实例的参数处理器" },
            { 2352, "ProcessCombination.CheckEffect: 效果的该需求参数索引指向参数处理器，但是该索引对应的效果的需求参数类型与参数处理器提供的参数类型不匹配" },
            { 2422, "ProcessCombination.CheckEffect: 效果的该需求目标类型未提供索引" },
            { 2423, "ProcessCombination.CheckEffect: 效果的该需求目标类型索引越界" },
            { 2441, "ProcessCombination.CheckEffect: 效果的该需求目标类型索引指向目标搜索器，但是该目标搜索器不是当前实例的目标搜索器" },
            { 2442, "ProcessCombination.CheckEffect: 效果的该需求目标类型索引指向目标搜索器，但是该索引对应的效果的需求参数类型与目标搜索器提供的目标类型不匹配" },

            // 通用
            { 9003, "ProcessCombination: 引用资源中存在相同指向的资源" },
        };
    }
    public sealed class ProcessCombination 
    {
        internal ICESTrigger Trigger { get; set; }
        internal List<ICESFreeParam> FreeParams { get; } = [];
        internal List<ICESParamProcessor> ParamProcessors { get; } = [];
        internal List<ICESTargetSearch> TargetSearches { get; } = [];
        internal List<ICESCondition> Conditions { get; } = []   ;
        internal List<ICESEffect> Effects { get; } = [];
        internal ActivityBase Activity { get; set; }
        private List<ComponentReference> ComponentReferences { get; } = [];
        public List<ComponentReference> ErrorComponents { get; } = [];
        private bool InvalidCheck(ICESComponent component)
        {
            return component == null || component.Owner != null;
        }
        private bool Contains(ICESComponent component)
        {
            switch (component)
            {
                case null:
                    return false;
                case ICESTrigger:
                    {
                        return component == Trigger;
                    }
                case ICESFreeParam:
                    {
                        return FreeParams.Contains((ICESFreeParam)component);
                    }
                case ICESParamProcessor:
                    {
                        return ParamProcessors.Contains((ICESParamProcessor)component);
                    }
                case ICESCondition:
                    {
                        return Conditions.Contains((ICESCondition)component);
                    }
                case ICESTargetSearch:
                    {
                        return TargetSearches.Contains((ICESTargetSearch)component);
                    }
                case ICESEffect:
                    {
                        return Effects.Contains((ICESEffect)component);
                    }
                case ICESActivity:
                    {
                        return component == Activity;
                    }
                default:
                    return false;
            }
        }
        private int GetLastParamSelfIndex()
        {
            return (Trigger?.ProvideParamTypes.Count ?? 0) + FreeParams.Count + ParamProcessors.Count - 1;
        }
        private ICESComponent SearchIndexComponent(int index)
        {
            if (index < 0)
            {
                return null;
            }
            if (index < (Trigger?.ProvideParamTypes.Count ?? 0))
            {
                return Trigger;
            }
            var list = new List<ICESComponent>();
            list.AddRange(FreeParams);
            list.AddRange(ParamProcessors);
            list.AddRange(Conditions);
            list.AddRange(TargetSearches);
            list.AddRange(Effects);
            list.Add(Activity);
            return list.FirstOrDefault(x => x.SelfIndex == index);
        }
        public int AddComponent(ICESComponent component)
        {
            if (InvalidCheck(component))
            {
                //输入的组件无效
                return -1;
            }
            switch (component)
            {
                case ICESTrigger:
                    {
                        Trigger = (ICESTrigger)component;
                        break;
                    }
                case ICESFreeParam:
                    {
                        var freeParam = (ICESFreeParam)component;
                        if (!FreeParams.TryAdd(freeParam))
                        {
                            //输入的自由参数类型已经存在
                            return 2;
                        }
                        break;
                    }
                case ICESParamProcessor:
                    {
                        var paramProcessor = (ICESParamProcessor)component;
                        if (!ParamProcessors.TryAdd(paramProcessor))
                        {
                            //输入的参数处理器类型已经存在
                            return 3;
                        }
                        break;
                    }
                case ICESCondition:
                    {
                        var condition = (ICESCondition)component;
                        if (!Conditions.TryAdd(condition))
                        {
                            //输入的条件类型已经存在
                            return 4;
                        }
                        break;
                    }
                case ICESTargetSearch:
                    {
                        var targetSearch = (ICESTargetSearch)component;
                        if (!TargetSearches.TryAdd(targetSearch))
                        {
                            //输入的目标搜索器类型已经存在
                            return 5;
                        }
                        break;
                    }
                case ICESEffect:
                    {
                        var effect = (ICESEffect)component;
                        if (!Effects.TryAdd(effect))
                        {
                            //输入的效果类型已经存在
                            return 6;
                        }
                        break;
                    }
                case ICESActivity:
                    {
                        Activity = (ActivityBase)component;
                        break;
                    }
                default:
                    break;
            }
            return 0;
        }
        public int RemoveComponent(ICESComponent component)
        {
            if (InvalidCheck(component))
            {
                //输入的组件无效
                return -1;
            }
            switch (component)
            {
                case ICESTrigger:
                    {
                        if (component == Trigger)
                        {
                            Trigger = null;
                        }
                        else
                        {
                            //输入的组件类型是触发器，但不是该实例当前触发器
                            return 7;
                        }
                        break;
                    }
                case ICESFreeParam:
                    {
                        var freeParam = (ICESFreeParam)component;
                        if (!FreeParams.Remove(freeParam))
                        {
                            //输入的组件类型是自由参数，但不是该实例当前持有的参数
                            return 8;
                        }
                        break;
                    }
                case ICESParamProcessor:
                    {
                        var paramProcessor = (ICESParamProcessor)component;
                        if (!ParamProcessors.Remove(paramProcessor))
                        {
                            //输入的组件类型是参数处理器，但不是该实例当前持有的处理器
                            return 9;
                        }
                        break;
                    }
                case ICESCondition:
                    {
                        var condition = (ICESCondition)component;
                        if (!Conditions.Remove(condition))
                        {
                            //输入的组件类型是条件，但不是该实例当前持有的条件
                            return 10;
                        }
                        break;
                    }
                case ICESTargetSearch:
                    {
                        var targetSearch = (ICESTargetSearch)component;
                        if (!TargetSearches.Remove(targetSearch))
                        {
                            //输入的组件类型是目标搜索器，但不是该实例当前持有的搜索器
                            return 11;
                        }
                        break;
                    }
                case ICESEffect:
                    {
                        var effect = (ICESEffect)component;
                        if (!Effects.Remove(effect))
                        {
                            //输入的组件类型是效果，但不是该实例当前持有的效果
                            return 12;
                        }
                        break;
                    }
                case ICESActivity:
                    {
                        if (component == Activity)
                        {
                            Activity = null;
                        }
                        else
                        {
                            //输入的组件类型是活动，但不是该实例当前持有的活动
                            return 13;
                        }
                        break;
                    }
                default:
                    break;
            }
            return 0;
        }
        public int SelfIndexReflect()
        {
            if (InvalidCheck(Trigger))
            {
                return 101;
            }
            if (InvalidCheck(Activity))
            {
                return 102;
            }
            var count = 0;
            Trigger.SelfIndex = 0;
            count += Trigger.ProvideParamTypes.Count;
            FreeParams.RemoveAll(x => x == null);
            for (int i = 0; i < FreeParams.Count; i++)
            {
                FreeParams[i].SelfIndex = count;
                count++;
            }
            ParamProcessors.RemoveAll(x => x == null);
            for (int i = 0; i < ParamProcessors.Count; i++)
            {
                ParamProcessors[i].SelfIndex = count;
                count++;
            }
            TargetSearches.RemoveAll(x => x == null);
            for (int i = 0; i < TargetSearches.Count; i++)
            {
                TargetSearches[i].SelfIndex = count;
                count++;
            }
            Conditions.RemoveAll(x => x == null);
            for (int i = 0; i < Conditions.Count; i++)
            {
                Conditions[i].SelfIndex = count;
                count++;
            }
            Effects.RemoveAll(x => x == null);
            for (int i = 0; i < Effects.Count; i++)
            {
                Effects[i].SelfIndex = count;
                count++;
            }
            Activity.SelfIndex = count;
            return 0;
        }
        public void ReferenceReflect()
        {
            ComponentReferences.RemoveAll(x => x == null);
            var list = ComponentReferences.FindAll(x => x.InvalidCheck()
                || (x.SourceComponent != null && !Contains(x.SourceComponent)));
            foreach(var item in list)
            {
                item.SplitAll();
                item.Dispose();
            }
        }
        public int CheckReference(ICESComponent sourceComponent, ICESComponent refereceComponent, bool IsParam, bool IsTarget, bool IsAffectTarget, int TriggerParamIndex = -1, int MultipleRequireIndex = -1)
        {
            if (InvalidCheck(sourceComponent))
            {
                //输入的影响目标组件无效
                return 5001;
            }
            if (InvalidCheck(refereceComponent))
            {
                //输入的引用组件无效
                return 5002;
            }
            if (!Contains(sourceComponent))
            {
                //影响目标组件未加入此实例
                return 5003;
            }
            if (!Contains(refereceComponent))
            {
                //引用资源未加入此实例
                return 5004;
            }
            if (sourceComponent is not ICESParamProcessor && sourceComponent is not ICESCondition && sourceComponent is not ICESEffect)
            {
                //输入的影响目标组件不是参数处理器、条件或效果
                return 5005;
            }
            if (IsParam == true)
            {
                if (IsTarget == true)
                {
                    //IsParam和IsTarget不能同时为true
                    return 5006;
                }
                if (refereceComponent is ICESTrigger tr)
                {
                    if (TriggerParamIndex < 0 || TriggerParamIndex > tr.ProvideParamTypes.Count)
                    {
                        //引用资源提供的是触发器，但给出的触发器参数索引越界
                        return 5007;
                    }
                }
            }
            if (IsAffectTarget && refereceComponent is not ICESTrigger && refereceComponent is not ICESTargetSearch)
            {
                return 5008;
            }
            if ((IsParam || IsTarget) && (sourceComponent is ICESCondition || sourceComponent is ICESEffect) && MultipleRequireIndex < 0)
            {
                //影响目标组件存在多个参数或目标需求，引用资源需要提供参数的目标需求索引
                return 5009;
            }
            if (!IsParam && !IsTarget && !IsAffectTarget)
            {
                //无意义的引用资源
                return 5011;
            }
            return 0;
        }
        public int CheckReference(ComponentReference reference)
        {
            return CheckReference(reference.SourceComponent, reference.ReferenceComponent, reference.IsParam, reference.IsTarget, reference.IsAffectTarget, reference.TriggerParamIndex, reference.MultipleRequireIndex);
        }
        public int AddReference(ICESComponent sourceComponent, ICESComponent refereceComponent, bool IsParam, bool IsTarget, bool IsAffectTarget, int TriggerParamIndex = -1, int MultipleRequireIndex = -1)
        {
            var result = CheckReference(sourceComponent, refereceComponent, IsParam, IsTarget, IsAffectTarget, TriggerParamIndex, MultipleRequireIndex);
            if (result != 0)
            {
                return result;
            }
            if(TriggerParamIndex < 0)
            {
                TriggerParamIndex = 0;
            }
            if (MultipleRequireIndex < 0)
            {
                MultipleRequireIndex = 0;
            }
            var reference = new ComponentReference()
            {
                SourceComponent = sourceComponent,
                ReferenceComponent = refereceComponent,
                IsParam = IsParam,
                IsTarget = IsTarget,
                IsAffectTarget = IsAffectTarget,
                TriggerParamIndex = TriggerParamIndex,
                MultipleRequireIndex = MultipleRequireIndex
            };
            var last = ComponentReferences.FirstOrDefault(x => x.SourceComponent == sourceComponent);
            if (last == null)
            {
                ComponentReferences.Add(reference);
            }
            else
            {
                foreach (var item in new List<ComponentReference>([last, .. last.SameSourceReferences]))
                {
                    if (item.IsSameTarget(reference))
                    {
                        //现有的引用资源中已经存在相同指向的资源
                        return 5010;
                    }
                }
                last.MergeOther(reference);
            }
            return 0;
        }
        public int AddReference(ComponentReference reference)
        {
            if (reference == null || reference.InvalidCheck() || reference.IsError())
            {
                //输入的引用资源无效
                return 5100;
            }
            var last = ComponentReferences.FirstOrDefault(x => x.SourceComponent == reference.SourceComponent);
            if (last == null)
            {
                ComponentReferences.Add(reference);
            }
            else
            {
                foreach (var item in new List<ComponentReference>([last, .. last.SameSourceReferences]))
                {
                    if (item.IsSameTarget(reference))
                    {
                        //现有的引用资源中已经存在相同指向的资源
                        return 5010;
                    }
                }
                last.MergeOther(reference);
            }
            return 0;
        }
        public int RemoveReference(ICESComponent sourceComponent, ICESComponent refereceComponent, bool IsParam, bool IsTarget, bool IsAffectTarget, int MultipleRequireIndex = -1, int TriggerParamIndex = 0)
        {
            var result = CheckReference(sourceComponent, refereceComponent, IsParam, IsTarget, IsAffectTarget, TriggerParamIndex, MultipleRequireIndex);
            if (result != 0)
            {
                return result;
            }
            var reference = new ComponentReference()
            {
                SourceComponent = sourceComponent,
                ReferenceComponent = refereceComponent,
                IsParam = IsParam,
                IsTarget = IsTarget,
                IsAffectTarget = IsAffectTarget,
                TriggerParamIndex = TriggerParamIndex,
                MultipleRequireIndex = MultipleRequireIndex
            };
            var list = ComponentReferences.FindAll(x => x.IsSameTarget(reference));
            foreach (var item in list)
            {
                item.Dispose();
                ComponentReferences.Remove(item);
            }
            return 0;
        }
        public int RemoveReference(ComponentReference reference)
        {
            if (reference == null || reference.InvalidCheck() || reference.IsError())
            {
                //输入的引用资源无效
                return 5100;
            }
            var list = ComponentReferences.FindAll(x => x.IsSameTarget(reference));
            foreach (var item in list)
            {
                item.Dispose();
                ComponentReferences.Remove(item);
            }
            return 0;
        }
        public void CheckParamProcessor(ICESParamProcessor paramProcessor)
        {
            if (ComponentReferences.FirstOrDefault(x => x.SourceComponent == paramProcessor) is ComponentReference reference)
            {
                var sameTL = reference.GetAllSameTarget();
                if (sameTL.Count > 0)
                {
                    foreach (var st in sameTL)
                    {
                        //引用资源中存在相同指向的资源
                        st.ErrorCode = 9003;
                        ErrorComponents.Add(st);
                    }
                    return;
                }
                if (reference.ReferenceComponent == null)
                {
                    //该参数处理器的引用资源引用组件为空
                    reference.ErrorCode = 1004;
                    ErrorComponents.Add(reference);
                    return;
                }
                if (!Contains(reference.ReferenceComponent))
                {
                    //该参数处理器的引用资源引用组件未加入此实例
                    reference.ErrorCode = 1005;
                    ErrorComponents.Add(reference);
                    return;
                }
                switch (reference.ReferenceComponent)
                {
                    case ICESTrigger tri:
                        {
                            if (tri.ProvideParamTypes.ElementAtOrDefault(reference.TriggerParamIndex) is Type type)
                            {
                                if (paramProcessor.RequireParamType.IsInheritedBy(type))
                                {
                                    paramProcessor.RequireParamIndex = reference.TriggerParamIndex;
                                    return;
                                }
                                else
                                {
                                    //引用资源提供的触发器中的参数索引对应的参数类型不匹配
                                    reference.ErrorCode = 1008;
                                    ErrorComponents.Add(reference);
                                    return;
                                }
                            }
                            else
                            {
                                //该参数处理器的引用资源为触发器，但提供的索引越界
                                reference.ErrorCode = 1007;
                                ErrorComponents.Add(reference);
                                return;
                            }
                        }
                    case ICESFreeParam freeParam:
                        {
                            if (paramProcessor.RequireParamType.IsInheritedBy(freeParam.ProvideParamType))
                            {
                                paramProcessor.RequireParamIndex = freeParam.SelfIndex;
                                return;
                            }
                            else
                            {
                                //引用资提供的自由参数的类型不匹配
                                reference.ErrorCode = 1009;
                                ErrorComponents.Add(reference);
                                return;
                            }
                        }
                    case ICESParamProcessor pp:
                        {
                            bool LoopingReference(ICESParamProcessor pp, List<ICESParamProcessor> node)
                            {
                                var @ref = ComponentReferences.FirstOrDefault(x => x.SourceComponent == pp);
                                if (@ref == null || @ref.ReferenceComponent is not ICESParamProcessor)
                                {
                                    return false;
                                }
                                if (node.Contains(@ref.ReferenceComponent))
                                {
                                    return true;
                                }
                                node.Add(@ref.ReferenceComponent as ICESParamProcessor);
                                return LoopingReference(@ref.ReferenceComponent as ICESParamProcessor, node);
                            }
                            if (pp.SelfIndex < pp.SelfIndex)
                            {
                                if (LoopingReference(pp, []))
                                {
                                    //引用资源中存在参数处理器列表中的循环引用
                                    reference.ErrorCode = 1013;
                                    ErrorComponents.Add(reference);
                                    return;
                                }
                                if (pp.RequireParamType.IsInheritedBy(pp.ProvideParamType))
                                {
                                    paramProcessor.RequireParamIndex = pp.SelfIndex;
                                    return;
                                }
                                else
                                {
                                    //引用资源提供的参数处理器的类型不匹配
                                    reference.ErrorCode = 1010;
                                    ErrorComponents.Add(reference);
                                    return;
                                }
                            }
                            else if (pp.SelfIndex == paramProcessor.SelfIndex)
                            {
                                //引用资源提供的参数处理器为需求自身
                                reference.ErrorCode = 1011;
                                ErrorComponents.Add(reference);
                                return;
                            }
                            else
                            {
                                //引用资源提供的参数处理器所处位置在该处理器后
                                reference.ErrorCode = 1012;
                                ErrorComponents.Add(reference);
                                return;
                            }
                        }
                    default:
                        {
                            //该参数处理器的引用资源不是合理的参数提供器
                            reference.ErrorCode = 1006;
                            ErrorComponents.Add(reference);
                            return;
                        }
                }
            }
            else
            {
                //该参数处理器未定义任何引用资源
                ErrorComponents.Add(new ComponentReference() { SourceComponent = paramProcessor, ErrorCode = 1003 });
                return;
            }
        }
        public void CheckCondition(ICESCondition condition)
        {
            if (ComponentReferences.FirstOrDefault(x => x.SourceComponent == condition) is ComponentReference reference)
            {
                var refList = reference.SameSourceReferences;
                var sameTL = reference.GetAllSameTarget();
                if (sameTL.Count > 0)
                {
                    foreach (var st in sameTL)
                    {
                        //引用资源中存在相同指向的资源
                        st.ErrorCode = 9003;
                        ErrorComponents.Add(st);
                    }
                    return;
                }
                foreach (var @ref in new List<ComponentReference>([reference, .. reference.SameSourceReferences]).Distinct())
                {
                    if (@ref.ReferenceComponent == null)
                    {
                        //该条件的引用资源引用组件为空
                        @ref.ErrorCode = 1104;
                        ErrorComponents.Add(@ref);
                        continue;
                    }
                    if (!Contains(@ref.ReferenceComponent))
                    {
                        //该条件的引用资源引用组件未加入此实例
                        @ref.ErrorCode = 1105;
                        ErrorComponents.Add(@ref);
                        continue;
                    }
                    if (@ref.IsAffectTarget)
                    {
                        if (@ref.ReferenceComponent is ICESTrigger tg)
                        {
                            if (!@ref.IsParam)
                            {
                                continue;
                            }
                            var type = tg.ProvideParamTypes.ElementAtOrDefault(@ref.TriggerParamIndex);
                            var type2 = condition.RequireParamTypes.ElementAtOrDefault(@ref.MultipleRequireIndex);
                            if (type == null)
                            {
                                //引用资源提供的条件的影响触发器对象提供的参数索引越界
                                @ref.ErrorCode = 1301;
                                ErrorComponents.Add(@ref);
                                continue;
                            }
                            if (type2 == null)
                            {
                                //引用资源提供的条件的需求参数索引越界
                                @ref.ErrorCode = 1302;
                                ErrorComponents.Add(@ref);
                                continue;
                            }
                            if (!type2.IsInheritedBy(type))
                            {
                                //引用资源提供的条件的影响触发器对象提供的参数类型与需求的参数类型不匹配
                                @ref.ErrorCode = 1303;
                                ErrorComponents.Add(@ref);
                                continue;
                            }
                            condition.AffectComponentIndex = @ref.ReferenceComponent.SelfIndex;
                            condition.RequireParamIndexes.InsertOrUpdateAt(@ref.MultipleRequireIndex, @ref.TriggerParamIndex);
                            continue;
                        }
                        if (@ref.ReferenceComponent is ICESTargetSearch ts)
                        {
                            var type = condition.RequireParamTypes.ElementAtOrDefault(@ref.MultipleRequireIndex);
                            if (type == null)
                            {
                                //引用资源提供的条件的需求参数索引越界
                                @ref.ErrorCode = 1311;
                                ErrorComponents.Add(@ref);
                                continue;
                            }
                            if (!type.IsInheritedBy(ts.ProvideTargetType))
                            {
                                //引用资源提供的索引对应的条件的需求参数与引用资源提供的目标搜索器的目标类型不匹配
                                @ref.ErrorCode = 1312;
                                ErrorComponents.Add(@ref);
                                continue;
                            }
                            condition.AffectComponentIndex = @ref.ReferenceComponent.SelfIndex;
                            condition.RequireParamIndexes.InsertOrUpdateAt(@ref.MultipleRequireIndex, @ref.ReferenceComponent.SelfIndex);
                            continue;
                        }
                        //引用资源指示为影响目标，但提供的不是触发器或目标搜索器
                        @ref.ErrorCode = 1106;
                        ErrorComponents.Add(@ref);
                        continue;
                    }
                    if (@ref.IsParam)
                    {
                        switch (@ref.ReferenceComponent)
                        {
                            case ICESTrigger tri:
                                {
                                    var type = tri.ProvideParamTypes.ElementAtOrDefault(@ref.TriggerParamIndex);
                                    var type2 = condition.RequireParamTypes.ElementAtOrDefault(@ref.MultipleRequireIndex);
                                    if (type == null)
                                    {
                                        //引用资源提供的触发器参数索引越界
                                        @ref.ErrorCode = 1111;
                                        ErrorComponents.Add(@ref);
                                        continue;
                                    }
                                    if (type2 == null)
                                    {
                                        //引用资源提供的需求类型索引越界
                                        @ref.ErrorCode = 1112;
                                        ErrorComponents.Add(@ref);
                                        continue;
                                    }
                                    if (!type2.IsInheritedBy(type))
                                    {
                                        //引用资源提供的触发器参数类型与需求的参数类型不匹配
                                        @ref.ErrorCode = 1113;
                                        ErrorComponents.Add(@ref);
                                        continue;
                                    }
                                    condition.RequireParamIndexes.InsertOrUpdateAt(@ref.MultipleRequireIndex, @ref.TriggerParamIndex);
                                    continue;
                                }
                            case ICESFreeParam fp:
                                {
                                    var type = condition.RequireParamTypes.ElementAtOrDefault(@ref.MultipleRequireIndex);
                                    if (type == null)
                                    {
                                        //引用资源提供的需求类型索引越界
                                        @ref.ErrorCode = 1121;
                                        ErrorComponents.Add(@ref);
                                        continue;
                                    }
                                    if (!type.IsInheritedBy(fp.ProvideParamType))
                                    {
                                        //引用资源提供的自由参数类型与需求的参数类型不匹配
                                        @ref.ErrorCode = 1122;
                                        ErrorComponents.Add(@ref);
                                        continue;
                                    }
                                    condition.RequireParamIndexes.InsertOrUpdateAt(@ref.MultipleRequireIndex, fp.SelfIndex);
                                    continue;
                                }
                            case ICESParamProcessor pp:
                                {
                                    var type = condition.RequireParamTypes.ElementAtOrDefault(@ref.MultipleRequireIndex);
                                    if (type == null)
                                    {
                                        //引用资源提供的需求类型索引越界
                                        @ref.ErrorCode = 1131;
                                        ErrorComponents.Add(@ref);
                                        continue;
                                    }
                                    if (!type.IsInheritedBy(pp.ProvideParamType))
                                    {
                                        //引用资源提供的自由参数类型与需求的参数类型不匹配
                                        @ref.ErrorCode = 1132;
                                        ErrorComponents.Add(@ref);
                                        continue;
                                    }
                                    condition.RequireParamIndexes.InsertOrUpdateAt(@ref.MultipleRequireIndex, pp.SelfIndex);
                                    continue;
                                }
                            default:
                                {
                                    //引用资源指示为参数提供但提供的不是合法的参数提供器
                                    @ref.ErrorCode = 1110;
                                    ErrorComponents.Add(@ref);
                                    continue;
                                }
                        }
                    }
                }
                //总检查
                var affectC = SearchIndexComponent(condition.AffectComponentIndex);
                var count = GetLastParamSelfIndex() + 1;
                if (affectC == null || (affectC is not ICESTrigger && affectC is not ICESTargetSearch))
                {
                    //该条件的影响组件未提供或不是合法的影响组件
                    ErrorComponents.Add(new ComponentReference() { SourceComponent = condition, ReferenceComponent = affectC, IsAffectTarget = true, ErrorCode = 1301 });
                }
                else if (affectC is ICESTargetSearch paramCTS)
                {
                    if (!TargetSearches.Contains(paramCTS))
                    {
                        //条件的该需求参数索引指向目标搜索器，但是该目标搜索器不是当前实例的目标搜索器
                        ErrorComponents.Add(new ComponentReference()
                        { SourceComponent = condition, ReferenceComponent = paramCTS, IsParam = true, ErrorCode = 1323 });
                    }
                    if (condition.RequireParamIndexes.IndexOf(count) is int i && i != 0)
                    {
                        if (!condition.RequireParamTypes[i].IsInheritedBy(paramCTS.ProvideTargetType))
                        {
                            //条件的该需求参数索引指向目标处理器，但是该索引对应的条件的需求参数类型与目标处理器的提供的目标类型不匹配
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = condition, ReferenceComponent = paramCTS, IsParam = true, ErrorCode = 1324 });
                        }
                    }
                    else
                    {
                        //条件的该需求参数索引指向目标处理器，但是该条件的参数需求索引中没有指向该目标处理器提供的目标类型的索引
                        ErrorComponents.Add(new ComponentReference()
                        { SourceComponent = condition, ReferenceComponent = paramCTS, IsParam = true, ErrorCode = 1325 });
                    }
                }
                for (int j = 0; j < condition.RequireParamTypes.Count; j++)
                {
                    if (j >= condition.RequireParamIndexes.Count)
                    {
                        //条件的该需求参数未提供索引
                        ErrorComponents.Add(new ComponentReference() { SourceComponent = condition, IsParam = true, ErrorCode = 1312 });
                        continue;
                    }
                    var paramIndex = condition.RequireParamIndexes[j];
                    if (paramIndex == count)
                    {
                        continue;
                    }
                    var paramC = SearchIndexComponent(paramIndex);
                    if (paramC == null)
                    {
                        //条件的该需求参数索引越界
                        ErrorComponents.Add(new ComponentReference() { SourceComponent = condition, ErrorCode = 1313 });
                        continue;
                    }
                    var type = condition.RequireParamTypes[j];
                    if (paramC is ICESTrigger tg)
                    {
                        if (tg != Trigger)
                        {
                            //条件的该需求参数索引指向触发器，但是该触发器不是当前实例的触发器
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = condition, ReferenceComponent = tg, IsParam = true, TriggerParamIndex = paramIndex, MultipleRequireIndex = paramIndex, ErrorCode = 1331 });
                        }
                        if (paramIndex < tg.ProvideParamTypes.Count)
                        {
                            if (!type.IsInheritedBy(tg.ProvideParamTypes[paramIndex]))
                            {
                                //条件的该需求参数索引指向触发器，但是该索引对应的条件的需求参数类型与触发器提供的参数类型不匹配
                                ErrorComponents.Add(new ComponentReference()
                                { SourceComponent = condition, ReferenceComponent = tg, IsParam = true, TriggerParamIndex = paramIndex, MultipleRequireIndex = paramIndex, ErrorCode = 1332 });
                            }
                        }
                        else
                        {
                            //条件的该需求参数索引指向触发器，但是索引超出该触发器提供的参数类型的数量
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = condition, ReferenceComponent = tg, IsParam = true, TriggerParamIndex = paramIndex, MultipleRequireIndex = paramIndex, ErrorCode = 1333 });
                        }
                        continue;
                    }
                    if (paramC is ICESFreeParam fp)
                    {
                        if (!FreeParams.Contains(fp))
                        {
                            //条件的该需求参数索引指向自由参数，但是该自由参数不是当前实例的自由参数
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = condition, ReferenceComponent = fp, IsParam = true, ErrorCode = 1341 });
                        }
                        if (!type.IsInheritedBy(fp.ProvideParamType))
                        {
                            //条件的该需求参数索引指向自由参数，但是该索引对应的条件的需求参数类型与自由参数提供的参数类型不匹配
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = condition, ReferenceComponent = fp, IsParam = true, ErrorCode = 1342 });
                        }
                        continue;
                    }
                    if (paramC is ICESParamProcessor pp)
                    {
                        if (!ParamProcessors.Contains(pp))
                        {
                            //条件的该需求参数索引指向参数处理器，但是该参数处理器不是当前实例的参数处理器
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = condition, ReferenceComponent = pp, IsParam = true, ErrorCode = 1351 });
                        }
                        if (!type.IsInheritedBy(pp.ProvideParamType))
                        {
                            //条件的该需求参数索引指向参数处理器，但是该索引对应的条件的需求参数类型与参数处理器提供的参数类型不匹配
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = condition, ReferenceComponent = pp, IsParam = true, ErrorCode = 1352 });
                        }
                        continue;
                    }
                }
            }
            else
            {
                //该条件未定义任何引用资源
                ErrorComponents.Add(new ComponentReference() { SourceComponent = condition, ErrorCode = 1103 });
                return;
            }
        }
        public void CheckEffect(ICESEffect effect)
        {
            if (effect == null)
            {
                return;
            }
            if (ComponentReferences.FirstOrDefault(x => x.SourceComponent == effect) is ComponentReference reference)
            {
                var refList = reference.SameSourceReferences;
                var sameTL = reference.GetAllSameTarget();
                if (sameTL.Count > 0)
                {
                    foreach (var st in sameTL)
                    {
                        //引用资源中存在相同指向的资源
                        st.ErrorCode = 9003;
                        ErrorComponents.Add(st);
                    }
                    return;
                }
                foreach (var @ref in new List<ComponentReference>([reference, .. reference.SameSourceReferences]).Distinct())
                {
                    if (@ref.ReferenceComponent == null)
                    {
                        //该效果的引用资源引用组件为空
                        @ref.ErrorCode = 2006;
                        ErrorComponents.Add(@ref);
                        continue;
                    }
                    if (!Contains(@ref.ReferenceComponent))
                    {
                        //该效果的引用资源引用组件未加入此实例
                        @ref.ErrorCode = 2007;
                        ErrorComponents.Add(@ref);
                        continue;
                    }
                    if (@ref.IsParam)
                    {
                        switch (@ref.ReferenceComponent)
                        {
                            case ICESTrigger tri:
                                {
                                    var type = tri.ProvideParamTypes.ElementAtOrDefault(@ref.TriggerParamIndex);
                                    var type2 = effect.RequireParamTypes.ElementAtOrDefault(@ref.MultipleRequireIndex);
                                    if (type == null)
                                    {
                                        //引用资源提供的触发器参数索引越界
                                        @ref.ErrorCode = 1111;
                                        ErrorComponents.Add(@ref);
                                        continue;
                                    }
                                    if (type2 == null)
                                    {
                                        //引用资源提供的需求类型索引越界
                                        @ref.ErrorCode = 1112;
                                        ErrorComponents.Add(@ref);
                                        continue;
                                    }
                                    if (!type2.IsInheritedBy(type))
                                    {
                                        //引用资源提供的触发器参数类型与需求的参数类型不匹配
                                        @ref.ErrorCode = 1113;
                                        ErrorComponents.Add(@ref);
                                        continue;
                                    }
                                    effect.RequireParamIndexes.InsertOrUpdateAt(@ref.MultipleRequireIndex, @ref.TriggerParamIndex);
                                    continue;
                                }
                            case ICESFreeParam fp:
                                {
                                    var type = effect.RequireParamTypes.ElementAtOrDefault(@ref.MultipleRequireIndex);
                                    if (type == null)
                                    {
                                        //引用资源提供的需求类型索引越界
                                        @ref.ErrorCode = 1121;
                                        ErrorComponents.Add(@ref);
                                        continue;
                                    }
                                    if (!type.IsInheritedBy(fp.ProvideParamType))
                                    {
                                        //引用资源提供的自由参数类型与需求的参数类型不匹配
                                        @ref.ErrorCode = 1122;
                                        ErrorComponents.Add(@ref);
                                        continue;
                                    }
                                    effect.RequireParamIndexes.InsertOrUpdateAt(@ref.MultipleRequireIndex, fp.SelfIndex);
                                    continue;
                                }
                            case ICESParamProcessor pp:
                                {
                                    var type = effect.RequireParamTypes.ElementAtOrDefault(@ref.MultipleRequireIndex);
                                    if (type == null)
                                    {
                                        //引用资源提供的需求类型索引越界
                                        @ref.ErrorCode = 1131;
                                        ErrorComponents.Add(@ref);
                                        continue;
                                    }
                                    if (!type.IsInheritedBy(pp.ProvideParamType))
                                    {
                                        //引用资源提供的自由参数类型与需求的参数类型不匹配
                                        @ref.ErrorCode = 1132;
                                        ErrorComponents.Add(@ref);
                                        continue;
                                    }
                                    effect.RequireParamIndexes.InsertOrUpdateAt(@ref.MultipleRequireIndex, pp.SelfIndex);
                                    continue;
                                }
                            default:
                                {
                                    //引用资源指示为参数提供但提供的不是合法的参数提供器
                                    @ref.ErrorCode = 1110;
                                    ErrorComponents.Add(@ref);
                                    continue;
                                }
                        }
                    }
                    if (@ref.IsTarget)
                    {
                        if (@ref.ReferenceComponent is ICESTargetSearch targetSearch)
                        {
                            //var list = new List<ICESTargetSearch>(TargetSearches);
                            //list.Sort();
                            //var index = @ref.ReferenceComponent.SelfIndex;
                            //if (index < 0)
                            //{
                            //    //引用资源提供的目标搜索器不是当前实例的目标搜索器
                            //    @ref.ErrorCode = 2021;
                            //    ErrorComponents.Add(@ref);
                            //    continue;
                            //}
                            var type = effect.RequireTargetTypes.ElementAtOrDefault(@ref.MultipleRequireIndex);
                            if (type == null)
                            {
                                //引用资源提供的需求目标类型索引越界
                                @ref.ErrorCode = 2022;
                                ErrorComponents.Add(@ref);
                                continue;
                            }
                            if (!type.IsInheritedBy(targetSearch.ProvideTargetType))
                            {
                                //引用资源提供的目标搜索器的目标类型与需求的目标类型不匹配
                                @ref.ErrorCode = 2023;
                                ErrorComponents.Add(@ref);
                                continue;
                            }
                            effect.RequireTargetIndexes.InsertOrUpdateAt(@ref.MultipleRequireIndex, targetSearch.SelfIndex);
                            continue;
                        }
                        else
                        {
                            //引用资源指示为目标提供但是指向的不是合法的目标搜索器
                            @ref.ErrorCode = 2012;
                            ErrorComponents.Add(@ref);
                            continue;
                        }

                    }
                }
                //总检查
                for (int j = 0; j < effect.RequireParamTypes.Count; j++)
                {
                    if (j >= effect.RequireParamIndexes.Count)
                    {
                        //效果的该需求参数未提供索引
                        ErrorComponents.Add(new ComponentReference() { SourceComponent = effect, IsParam = true, ErrorCode = 2312 });
                        continue;
                    }
                    var paramIndex = effect.RequireParamIndexes[j];
                    var paramC = SearchIndexComponent(paramIndex);
                    if (paramC == null)
                    {
                        //效果的该需求参数索引越界
                        ErrorComponents.Add(new ComponentReference() { SourceComponent = effect, ErrorCode = 2313 });
                        continue;
                    }
                    var type = effect.RequireParamTypes[j];
                    if (paramC is ICESTrigger tg)
                    {
                        if (tg != Trigger)
                        {
                            //效果的该需求参数索引指向触发器，但是该触发器不是当前实例的触发器
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = effect, ReferenceComponent = tg, IsParam = true, TriggerParamIndex = paramIndex, MultipleRequireIndex = paramIndex, ErrorCode = 2331 });
                        }
                        if (paramIndex < tg.ProvideParamTypes.Count)
                        {
                            if (!type.IsInheritedBy(tg.ProvideParamTypes[paramIndex]))
                            {
                                //效果的该需求参数索引指向触发器，但是该索引对应的效果的需求参数类型与触发器提供的参数类型不匹配
                                ErrorComponents.Add(new ComponentReference()
                                { SourceComponent = effect, ReferenceComponent = tg, IsParam = true, TriggerParamIndex = paramIndex, MultipleRequireIndex = paramIndex, ErrorCode = 2332 });
                            }
                        }
                        else
                        {
                            //效果的该需求参数索引指向触发器，但是索引超出该触发器提供的参数类型的数量
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = effect, ReferenceComponent = tg, IsParam = true, TriggerParamIndex = paramIndex, MultipleRequireIndex = paramIndex, ErrorCode = 2333 });
                        }
                        continue;
                    }
                    if (paramC is ICESFreeParam fp)
                    {
                        if (!FreeParams.Contains(fp))
                        {
                            //效果的该需求参数索引指向自由参数，但是该自由参数不是当前实例的自由参数
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = effect, ReferenceComponent = fp, IsParam = true, ErrorCode = 2341 });
                        }
                        if (!type.IsInheritedBy(fp.ProvideParamType))
                        {
                            //效果的该需求参数索引指向自由参数，但是该索引对应的效果的需求参数类型与自由参数提供的参数类型不匹配
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = effect, ReferenceComponent = fp, IsParam = true, ErrorCode = 2342 });
                        }
                        continue;
                    }
                    if (paramC is ICESParamProcessor pp)
                    {
                        if (!ParamProcessors.Contains(pp))
                        {
                            //效果的该需求参数索引指向参数处理器，但是该参数处理器不是当前实例的参数处理器
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = effect, ReferenceComponent = pp, IsParam = true, ErrorCode = 2351 });
                        }
                        if (!type.IsInheritedBy(pp.ProvideParamType))
                        {
                            //效果的该需求参数索引指向参数处理器，但是该索引对应的效果的需求参数类型与参数处理器提供的参数类型不匹配
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = effect, ReferenceComponent = pp, IsParam = true, ErrorCode = 2352 });
                        }
                        continue;
                    }
                }
                for (int j = 0; j < effect.RequireTargetTypes.Count; j++)
                {
                    if (j >= effect.RequireTargetIndexes.Count)
                    {
                        //效果的该需求目标类型未提供索引
                        ErrorComponents.Add(new ComponentReference() { SourceComponent = effect, IsTarget = true, ErrorCode = 2422 });
                        continue;
                    }
                    var targetIndex = effect.RequireTargetIndexes[j];
                    var targetC = SearchIndexComponent(targetIndex);
                    //var list = new List<ICESTargetSearch>(TargetSearches);
                    //list.Sort();
                    //var targetC = list.ElementAtOrDefault(targetIndex);
                    //if (targetC == null)
                    //{
                    //    //效果的该需求目标类型索引越界
                    //    ErrorComponents.Add(new ComponentReference() { SourceComponent = effect, ErrorCode = 2423 });
                    //    continue;
                    //}
                    var type = effect.RequireTargetTypes[j];
                    if (targetC is ICESTargetSearch ts)
                    {
                        if (!TargetSearches.Contains(ts))
                        {
                            //效果的该需求目标类型索引指向目标搜索器，但是该目标搜索器不是当前实例的目标搜索器
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = effect, ReferenceComponent = ts, IsTarget = true, ErrorCode = 2441 });
                        }
                        if (!type.IsInheritedBy(ts.ProvideTargetType))
                        {
                            //效果的该需求目标类型索引指向目标搜索器，但是该索引对应的效果的需求参数类型与目标搜索器提供的目标类型不匹配
                            ErrorComponents.Add(new ComponentReference()
                            { SourceComponent = effect, ReferenceComponent = ts, IsTarget = true, ErrorCode = 2442 });
                        }
                        continue;
                    }
                    //效果的该需求目标类型索引越界
                    ErrorComponents.Add(new ComponentReference() { SourceComponent = effect, ErrorCode = 2423 });
                    continue;
                }
            }
            else
            {
                //该效果未定义任何引用资源
                ErrorComponents.Add(new ComponentReference() { SourceComponent = effect, ErrorCode = 2005 });
                return;
            }
        }
        public int CheckResult()
        {
            if (InvalidCheck(Trigger))
            {
                //已输入的触发器无效
                return 9001;
            }
            if (InvalidCheck(Activity))
            {
                //已输入的活动无效
                return 9002;
            }
            SelfIndexReflect();
            ReferenceReflect();
            ErrorComponents.Clear();
            for (int i = 0; i < ParamProcessors.Count; i++)
            {
                CheckParamProcessor(ParamProcessors[i]);
            }
            for (int i = 0; i < Conditions.Count; i++)
            {
                CheckCondition(Conditions[i]);
            }
            for (int i = 0; i < Effects.Count; i++)
            {
                CheckEffect(Effects[i]);
            }
            if (ErrorComponents.Count != 0)
            {
                //存在错误的组件
                return -1000;
            }
            return 0;
        }
        public (T, int) GetResult<T>() where T : SingleEffect ,new()
        {
            var result = new T();
            var i = CheckResult();
            if (i == 0)
            {
                FreeParams.Sort();
                ParamProcessors.Sort();
                Conditions.Sort();
                TargetSearches.Sort();
                Effects.Sort();
                result.Trigger = Trigger;
                Trigger.Owner = result;
                result.FreeParams.AddRange([.. FreeParams]);
                FreeParams.ForEach(x => x.Owner = result);
                result.ParamProcessors.AddRange([.. ParamProcessors]);
                ParamProcessors.ForEach(x => x.Owner = result);
                result.Conditions.AddRange([.. Conditions]);
                Conditions.ForEach(x => x.Owner = result);
                result.TargetSearches.AddRange([.. TargetSearches]);
                TargetSearches.ForEach(x => x.Owner = result);
                result.Effects.AddRange([.. Effects]);
                Effects.ForEach(x => x.Owner = result);
                result.Activity = Activity;
                Activity.Owner = result;
            }
            return (result, i);
        }
    }
}
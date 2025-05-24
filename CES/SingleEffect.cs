using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES
{
    public class SingleEffect
    {
        public virtual int ID { get; set; } = 0;
        public ICESTrigger Trigger { get; set; }
        public List<ICESFreeParam> FreeParams { get; } = [];
        public List<ICESParamProcessor> ParamProcessors { get; } = [];
        public List<ICESTargetSearch> TargetSearches { get; } = [];
        public List<ICESCondition> Conditions { get; } = [];
        public List<ICESEffect> Effects { get; } = [];
        public ICESActivity Activity { get; set; }
        public List<ICESComponent> AllComponents => [Trigger, .. FreeParams, .. ParamProcessors, .. TargetSearches, .. Conditions, .. Effects, Activity];
        public virtual ICESAbilityable Owner { get; set; }
        public virtual IDescriptionCombiner DesciptionCombineFunction { get; set; } = null;
        public ICESComponent SearchParamComponentByIndex(int index)
        {
            if (index > -1 && index < Trigger.ProvideParamTypes.Count)
            {
                return Trigger;
            }
            return AllComponents.FirstOrDefault(x=>x.SelfIndex == index);
        }
        public ICESParamable GetFinalParam(ICESComponent component, int paramIndex = 0)
        {
            switch (component)
            {
                case ICESTrigger trigger:
                    return trigger.ProvideParams.ElementAtOrDefault(paramIndex);
                case ICESFreeParam freeParam:
                    return freeParam.TryGetParam();
                case ICESParamProcessor paramProcessor:
                    var param = AllComponents.FirstOrDefault(x => x.SelfIndex == paramProcessor.RequireParamIndex);
                    return paramProcessor.Process(GetFinalParam(param, paramProcessor.RequireParamIndex));
                default:
                    return default;
            }
        }
        public virtual void Triggered()
        {
            if (Activity != null)
            {
                Activity.Action();
            }
        }
        public virtual void Init()
        {
            Trigger?.Init();
            foreach (var freeParam in new List<ICESFreeParam>(FreeParams))
            {
                freeParam?.Init();
            }
            foreach (var paramProcessor in new List<ICESParamProcessor>(ParamProcessors))
            {
                paramProcessor?.Init();
            }
            foreach (var condition in new List<ICESCondition>(Conditions))
            {
                condition?.Init();
            }
            foreach (var targetSearch in new List<ICESTargetSearch>(TargetSearches))
            {
                targetSearch?.Init();
            }
            Activity?.Init();
            foreach (var effect in new List<ICESEffect>(Effects))
            {
                effect?.Init();
            }
        }
        public virtual void Destroy()
        {
            Trigger?.Destroy();
            foreach (var freeParam in new List<ICESFreeParam>(FreeParams))
            {
                freeParam?.Destroy();
            }
            foreach (var paramProcessor in new List<ICESParamProcessor>(ParamProcessors))
            {
                paramProcessor?.Destroy();
            }
            foreach (var condition in new List<ICESCondition>(Conditions))
            {
                condition?.Destroy();
            }
            foreach (var targetSearch in new List<ICESTargetSearch>(TargetSearches))
            {
                targetSearch?.Destroy();
            }
            Activity?.Destroy();
            foreach (var effect in new List<ICESEffect>(Effects))
            {
                effect?.Destroy();
            }
            Trigger = null;
            FreeParams.Clear();
            ParamProcessors.Clear();
            Conditions.Clear();
            TargetSearches.Clear();
            Activity = null;
            Effects.Clear();
        }
        public override string ToString()
        {
            return $"ID : <{ID}>\n" +
                $"Trigger : <{Trigger}>\n" +
                $"FreeParamsCount : <{FreeParams.Count}>\n" +
                $"ParamProcessorsCount : <{ParamProcessors.Count}>\n" +
                $"TargetSearchesCount : <{TargetSearches.Count}>\n" +
                $"ConditionsCount : <{Conditions.Count}>\n" +
                $"EffectsCount : <{Effects.Count}>\n" +
                $"Activity : <{Activity}>";
        }
    }
    public interface ICESAbilityable
    {
        public List<SingleEffect> HandledEffects { get; }

    }
}

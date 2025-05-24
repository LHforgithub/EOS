using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES
{
    public interface IDescribeProcessor
    {
        public string GetMainDescription(ICESComponent component);
        public string GetRequiredParamDescription(ICESComponent component, int index);
        public string GetRequiredTargetDescription(ICESComponent component, int index);
        public string GetProvideParamDescription(ICESComponent component, int index);
        public string GetProvideTargetDescription(ICESComponent component, int index);
        public string ChangeDescription(ICESComponent component, string originalDesc);
    }
    public interface IDescriptionCombiner
    {
        public string CombineDescription<T>(T singleEffect) where T : SingleEffect;
    }
}

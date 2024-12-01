using System.Collections.Generic;

namespace EOS.Tiles
{
    /// <summary>事件参数类型</summary>
    public sealed class EventParams
    {
        /// <summary></summary>
        public EventParams(EOSControler controler, EventCode code, EventParams last = null, uint level = 0, params object[] values)
        {
            Controler = controler;
            Code = code;
            Last = last;
            BroadCastLevel = level;
            Values = new List<object>(values).ToArray();
        }
        /// <summary>主控制器</summary>
        public EOSControler Controler { get; private set; } = null;
        /// <summary>此事件参数用于的事件码</summary>
        public EventCode Code { get; private set; }
        /// <summary>
        /// 上一个事件参数
        /// <para>为<see langword="null"/>如果<see cref="BroadCastLevel"/>为<see langword="1"/>。</para>
        /// </summary>
        public EventParams Last { get; private set; } = null;
        /// <summary>该主控制器的广播层级（总共有多少个EventParams）</summary>
        public uint BroadCastLevel { get; private set; } = 0;
        /// <summary>该广播事件输入的参数</summary>
        public object[] Values { get; } = new object[0];
        /// <summary>
        /// 获取对应的等级的<see cref="EventParams"/>数据。
        /// </summary>
        /// <param name="level"></param>
        /// <returns>返回<see langword="null"/>如果<paramref name="level"/>大于此实例的<see cref="BroadCastLevel"/>。否则为对应的<see cref="EventParams"/>实例。</returns>
        public EventParams GetParams(uint level)
        {
            if (BroadCastLevel == level)
            {
                return this;
            }
            if (level > BroadCastLevel)
            {
                return null;
            }
            return Last?.GetParams(level);
        }
    }
}

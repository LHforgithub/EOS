using EOS.Attributes;

namespace EOS
{
    /// <summary>继承该接口的类等效继承了<see cref="EventCodeAttribute"/>特性，并可用作<see cref="EOSControler.AddNewCode{T}()"/>中的泛型。</summary>
    /// <remarks>注意，与单独使用<see cref="EventCodeAttribute"/>特性不同，<see cref="EventCodeAttribute"/>特性不会被继承，而继承了此接口的类，会自动作为事件码使用，并可能会因为缺少<see cref="EventCodeMethodAttribute"/>特性而报错。但是，继承该类型的子类<see langword="不会"/>被作为事件码使用。</remarks>
    [EventCode]
    public interface IEventCode
    {
    }
}

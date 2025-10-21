using EOS.Attributes;

namespace EOS
{
    /// <summary>继承该接口的类等效继承了<see cref="EventCodeAttribute"/>特性，并可用作<see cref="EOSControler.AddNewCode{T}()"/>中的泛型。</summary>
    /// <remarks>注意，继承该接口的类被继承时，子类也会被作为<see cref="EOS.Tiles.EventCode"/>使用。这与单独使用<see cref="EventCodeAttribute"/>特性不同，<see cref="EventCodeAttribute"/>特性不会被继承。</remarks>
    [EventCode]
    public interface IEventCode
    {
    }
}

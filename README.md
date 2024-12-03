# **EOS 事件系统模块**
***
##### ***目标框架：.Net Framework 4.8.1***

###### 也可以通过修改源代码目标框架来获得更低版本，但可能会有语法问题。理论最低需要 .Net Framework 4.5

***
>这是一个通过C#特性```Attribute```制作的通用事件系统。通过在类与方法上标注特性，可以轻松将其作为委托托管给事件调用。
***
## **核心类型：**

- [EventCodeAttribute](#eventcodeattribute)
- [EventCodeMethodAttribute](#eventcodemethodattribute)
- [EventListenerAttribute](#eventlistenerattribute)
- [EOSManager](#eosmanager)
- [EOSControler](#eoscontroler)
- [IEventCode](#ieventcode)
- [IEventListener](#ieventlistener)

**更多类型**

>参见类型注释
- EventPriorityAttribute
- EventCode
- EventParams
- TempLog

***

### EventCodeAttribute

>只能用于类或接口。该特性不会被继承。

添加至一个类或接口后，相当于以该类或接口的程序集限定名注册了一个事件码。这个类型应当是公开的而非程序集限定或私有的。

该类型内必须同时存在一个继承了[EventCodeMethodAttribute](#eventcodemethodattribute)特性的方法。

该事件的广播方法定义即为继承了该[EventCodeMethodAttribute](#eventcodemethodattribute)特性的方法。

>这意味着，您广播该方法时，需要输入对应该定义方法参数。

完全定义之后，您可以将继承该特性的类型直接作为事件码在[EOSManager](#eosmanager)和[EOSControler](#eoscontroler)类型的方法中使用。

><span id="eventcodeattributeexample">例如</span>：
```C#
[EventCode]
public interface TestInterface
{
	[EventCodeMethod]
	void EventFunction_Define(int i, int nameInt = 10)
}
```
这相当于在程序集中存在一个以```typeof(TestInterface).AssemblyQualifiedName```为Key值的事件，这个事件会广播一个以```int, int```为参数的方法。

***

### EventCodeMethodAttribute

>只能用于方法。单独使用时无意义。该特性不会被继承。

用于在继承了[EventCodeAttribute](#eventcodeattribute)特性的类中声明对应事件的方法。

这个方法会作为该事件的方法的定义，保存对应信息。

广播事件时，会获取该方法参数的默认值，在广播方法传入的参数不足时补充。

尝试添加进该事件的[EventListenerAttribute](#eventlistenerattribute)接收者实例中，必须有用[EventListenerAttribute](#eventlistenerattribute)特性声明了的，
且与该方法相同参数和返回值的对应方法（无需同名）。

虽然您可以为方法定义一个返回类型，但是实际广播的时候您无法获取该返回值。

如果类中有多个添加了此特性的方法，只有第一个方法会被获取作为定义。

>例如：见[EventCodeAttribute](#eventcodeattributeexample)。

> **暂不支持泛型方法**

> 补充：
>
> 对于有ref、out关键词的参数，详见```EOSControler.BroadCast<T>(params object[] values)```中对```values```参数的注释。

***

### EventListenerAttribute

>用作注明类型可为事件的接收者。可以用于类、接口和方法。类应当是公开的

添加至类或接口后，即为声明该类、继承该类的类型或继承该接口的类可以作为事件的具体接收者，调用对应方法。

>可以添加至静态类上。此时，可以将该静态类作为泛型参数调用[EOSManager](#eosmanager)或[EOSControler](#eoscontroler)中的相关方法。

声明类为接收者后，还需要声明该类中哪个方法作为哪个事件的可调用的方法。
该方法需要与事件定义的方法参数类型、位置和返回类型相同，但不限制是公开还是私有成员，也不需要方法名称相同。

>例如：
>
>此时我们假设你已经在[EventCodeAttribute](#eventcodeattributeexample)中声明了一个事件码。
>
>那么，你可以这样来声明这个委托：
>```C#
>[EventListener]
>public class TestClass
>{
>	[EventListener(typeof(TestInterface))]
>	void EventFunction(int i, int defNameInt = 4);
>}
>```
>此时，若您在[EOSManager](#eosmanager)或[EOSControler](#eoscontroler)中的```BroadCast```方法中传入的参数只有一个```int```值，
控制器会将您定义的该事件中的方法参数的默认值```10```传入方法中。所以```EventFunction```会得到```defNameInt = 10```而非```defNameInt = 4```。

对于一个接收者类型，它可以同时存在对应多个事件的方法，但是对应一个事件只能声明一个方法。

>例如：
>
>您可以这样做：
>
>```C#
>[EventListener]
>public class TestClass
>{
>	[EventListener(typeof(Code_1))]
>	void Function_1(int i, string ...);
>
>	[EventListener(typeof(Code_2))]
>	void Function_2(int i, int ...);
>
>	...
>}
>```
>但是不能这样做：
>```C#
>[EventListener]
>public class TestClass
>{
>	[EventListener(typeof(Code))]
>	void Function_1(int i, ...);
>
>	[EventListener(typeof(Code))]
>	void Function_2(int i, ...);
>
>	...
>}
>```
>这将会获得一个错误并记录在TempLog类型中。
>
>


***

### EOSManager

>一个单例，用于获取程序集的[EOSControler](#eoscontroler)类型，或者直接将[EOSControler](#eoscontroler)添加到控制器单例中。

#### 详见文档注释中以下方法注释：

- ```MergeToSingleton()```
- ```MergeToSingleton(Assembly)```
- ```GetNewControler()```
- ```GetNewControler(Assembly)```
- ```AddListener(object)``` 及其重载。
- ```RemoveListener(object)``` 及其重载。
- ```BroadCast(string, params object[])``` 及其重载。


***

### EOSControler

>实际的事件控制器。对事件码和接收者的具体操作都在此类中进行。

包括对事件的添加、移除和广播等方法，控制对一个程序集的事件。通过[EOSManager](#eosmanager)中的```EOSManager.GetNewControler```方法获取。

可以将多个控制器合并。

如果您在某个程序集中调用了另一个程序集中的事件，那么它们将会自动合并至一起。

同时，此类型中的大部分方法在遇到异常时会抛出，可能导致进程中断。

因此如无特殊需求，请尽量使用```EOSManager.MergeToSingleton```方法将控制器合并至单例控制器，然后调用[EOSManager](#eosmanager)中的静态方法。

***

### IEventCode

>一个继承了[EventCodeAttribute](#eventcodeattribute)类型的接口

必须继承该接口才能将类型作为[EOSManager](#eosmanager)或[EOSControler](#eoscontroler)中[EventListener](#eventlistenerattribute)相关方法的泛型参数。

***

### IEventListener

>一个继承了[EventListenerAttribute](#eventlistenerattribute)类型的接口

必须继承该接口才能将类型作为[EOSManager](#eosmanager)或[EOSControler](#eoscontroler)中[EventListener](#eventlistenerattribute)相关方法的泛型参数。

***
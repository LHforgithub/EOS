using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CES
{
    /// <summary>用于查找反射的工具方法合集。</summary>
    internal static class ReflectionTools
    {
        /// <summary>所有检索类型</summary>
        internal static readonly BindingFlags All = BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.GetField
            | BindingFlags.SetField
            | BindingFlags.GetProperty
            | BindingFlags.SetProperty;

        /// <summary>检索不包括继承的成员类型</summary>
        internal static readonly BindingFlags AllDeclared = All | BindingFlags.DeclaredOnly;

        /// <summary>只检索字段的GetSet方法。</summary>
        internal static readonly BindingFlags AllGetSet = BindingFlags.GetField
            | BindingFlags.SetField
            | BindingFlags.GetProperty
            | BindingFlags.SetProperty;

        /// <summary>检索不包括字段的GetSet方法。</summary>
        internal static readonly BindingFlags AllNoGetSet = All & ~AllGetSet;


        /// <summary>获取所有<paramref name="assembly"/>中成功加载的类型</summary>
        /// <param name="assembly">目标程序集</param>
        /// <returns>一个<see cref="Type"/>列表。</returns>
        /// <remarks>
        /// 此函数调用 <see cref="Assembly.GetTypes"/>， 当抛出 <see cref="ReflectionTypeLoadException"/>异常时，
        /// 返回<paramref name="assembly"/>中成功加载的类型
        /// （<see cref="ReflectionTypeLoadException.Types"/>，筛选出所有不为<see langword="null"/>的类型。）。
        /// </remarks>
        internal static Type[] GetSuccessfullyLoadedTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                LogTool.Instance.Log(ex.Message);
                return ex.Types.Where(type => type is not null).ToArray();
            }
        }
        /// <summary>获取<paramref name="type"/>中所有声明的方法，不包括继承的方法。</summary>
		/// <param name="type">声明了方法的类型。</param>
		/// <returns>一个<see cref="MethodInfo"/>集合。如果<paramref name="type"/>为<see langword="null"/>，返回空集合。</returns>
		internal static List<MethodInfo> GetDeclaredMethods(Type type)
        {
            if (type is null)
            {
                LogTool.Instance.Log(new ArgumentNullException(nameof(type)).ToString());
                return new();
            }
            return new(type.GetMethods(AllDeclared));
        }
        /// <summary>获取<paramref name="type"/>中所有声明的方法，不包括字段和属性的<see langword="Get"/>或<see langword="Set"/>方法。</summary>
        /// <param name="type"></param>
        /// <returns>一个<see cref="MethodInfo"/>集合。如果<paramref name="type"/>为<see langword="null"/>，返回空集合。</returns>
        internal static List<MethodInfo> GetNotGetSetMethods(Type type)
        {
            if (type is null)
            {
                LogTool.Instance.Log(new ArgumentNullException(nameof(type)).ToString());
                return new();
            }
            return new(type.GetMethods(AllNoGetSet));
        }

        /// <summary>检查该方法是否未构造函数或存在未定义泛型类型的泛型方法。</summary>
        /// <param name="methodInfo"></param>
        /// <returns>如果是构造函数或是存在未定义的泛型类型，<see langword="true"/>，否则<see langword="false"/></returns>
        internal static bool IsConstructorOrGeneric(MethodInfo methodInfo)
        {
            return methodInfo.IsConstructor || (methodInfo.IsGenericMethod && methodInfo.ContainsGenericParameters);
        }

        /// <summary>检查方法的参数类型数量以及返回值是否与另一个方法相同。</summary>
        /// <returns>
        /// 如果不是构造函数或泛型方法，
        /// 且参数的数量、位置、类型，以及返回的类型完全相同，
        /// 返回<see langword="true"/>。
        /// 否则<see langword="false"/>。
        /// </returns>
        internal static bool IsParamsAndReturnEquelsWith(MethodInfo methodInfoFirst, MethodInfo methodInfoSecound)
        {
            if ((IsConstructorOrGeneric(methodInfoFirst) || IsConstructorOrGeneric(methodInfoSecound))
                && methodInfoFirst != methodInfoSecound)
            {
                return false;
            }
            if (methodInfoFirst.ReturnType != methodInfoSecound.ReturnType)
            {
                return false;
            }
            var parameters = methodInfoFirst.GetParameters();
            var eventParameters = methodInfoSecound.GetParameters();
            if (parameters.Length != eventParameters.Length)
            {
                return false;
            }
            foreach (var parameter in parameters)
            {
                if (eventParameters.FirstOrDefault(x => x.Position == parameter.Position).ParameterType != parameter.ParameterType)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>检索应用于指定成员的指定类型的自定义特性。</summary>
        /// <typeparam name="T">要匹配的特性的类型</typeparam>
        /// <param name="memberInfo">要查找特性的成员</param>
        /// <param name="inherit">是否查找继承的类</param>
        /// <param name="inheritFromInterfaces">是否查找继承的接口。必须<paramref name="inherit"/>为<see langword="true"/>此项才有意义。</param>
        /// <returns>
        /// 与<typeparamref name="T"/>匹配的特性。如果未找到此类特性，则为<see langword="null"/>。
        /// <para>如果<paramref name="inherit"/>与<paramref name="inheritFromInterfaces"/>
        /// 同时为<see langword="true"/>，那么也会查找定义该<paramref name="memberInfo"/>类型所继承的接口中的
        /// 与<typeparamref name="T"/>匹配的特性。</para>
        /// </returns>
        /// <remarks>
        /// 
        /// </remarks>
        internal static T GetCustomAttribute<T>(MemberInfo memberInfo, bool inherit, bool inheritFromInterfaces) where T : Attribute
        {
            if (!inherit)
            {
                return memberInfo.GetCustomAttribute<T>();
            }
            if (inheritFromInterfaces)
            {
                var result = memberInfo.GetCustomAttribute<T>(true);
                if (result is null)
                {
                    if (memberInfo is Type type)
                    {
                        if (type.IsInterface || type.IsClass || type.IsValueType)
                        {
                            foreach (var @interface in type.GetInterfaces())
                            {
                                if (GetCustomAttribute<T>(@interface, true, true) is T attribute)
                                {
                                    result = attribute;
                                    break;
                                }
                            }
                        }
                    }
                    else if ((int)(memberInfo.MemberType & (MemberTypes.Property
                        | MemberTypes.Method | MemberTypes.Field | MemberTypes.Event)) > 0)
                    {
                        foreach (var @interface in memberInfo.ReflectedType.GetInterfaces())
                        {
                            if (@interface.GetMembers(All).FirstOrDefault(member
                                => IsInheritedBy(memberInfo, member)) is MemberInfo info)
                            {
                                if (GetCustomAttribute<T>(info, true, true) is T attribute)
                                {
                                    result = attribute;
                                    break;
                                }
                            }
                        }
                    }
                }
                return result;
            }
            return memberInfo.GetCustomAttribute<T>(true);
        }



        /// <summary>
        /// 判断一个<see cref="MemberInfo"/>是否继承自另一个<see cref="MemberInfo"/>。
        /// 包括继承的接口成员。
        /// </summary>
        /// <param name="original">要判断的成员</param>
        /// <param name="other">假设是从中继承的成员</param>
        /// <returns>
        /// 如果<paramref name="original"/>是继承自<paramref name="other"/>，
        /// 或继承自类或接口类型的<paramref name="other"/>中的成员，为<see langword="true"/>。
        /// 否则为<see langword="false"/>。
        /// </returns>
        /// <remarks>
        /// <b><i><see langword="暂时不支持对构造函数的继承的判断。"/></i></b>
        /// </remarks>
        internal static bool IsInheritedBy(MemberInfo original, MemberInfo other)
        {
            if (original is null || other is null)
            {
                return false;
            }
            if (IsMemberType(original, MemberTypes.TypeInfo | MemberTypes.NestedType))
            {
                if (IsMemberType(other, MemberTypes.TypeInfo | MemberTypes.NestedType))
                {
                    var type = (Type)original;
                    var baseType = type.BaseType;
                    return type == other || IsInheritedBy(baseType, other) || type.GetInterfaces().Any(x => IsInheritedBy(x, other));
                }
                return false;
            }
            if (!IsInheritedBy(original.ReflectedType, other is Type ? other : other.ReflectedType))
            {
                return false;
            }
            if (IsMemberType(other, MemberTypes.TypeInfo | MemberTypes.NestedType))
            {
                return (other as Type).GetMembers(All).Any(x => IsInheritedBy(original, x));
            }
            if (original.MemberType != other.MemberType)
            {
                return false;
            }
            switch (original.MemberType)
            {
                case MemberTypes.Field:
                    {
                        return original.Name == other.Name;
                    }
                case MemberTypes.Property:
                    {
                        var method0 = (original as PropertyInfo).GetGetMethod(true);
                        var method1 = (original as PropertyInfo).GetSetMethod(true);
                        if ((!(method0?.IsVirtual ?? true) || !(method1?.IsVirtual ?? true))
                            && original.ReflectedType != other.ReflectedType)
                        {
                            return false;
                        }
                        return original.Name == other.Name;
                    }
                case MemberTypes.Method:
                    {
                        var method0 = original as MethodInfo;
                        var method1 = other as MethodInfo;
                        if (!method0.IsVirtual && original.ReflectedType != other.ReflectedType)
                        {
                            return false;
                        }
                        var parameters0 = method0.GetParameters();
                        var parameters1 = method1.GetParameters();
                        return original.Name == other.Name && method0.ReturnType == method1.ReturnType
                            && method0.GetGenericArguments().Length == method1.GetGenericArguments().Length
                            && parameters0.All(p => parameters1.Any(x => x.ParameterType == p.ParameterType && x.Position == p.Position));
                    }
                case MemberTypes.Event:
                    {
                        return original == other;
                    }
                case MemberTypes.Constructor:
                    {
                        //没找到好方法对构造函数进行判断，不过一般也用不到。
                        return false;
                    }
                default:
                    {
                        return false;
                    }
            }
        }
        /// <summary>
        /// 判断某个成员是否为对应的<see cref="MemberTypes"/>枚举成员。
        /// </summary>
        /// <returns>如果<see cref="MemberInfo.MemberType"/>与<paramref name="memberTypes"/>对应，为<see langword="true"/>。否则为<see langword="false"/>。</returns>
        internal static bool IsMemberType(MemberInfo memberInfo, MemberTypes memberTypes)
        {
            return (memberInfo.MemberType & memberTypes) > 0;
        }
        /// <summary>返回成员的返回类型。</summary>
        /// <returns>
        /// 如果<paramref name="member"/>是类或接口，返回其自身类型。
        /// <para>如果是事件或方法，返回其返回类型。</para>
        /// <para>如果是字段或属性，返回其定义类型。</para>
        /// </returns>
        internal static Type GetUnderlyingType(MemberInfo member)
        {
            return member.MemberType switch
            {
                MemberTypes.TypeInfo | MemberTypes.NestedType => (Type)member,
                MemberTypes.Event => ((EventInfo)member).EventHandlerType,
                MemberTypes.Field => ((FieldInfo)member).FieldType,
                MemberTypes.Method => ((MethodInfo)member).ReturnType,
                MemberTypes.Property => ((PropertyInfo)member).PropertyType,
                MemberTypes.Constructor => null,
                _ => null
            };
        }
    }

    /// <summary>用于查找反射的工具方法合集。以拓展方式使用。</summary>
    internal static class ReflectionToolsExtensions
    {
        /// <inheritdoc cref="ReflectionTools.GetSuccessfullyLoadedTypes(Assembly)"/>
        internal static Type[] GetSuccessfullyLoadedTypes(this Assembly assembly) => ReflectionTools.GetSuccessfullyLoadedTypes(assembly);

        /// <inheritdoc cref="ReflectionTools.GetDeclaredMethods(Type)"/>
		internal static List<MethodInfo> GetDeclaredMethods(this Type type) => ReflectionTools.GetDeclaredMethods(type);

        /// <inheritdoc cref="ReflectionTools.GetNotGetSetMethods(Type)"/>
        internal static List<MethodInfo> GetNotGetSetMethods(this Type type) => ReflectionTools.GetNotGetSetMethods(type);

        /// <inheritdoc cref="ReflectionTools.IsConstructorOrGeneric"/>
        internal static bool IsConstructorOrGeneric(this MethodInfo methodInfo) => ReflectionTools.IsConstructorOrGeneric(methodInfo);

        /// <inheritdoc cref="ReflectionTools.IsParamsAndReturnEquelsWith"/>
        internal static bool IsParamsAndReturnEquelsWith(this MethodInfo methodInfoFirst, MethodInfo methodInfoSecound) => ReflectionTools.IsParamsAndReturnEquelsWith(methodInfoFirst, methodInfoSecound);

        /// <inheritdoc cref="ReflectionTools.GetCustomAttribute{T}(MemberInfo, bool, bool)"/>
        internal static T GetCustomAttribute<T>(this MemberInfo memberInfo, bool inherit, bool inheritFromInterface) where T : Attribute => ReflectionTools.GetCustomAttribute<T>(memberInfo, inherit, inheritFromInterface);

        /// <inheritdoc cref="ReflectionTools.IsInheritedBy(MemberInfo, MemberInfo)"/>
        internal static bool IsInheritedBy(this MemberInfo original, MemberInfo other) => ReflectionTools.IsInheritedBy(original, other);

        /// <inheritdoc cref="ReflectionTools.IsMemberType(MemberInfo, MemberTypes)"/>
        internal static bool IsMemberType(this MemberInfo memberInfo, MemberTypes memberTypes) => ReflectionTools.IsMemberType(memberInfo, memberTypes);

        /// <inheritdoc cref="ReflectionTools.GetUnderlyingType(MemberInfo)"/>
        internal static Type GetUnderlyingType(this MemberInfo member) => ReflectionTools.GetUnderlyingType(member);
    }
}

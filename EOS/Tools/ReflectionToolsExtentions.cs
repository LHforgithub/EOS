using System;
using System.Collections.Generic;
using System.Reflection;
namespace EOS.Tools
{
    /// <summary>用于查找反射的工具方法合集。以拓展方式使用。</summary>
    public static class ReflectionToolsExtensions
    {
        /// <inheritdoc cref="ReflectionTools.GetSuccessfullyLoadedTypes(Assembly)"/>
        public static Type[] GetSuccessfullyLoadedTypes(this Assembly assembly) => ReflectionTools.GetSuccessfullyLoadedTypes(assembly);

        /// <inheritdoc cref="ReflectionTools.GetDeclaredMethods(Type)"/>
		public static List<MethodInfo> GetDeclaredMethods(this Type type) => ReflectionTools.GetDeclaredMethods(type);

        /// <inheritdoc cref="ReflectionTools.GetNotGetSetMethods(Type)"/>
        public static List<MethodInfo> GetNotGetSetMethods(this Type type) => ReflectionTools.GetNotGetSetMethods(type);

        /// <inheritdoc cref="ReflectionTools.IsConstructorOrGeneric"/>
        public static bool IsConstructorOrGeneric(this MethodInfo methodInfo) => ReflectionTools.IsConstructorOrGeneric(methodInfo);

        /// <inheritdoc cref="ReflectionTools.IsParamsAndReturnEquelsWith"/>
        public static bool IsParamsAndReturnEquelsWith(this MethodInfo methodInfoFirst, MethodInfo methodInfoSecound) => ReflectionTools.IsParamsAndReturnEquelsWith(methodInfoFirst, methodInfoSecound);

        /// <inheritdoc cref="ReflectionTools.GetCustomAttribute{T}(MemberInfo, bool, bool)"/>
        public static T GetCustomAttribute<T>(this MemberInfo memberInfo, bool inherit, bool inheritFromInterface) where T : Attribute => ReflectionTools.GetCustomAttribute<T>(memberInfo, inherit, inheritFromInterface);

        /// <inheritdoc cref="ReflectionTools.IsInheritedBy(MemberInfo, MemberInfo)"/>
        public static bool IsInheritedBy(this MemberInfo original, MemberInfo other) => ReflectionTools.IsInheritedBy(original, other);

        /// <inheritdoc cref="ReflectionTools.IsMemberType(MemberInfo, MemberTypes)"/>
        public static bool IsMemberType(this MemberInfo memberInfo, MemberTypes memberTypes) => ReflectionTools.IsMemberType(memberInfo, memberTypes);

        /// <inheritdoc cref="ReflectionTools.GetUnderlyingType(MemberInfo)"/>
        public static Type GetUnderlyingType(this MemberInfo member) => ReflectionTools.GetUnderlyingType(member);
    }
}

using System;

namespace WorldOfTheThreeKingdoms.GameGlobal;

/// <summary>
/// 标记需要生成 UI 属性访问器的类
/// 源生成器会扫描带有此特性的类，自动生成静态访问方法替代反射
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class GenerateUIAccessorAttribute : Attribute
{
}

using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace GetSkipper.NUnit;

/// <summary>
/// Assembly-level attribute that activates the <see cref="SkipperTestAction"/>
/// for every test in the assembly without requiring per-class or per-method annotation.
///
/// <para>
/// This attribute is added automatically by the package via MSBuild
/// (see <c>GetSkipper.NUnit.props</c> in the package).
/// You can also add it manually in <c>AssemblyInfo.cs</c> if preferred:
/// </para>
/// <code>
/// [assembly: SkipperAssembly]
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class SkipperAssemblyAttribute : Attribute, ITestAction
{
    private readonly SkipperTestAction _action = new();

    public ActionTargets Targets => ActionTargets.Test;

    public void BeforeTest(ITest test) => _action.BeforeTest(test);
    public void AfterTest(ITest test) => _action.AfterTest(test);
}

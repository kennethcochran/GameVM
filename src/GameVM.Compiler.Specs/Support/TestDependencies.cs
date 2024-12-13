using TechTalk.SpecFlow;
using BoDi;

namespace GameVM.Compiler.Specs.Support
{
    [Binding]
    public class TestDependencies
    {
        [BeforeScenario]
        public static void RegisterDependencies(IObjectContainer container)
        {
            // Register our test context as a singleton
            container.RegisterTypeAs<CompilerTestContext, CompilerTestContext>();
        }
    }
}

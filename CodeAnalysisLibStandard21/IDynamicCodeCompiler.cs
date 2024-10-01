using System.Reflection;

namespace CodeAnalysisLibStandard21
{
    public interface IDynamicCodeCompiler
    {
        bool Compile();
        void Execute();
    }
}
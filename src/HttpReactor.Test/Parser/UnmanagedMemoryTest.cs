using HttpReactor.Parser;
using NUnit.Framework;

namespace HttpReactor.Test.Parser
{
    [TestFixture]
    internal sealed class UnmanagedMemoryTest
    {
        [Test]
        public void MultipleDispose()
        {
            var memory = new UnmanagedMemory(128);
            memory.Dispose();
            memory.Dispose();
        }
    }
}

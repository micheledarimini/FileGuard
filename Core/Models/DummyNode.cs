using System.Windows.Threading;

namespace FileGuard.Core.Models
{
    internal class DummyNode : FileSystemNode
    {
        public override bool IsDirectory => false;
        
        public DummyNode() : base(string.Empty) { }
    }
}

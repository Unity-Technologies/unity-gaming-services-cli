using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.ModuleTemplate.Authoring.Core.IO
{
    public interface IFileSystem //Abstracted away - delete
    {
        Task<string> ReadAllText(
            string path,
            CancellationToken token = default(CancellationToken));

        Task WriteAllText(
            string path,
            string contents,
            CancellationToken token = default(CancellationToken));

        Task Delete(string path, CancellationToken token = default(CancellationToken));
    }
}

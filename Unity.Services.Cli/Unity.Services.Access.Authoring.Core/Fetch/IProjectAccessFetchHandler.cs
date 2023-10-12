using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Access.Authoring.Core.Model;
using Unity.Services.Access.Authoring.Core.Results;

namespace Unity.Services.Access.Authoring.Core.Fetch
{
    public interface IProjectAccessFetchHandler
    {
        public Task<FetchResult> FetchAsync(
            string rootDirectory,
            IReadOnlyList<IProjectAccessFile> files,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default);
    }
}

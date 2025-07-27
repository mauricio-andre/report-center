using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportCenter.Common.Providers.Storage.Interfaces;

public interface IStorageService
{
    Task SaveAsync(string fullFileName, Stream content, string contentType, CancellationToken cancellationToken = default);
}

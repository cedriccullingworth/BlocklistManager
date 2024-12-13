using System;
using System.Collections.Generic;
using System.IO;

using BlocklistManager.Classes;
using BlocklistManager.Models;

namespace BlocklistManager.Interfaces;

public interface IDataTranslator : IDisposable
{
    public List<CandidateEntry> TranslateFileData( RemoteSite site, string data );

    public List<CandidateEntry> TranslateDataStream( RemoteSite site, Stream dataStream );
}

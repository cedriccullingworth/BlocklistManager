using System;
using System.Collections.Generic;
using System.IO;

using BlocklistManager.Classes;
using BlocklistManager.Models;

namespace BlocklistManager.Interfaces;

public interface IDataTranslator : IDisposable
{
    List<CandidateEntry> TranslateFileData( RemoteSite site, string data, string fileName );

    List<CandidateEntry> TranslateDataStream( RemoteSite site, Stream dataStream, string fileName );
}

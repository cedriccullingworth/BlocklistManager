using System;
using System.Collections.Generic;

namespace BlocklistManager.Classes;

internal class PathInfo
{
    internal PathInfo( string absoluteUri, bool isDir )
    {
        AbsoluteUrl = new Uri( absoluteUri );
        IsDir = isDir;
        Children = [];
    }

    internal Uri AbsoluteUrl { get; set; }

    internal string AbsoluteUrlStr
    {
        get { return AbsoluteUrl.ToString( ); }
    }

    internal string RootUrl
    {
        get { return AbsoluteUrl.GetLeftPart( UriPartial.Authority ); }
    }

    internal string RelativeUrl
    {
        get { return AbsoluteUrl.PathAndQuery; }
    }

    internal string Query
    {
        get { return AbsoluteUrl.Query; }
    }

    internal bool IsDir { get; set; }
    internal List<PathInfo> Children { get; set; }


    public override string ToString( )
    {
        return string.Format( "{0} IsDir {1} ChildCount {2} AbsUrl {3}", RelativeUrl, IsDir, Children.Count, AbsoluteUrlStr );
    }
}

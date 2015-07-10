using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Optimization;

public class StyleImagePathBundle : Bundle
{
    public StyleImagePathBundle( string virtualPath ) : base( virtualPath )
    {
        base.Transforms.Add(new CssMinify());
    }

    public StyleImagePathBundle( string virtualPath, string cdnPath ) : base( virtualPath )
    {
        base.Transforms.Add( new CssMinify() );
    }

    private static string RelativeFromAbsolutePath( HttpContextBase context, string path )
    {
        var request = context.Request;
        var applicationPath = request.PhysicalApplicationPath;
        var virtualDir = request.ApplicationPath;
        virtualDir = virtualDir == "/" ? virtualDir : ( virtualDir + "/" );
        return path.Replace( applicationPath, virtualDir ).Replace( @"\", "/" );
    }

    /// <summary>
    /// Test
    /// </summary>
    /// <param name="virtualPaths"></param>
    /// <returns></returns>
    public new Bundle Include( params string[] virtualPaths )
    {
        if ( HttpContext.Current.IsDebuggingEnabled )
        {
            // Debugging. Bundling will not occur so act normal and no one gets hurt.
            base.Include( virtualPaths.ToArray() );
            return this;
        }

        // In production mode so CSS will be bundled. Correct image paths.
        var bundlePaths = new List<string>();
        var svr = HttpContext.Current.Server;
        foreach ( var path in virtualPaths )
        {
            var pattern = new Regex( @"url\s*\(\s*([""']?)([^:)]+)\1\s*\)", RegexOptions.IgnoreCase );
            var contents = IO.File.ReadAllText( svr.MapPath( path ) );
            if ( !pattern.IsMatch( contents ) )
            {
                bundlePaths.Add( path );
                continue;
            }

            var bundlePath = ( IO.Path.GetDirectoryName( path ) ?? string.Empty ).Replace( @"\", "/" ) + "/";
            var bundleUrlPath = VirtualPathUtility.ToAbsolute( bundlePath );
            var bundleFilePath = String.Format( "{0}{1}.bundle{2}",
                                               bundlePath,
                                               IO.Path.GetFileNameWithoutExtension( path ),
                                               IO.Path.GetExtension( path ) );
            contents = pattern.Replace( contents, "url($1" + bundleUrlPath + "$2$1)" );
            IO.File.WriteAllText( svr.MapPath( bundleFilePath ), contents );
            bundlePaths.Add( bundleFilePath );
        }
        base.Include( bundlePaths.ToArray() );
        return this;
    }

}
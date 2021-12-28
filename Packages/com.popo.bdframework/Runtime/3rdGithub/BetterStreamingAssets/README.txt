README - Better Streaming Assets
--------------------------------
Many thanks for downloading!  Before getting your hands on the tool please have a read.

Better Streaming Assets is a plugin that lets you access Streaming Assets directly in an uniform and
thread-safe way, with neglectible overhead. Mostly beneficial for Android projects, where the 
alternatives are to use archaic and hugely inefficient WWW or embed data in Asset Bundles. API is 
based on System.IO.File and System.IO.Directory classes.

Contact / Support: 
------------------
Support/Feedback: support@dmprog.pl
Twitter:          @gwiazdorrr
Support Page:     http://dmprog.pl/unity-plugins/

Note on Android & App Bundles
------------------
App Bundles (.aab) builds are bugged when it comes to Streaming Assets. See https://github.com/gwiazdorrr/BetterStreamingAssets/issues/10 for details. The bottom line is:
!!! Keep all file names in Streaming Assets lowercase! !!!

Usage:
------
Check examples below. Note that all the paths are relative to StreamingAssets directory. That is, if you have files

    <project>/Assets/StreamingAssets/foo.bar
    <project>/Assets/StreamingAssets/dir/foo.bar

You are expected to use following paths:

    foo.bar (or /foo.bar)
    dir/foo.bar (or /dir/foo.bar)

Examples:
---------
Initialization (before first use, needs to be called on main thread):

    BetterStreaminAssets.Initialize();

Typical scenario, deserializing from Xml:

    public static Foo ReadFromXml(string path)
    {
        if ( !BetterStreamingAssets.FileExists(path) )
        {
            Debug.LogErrorFormat("Streaming asset not found: {0}", path);
            return null;
        }

        using ( var stream = BetterStreamingAssets.OpenRead(path) )
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Foo));
            return (Foo)serializer.Deserialize(stream);
        }
    }

Note that ReadFromXml can be called from any thread, as long as Foo's constructor doesn't make any 
UnityEngine calls.

Listing all Streaming Assets in with .xml extension:

    // all the xmls
    string[] paths = BetterStreamingAssets.GetFiles("\", "*.xml", SearchOption.AllDirectories); 
    // just xmls in Config directory (and nested)
    string[] paths = BetterStreamingAssets.GetFiles("Config", "*.xml", SearchOption.AllDirectories); 

Checking if a directory exists:

    Debug.Asset( BetterStreamingAssets.DirectoryExists("Config") );

Ways of reading a file:

    // all at once
    byte[] data = BetterStreamingAssets.ReadAllBytes("Foo/bar.data");
    
    // as stream, last 10 bytes
    byte[] footer = new byte[10];
    using (var stream = BetterStreamingAssets.OpenRead("Foo/bar.data"))
    {
        stream.Seek(footer.Length, SeekOrigin.End);
        stream.Read(footer, 0, footer.Length);
    }
    
Asset bundles (again, main thread only):

    // synchronous
    var bundle = BetterStreamingAssets.LoadAssetBundle(path);
    // async
    var bundleOp = BetterStreamingAssets.LoadAssetBundleAsync(path);

    
Legal Stuff / Licensing:
------------------------
Code uses MIT license, as follows:

The MIT License(MIT)

Copyright(c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. 
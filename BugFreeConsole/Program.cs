//
// Program.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Reflection;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BugFreeLibrary;

namespace BugFreeConsole
{
	class Program
	{
		static void Main (string[] args)
		{
			string path = args.Length > 0 ? args[0] : GetPackagePath ();
			Console.WriteLine (path);
			PushPackage (path);
		}

		static string GetPackagePath ()
		{
			var thisPath = Path.GetDirectoryName (Path.GetDirectoryName (Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location)));
			var systemPath = Path.Combine (Path.GetDirectoryName (thisPath), "BugFreeSystem");
			return Path.Combine (systemPath, "Baulig.MartinsPlayground.0.1.0.nupkg");
		}

		internal static Guid GetKey ()
		{
			var path = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			var file = Path.Combine (path, ".mono", "martin", "nuget-key.txt");
			var text = File.ReadAllText (file);
			return Guid.Parse (text);
		}

		public static void PushPackage (string path)
		{
			var apiKey = GetKey ();

			var source = "https://www.nuget.org/api/v2/package";
			var result = PushPackage (source, apiKey.ToString (), path).Result;
			if (!result)
				Console.WriteLine ("FAILED!");
		}

		static async Task<bool> PushPackage (string source, string apiKey, string packagePath)
		{
			var request = WebRequest.CreateHttp (source);
			request.Method = "PUT";
			request.SendChunked = true;

			request.Headers.Add ("X-NuGet-ApiKey", apiKey);
			request.Headers.Add ("X-NuGet-Client-Version", "4.3.0");

			var content = new MultipartFormDataContent ();
			var fileStream = new FileStream (packagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			var packageContent = new StreamContent (fileStream);
			packageContent.Headers.ContentType = MediaTypeHeaderValue.Parse ("application/octet-stream");

			//"package" and "package.nupkg" are random names for content deserializing
			//not tied to actual package name.
			content.Add (packageContent, "package", "package.nupkg");

			request.ContentType = content.Headers.ContentType.ToString ();
			request.ContentLength = content.Headers.ContentLength.Value;

			Stream requestStream = request.GetRequestStream ();
			var data = await content.ReadAsByteArrayAsync ();
			await requestStream.WriteAsync (data, 0, data.Length);

			var response = (HttpWebResponse)await request.GetResponseAsync ();

			if (response.StatusCode != HttpStatusCode.OK) {
				Console.WriteLine ($"Failed with status code {response.StatusCode}");
				return false;
			}

			return true;
		}
	}
}

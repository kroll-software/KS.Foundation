using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.IO;

namespace KS.Foundation 
{
	public static class Compression 
	{
		public static string CompressString(string str) {
			var bytes = Encoding.UTF8.GetBytes(str);
			using (var msi = new MemoryStream(bytes)) {
				using (var mso = new MemoryStream()) {
					using (var gs = new GZipStream(mso, CompressionMode.Compress)) {
						CopyTo(msi, gs);
					}
					return Convert.ToBase64String(mso.ToArray());
				}
			}
		}

		public static string UnCompressString(string str) {
			byte[] bytes = Convert.FromBase64String(str);
			using (var msi = new MemoryStream(bytes))
			using (var mso = new MemoryStream()) {
				using (var gs = new GZipStream(msi, CompressionMode.Decompress)) {
					CopyTo(gs, mso);
				}
				return Encoding.UTF8.GetString(mso.ToArray());
			}
		}

		private static void CopyTo(Stream src, Stream dest) {
			byte[] bytes = new byte[4096];
			int cnt;
			while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0) {
				dest.Write(bytes, 0, cnt);
			}
		}
	}
}
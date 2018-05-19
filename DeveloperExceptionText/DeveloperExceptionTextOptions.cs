using Microsoft.Extensions.FileProviders;

namespace DeveloperExceptionText
{
	public sealed class DeveloperExceptionTextOptions
	{
		public int SourceCodeLineCount { get; set; } = 6;
		public IFileProvider FileProvider { get; set; }
	}
}

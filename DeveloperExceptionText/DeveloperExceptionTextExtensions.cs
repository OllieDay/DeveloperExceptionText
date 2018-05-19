using Microsoft.AspNetCore.Builder;

namespace DeveloperExceptionText
{
	public static class DeveloperExceptionTextExtensions
	{
		public static IApplicationBuilder UseDeveloperExceptionText(this IApplicationBuilder @this)
		{
			return @this.UseMiddleware<DeveloperExceptionTextMiddleware>();
		}
	}
}

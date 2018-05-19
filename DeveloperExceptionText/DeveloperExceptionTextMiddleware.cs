using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using DeveloperExceptionText.Microsoft.Extensions.StackTrace.Sources;
using Microsoft.Extensions.Logging;

namespace DeveloperExceptionText
{
	public sealed class DeveloperExceptionTextMiddleware
	{
		private const string Indentation = "   ";

		private readonly RequestDelegate _next;
		private readonly DeveloperExceptionTextOptions _options;
		private readonly ILogger _logger;
		private readonly IFileProvider _fileProvider;
		private readonly ExceptionDetailsProvider _exceptionDetailsProvider;

		public DeveloperExceptionTextMiddleware(RequestDelegate next, IOptions<DeveloperExceptionTextOptions> options, ILoggerFactory loggerFactory, IHostingEnvironment hostingEnvironment)
		{
			_next = next;
			_options = options.Value;
			_logger = loggerFactory.CreateLogger<DeveloperExceptionTextMiddleware>();
			_fileProvider = _options.FileProvider ?? hostingEnvironment.ContentRootFileProvider;
			_exceptionDetailsProvider = new ExceptionDetailsProvider(_fileProvider, _options.SourceCodeLineCount);
		}

		public async Task Invoke(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (Exception e1)
			{
				_logger.LogError(new EventId(1, "UnhandledException"), e1, "An unhandled exception has occurred while executing the request.");

				if (context.Response.HasStarted)
				{
					_logger.LogWarning(new EventId(2, "ResponseStarted"), "The response has already started, the error text middleware will not be executed.");

					throw;
				}

				try
				{
					await SetResponse(context, e1);
				}
				catch (Exception e2)
				{
					// If there's a Exception while generating the error text, re-throw the original exception.
					_logger.LogError(new EventId(3, "DisplayErrorTextException"), e2, "An exception was thrown attempting to display the error text.");
				}

				throw;
			}
		}

		private async Task SetResponse(HttpContext context, Exception e)
		{
			context.Response.Clear();
			context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

			await context.Response.WriteAsync(CreateExceptionText(context, e));
		}

		private string CreateExceptionText(HttpContext context, Exception e)
		{
			var text = new StringBuilder();

			AppendTitle(text);
			AppendException(text, e);
			AppendQuery(text, context.Request.Query);
			AppendCookies(text, context.Request.Cookies);
			AppendHeaders(text, context.Request.Headers);
			AppendRaw(text, e);

			return text.ToString();
		}

		private static void AppendTitle(StringBuilder text)
		{
			text.AppendLine("An unhandled exception occurred while processing the request.");
			text.AppendLine();
		}

		private void AppendException(StringBuilder text, Exception e)
		{
			var exceptionDetails = _exceptionDetailsProvider.GetDetails(e);

			foreach (var exceptionDetail in exceptionDetails)
			{
				AppendExceptionDetail(text, exceptionDetail);
			}

			text.AppendLine();
		}

		private static void AppendExceptionDetail(StringBuilder text, ExceptionDetails exceptionDetail)
		{
			AppendExceptionTypeAndMessage(text, exceptionDetail);
			AppendStackFrames(text, exceptionDetail.StackFrames);
		}

		private static void AppendExceptionTypeAndMessage(StringBuilder text, ExceptionDetails exceptionDetail)
		{
			text.Append(exceptionDetail.Error.GetType().Name);
			text.Append(": ");
			text.AppendLine(exceptionDetail.Error.Message);
		}

		private static void AppendStackFrames(StringBuilder text, IEnumerable<StackFrameSourceCodeInfo> stackFrames)
		{
			var firstFrame = stackFrames.FirstOrDefault();

			text.Append(Indentation);

			if (firstFrame == null)
			{
				text.Append("Unknown location");
			}
			else
			{
				AppendStackFrameLocation(text, firstFrame);
			}

			text.AppendLine();
		}

		private static void AppendStackFrameLocation(StringBuilder text, StackFrameSourceCodeInfo stackFrame)
		{
			text.Append(stackFrame.Function);

			if (!string.IsNullOrEmpty(stackFrame?.File))
			{
				text.Append(" in ");
				text.Append(Path.GetFileName(stackFrame.File));
			}
		}

		private static void AppendMessage(StringBuilder text, string message)
		{
			text.AppendLine(message);
			text.AppendLine();
		}

		private static void AppendQuery(StringBuilder text, IQueryCollection query)
		{
			AppendCollection(text, "Query", query.ToDictionary(x => x.Key, x => x.Value.Cast<string>()), "=");
		}

		private static void AppendCookies(StringBuilder text, IRequestCookieCollection cookies)
		{
			AppendCollection(text, "Cookies", cookies, "=");
		}

		private static void AppendHeaders(StringBuilder text, IHeaderDictionary headers)
		{
			AppendCollection(text, "Headers", headers.ToDictionary(x => x.Key, x => x.Value.Cast<string>()), ": ");
		}

		private static void AppendCollection(StringBuilder text, string title, IEnumerable<KeyValuePair<string, IEnumerable<string>>> collection, string delimiter)
		{
			text.AppendLine(title);

			foreach (var item in collection)
			{
				foreach (var value in item.Value)
				{
					AppendItem(text, item.Key, value, delimiter);
				}
			}

			text.AppendLine();
		}

		private static void AppendCollection(StringBuilder text, string title, IEnumerable<KeyValuePair<string, string>> collection, string delimiter)
		{
			text.AppendLine(title);

			foreach (var item in collection)
			{
				AppendItem(text, item.Key, item.Value, delimiter);
			}

			text.AppendLine();
		}

		private static void AppendItem(StringBuilder text, string name, string value, string delimiter)
		{
			text.Append(Indentation);
			text.Append(name);
			text.Append(delimiter);
			text.Append(value);
			text.AppendLine();
		}

		private static void AppendRaw(StringBuilder text, Exception e)
		{
			text.AppendLine(e.ToString());
		}
	}
}

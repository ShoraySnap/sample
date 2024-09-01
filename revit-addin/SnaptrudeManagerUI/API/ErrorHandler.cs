using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using SnaptrudeManagerUI;

public static class ErrorHandler
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void HandleException(Exception ex)
    {
        switch (ex)
        {
            case HttpRequestException httpRequestException:
                HandleHttpRequestException(httpRequestException);
                break;

            case TaskCanceledException taskCanceledException:
                HandleTaskCanceledException(taskCanceledException);
                break;

            case TimeoutException timeoutException:
                HandleTimeoutException(timeoutException);
                break;

            case JsonSerializationException jsonSerializationException:
                HandleJsonSerializationException(jsonSerializationException);
                break;

            case ArgumentNullException argumentNullException:
                HandleArgumentNullException(argumentNullException);
                break;

            case ArgumentException argumentException:
                HandleArgumentException(argumentException);
                break;

            case InvalidOperationException invalidOperationException:
                HandleInvalidOperationException(invalidOperationException);
                break;

            case UnauthorizedAccessException unauthorizedAccessException:
                HandleUnauthorizedAccessException(unauthorizedAccessException);
                break;

            case NotSupportedException notSupportedException:
                HandleNotSupportedException(notSupportedException);
                break;

            case IOException ioException:
                HandleIOException(ioException);
                break;

            case AggregateException aggregateException:
                HandleAggregateException(aggregateException);
                break;

            default:
                HandleGeneralException(ex);
                break;
        }
    }

    private static void HandleHttpRequestException(HttpRequestException ex)
    {
        Logger.Error("Error on upload to Snaptrude: " + ex.StackTrace);
        App.OnUploadIssue.Invoke("A network error occurred while connecting to Snaptrude. Please check your internet connection and try again.");
    }

    private static void HandleTaskCanceledException(TaskCanceledException ex)
    {
        Logger.Error("Error on upload to Snaptrude: " + ex.StackTrace);
        App.OnUploadIssue.Invoke("The request to Snaptrude timed out. Please try again later.");
    }

    private static void HandleTimeoutException(TimeoutException ex)
    {
        Logger.Error("Error on upload to Snaptrude: " + ex.StackTrace);
        App.OnUploadIssue.Invoke("An operation timed out. Please try again.");
    }

    private static void HandleJsonSerializationException(JsonSerializationException ex)
    {
        Logger.Error("Error on upload to Snaptrude: " + ex.StackTrace);
        App.OnUploadIssue.Invoke("There was an error processing the data. Please contact support.");
    }

    private static void HandleArgumentNullException(ArgumentNullException ex)
    {
        Logger.Error("Error on upload to Snaptrude: " + ex.StackTrace);
        App.OnUploadIssue.Invoke("A required input was missing. Please check the input and try again.");
    }

    private static void HandleArgumentException(ArgumentException ex)
    {
        Logger.Error("Error on upload to Snaptrude: " + ex.StackTrace);
        App.OnUploadIssue.Invoke("An input value is invalid. Please check the input and try again.");
    }

    private static void HandleInvalidOperationException(InvalidOperationException ex)
    {
        Logger.Error("Error on upload to Snaptrude: " + ex.StackTrace);
        App.OnUploadIssue.Invoke("The operation is not valid in the current state. Please try again.");
    }

    private static void HandleUnauthorizedAccessException(UnauthorizedAccessException ex)
    {
        Logger.Error("Error on upload to Snaptrude: " + ex.StackTrace);
        App.OnUploadIssue.Invoke("Access denied. Please check your credentials and try again.");
    }

    private static void HandleNotSupportedException(NotSupportedException ex)
    {
        Logger.Error("Error on upload to Snaptrude: " + ex.StackTrace);
        App.OnUploadIssue.Invoke("An unsupported operation was attempted. Please contact support.");
    }

    private static void HandleIOException(IOException ex)
    {
        Logger.Error("Error on upload to Snaptrude: " + ex.StackTrace);
        App.OnUploadIssue.Invoke("There was an input/output error. Please check your file system or network and try again.");
    }

    private static void HandleAggregateException(AggregateException ex)
    {
        Logger.Error("Error on upload to Snaptrude: " + ex.StackTrace);
        App.OnUploadIssue.Invoke("Multiple errors occurred. Please review the logs for more details.");

        foreach (var innerException in ex.InnerExceptions)
        {
            HandleException(innerException); // Recursively handle each exception
        }
    }

    private static void HandleGeneralException(Exception ex)
    {
        Logger.Error("Error on upload to Snaptrude: " + ex.StackTrace);
        App.OnUploadIssue.Invoke("An unexpected error occurred. Please try again or contact support if the issue persists.");
    }
}
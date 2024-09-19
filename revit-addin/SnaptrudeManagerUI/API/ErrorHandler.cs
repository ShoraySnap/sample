using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace SnaptrudeManagerUI.API
{
    public static class ErrorHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static void HandleException(Exception ex, Action<string> warningAction)
        {
            switch (ex)
            {
                case HttpRequestException httpRequestException:
                    HandleHttpRequestException(httpRequestException, warningAction);
                    break;

                case TaskCanceledException taskCanceledException:
                    HandleTaskCanceledException(taskCanceledException, warningAction);
                    break;

                case TimeoutException timeoutException:
                    HandleTimeoutException(timeoutException, warningAction);
                    break;

                case JsonSerializationException jsonSerializationException:
                    HandleJsonSerializationException(jsonSerializationException, warningAction);
                    break;

                case ArgumentNullException argumentNullException:
                    HandleArgumentNullException(argumentNullException, warningAction);
                    break;

                case ArgumentException argumentException:
                    HandleArgumentException(argumentException, warningAction);
                    break;

                case InvalidOperationException invalidOperationException:
                    HandleInvalidOperationException(invalidOperationException, warningAction);
                    break;

                case UnauthorizedAccessException unauthorizedAccessException:
                    HandleUnauthorizedAccessException(unauthorizedAccessException, warningAction);
                    break;

                case NotSupportedException notSupportedException:
                    HandleNotSupportedException(notSupportedException, warningAction);
                    break;

                case IOException ioException:
                    HandleIOException(ioException, warningAction);
                    break;

                case AggregateException aggregateException:
                    HandleAggregateException(aggregateException, warningAction);
                    break;

                case InvalidTokenException invalidTokenException:
                    HandleInvalidTokenException(invalidTokenException, warningAction);
                    break;

                case NoInternetException noInternetException:
                    HandleNoInternetException(noInternetException, warningAction);
                    break;

                case SnaptrudeDownException snaptrudeDownException:
                    HandleSnaptrudeDownException(snaptrudeDownException, warningAction);
                    break;

                default:
                    HandleGeneralException(ex, warningAction);
                    break;
            }
        }

        private static void HandleHttpRequestException(HttpRequestException ex, Action<string> warningAction)
        {
            Logger.Error($"Error in {warningAction.Method.Name}: " + ex.StackTrace);
            warningAction.Invoke("A network error occurred while connecting to Snaptrude. Please check your internet connection and try again.");
        }

        private static void HandleTaskCanceledException(TaskCanceledException ex, Action<string> warningAction)
        {
            Logger.Error($"Error in {warningAction.Method.Name}: " + ex.StackTrace);
            warningAction.Invoke("The request to Snaptrude timed out. Please try again later.");
        }

        private static void HandleTimeoutException(TimeoutException ex, Action<string> warningAction)
        {
            Logger.Error($"Error in {warningAction.Method.Name}: " + ex.StackTrace);
            warningAction.Invoke("An operation timed out. Please try again.");
        }

        private static void HandleJsonSerializationException(JsonSerializationException ex, Action<string> warningAction)
        {
            Logger.Error($"Error in {warningAction.Method.Name}: " + ex.StackTrace);
            warningAction.Invoke("There was an error processing the data. Please contact support.");
        }

        private static void HandleArgumentNullException(ArgumentNullException ex, Action<string> warningAction)
        {
            Logger.Error($"Error in {warningAction.Method.Name}: " + ex.StackTrace);
            warningAction.Invoke("A required input was missing. Please check the input and try again.");
        }

        private static void HandleArgumentException(ArgumentException ex, Action<string> warningAction)
        {
            Logger.Error($"Error in {warningAction.Method.Name}: " + ex.StackTrace);
            warningAction.Invoke("An input value is invalid. Please check the input and try again.");
        }

        private static void HandleInvalidOperationException(InvalidOperationException ex, Action<string> warningAction)
        {
            Logger.Error($"Error in {warningAction.Method.Name}: " + ex.StackTrace);
            warningAction.Invoke("The operation is not valid in the current state. Please try again.");
        }

        private static void HandleUnauthorizedAccessException(UnauthorizedAccessException ex, Action<string> warningAction)
        {
            Logger.Error($"Error in {warningAction.Method.Name}: " + ex.StackTrace);
            warningAction.Invoke("Access denied. Please check your credentials and try again.");
        }

        private static void HandleNotSupportedException(NotSupportedException ex, Action<string> warningAction)
        {
            Logger.Error($"Error in {warningAction.Method.Name}: " + ex.StackTrace);
            warningAction.Invoke("An unsupported operation was attempted. Please contact support.");
        }

        private static void HandleIOException(IOException ex, Action<string> warningAction)
        {
            Logger.Error($"Error in {warningAction.Method.Name}: " + ex.StackTrace);
            warningAction.Invoke("There was an input/output error. Please check your file system or network and try again.");
        }

        private static void HandleAggregateException(AggregateException ex, Action<string> warningAction)
        {
            Logger.Error($"Error in {warningAction.Method.Name}: " + ex.StackTrace);
            warningAction.Invoke("Multiple errors occurred. Please review the logs for more details.");
        }

        private static void HandleInvalidTokenException(InvalidTokenException ex, Action<string> warningAction)
        {
            Logger.Error($"Error in {warningAction.Method.Name}: " + ex.StackTrace);
            App.ShowInvalidTokenIssue("Your session token is no longer valid. Please log in again.");
        }

        private static void HandleNoInternetException(NoInternetException ex, Action<string> warningAction)
        {
            Logger.Error($"Error: No internet connection in {warningAction.Method.Name}" + ex.StackTrace);
            warningAction.Invoke($"A network error occurred while connecting to Snaptrude. Please check your internet connection and try again.");
        }

        private static void HandleSnaptrudeDownException(SnaptrudeDownException ex, Action<string> warningAction)
        {
            Logger.Error($"Error: Snaptrude servers are down in {warningAction.Method.Name}" + ex.StackTrace);
            warningAction.Invoke($"Snaptrude servers are down at the moment. Please try again later.");
        }

        private static void HandleGeneralException(Exception ex, Action<string> warningAction)
        {
            Logger.Error($"Error in {warningAction.Method.Name}: " + ex.StackTrace);
            warningAction.Invoke($"An unexpected error occurred. Please contact support. {ex.Message}");
        }
    }
}
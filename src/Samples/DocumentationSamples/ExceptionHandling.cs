using Baseline.Dates;
using Wolverine;
using Wolverine.ErrorHandling;
using Microsoft.Extensions.Hosting;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace DocumentationSamples
{
    public class ExceptionHandling
    {

    }



    public static class AppWithErrorHandling
    {
        public static async Task sample()
        {
            #region sample_AppWithErrorHandling
            using var host = await Host.CreateDefaultBuilder()
                .UseWolverine(opts =>
                {
                    // On a SqlException, reschedule the message to be retried
                    // at 3 seconds, then 15, then 30 seconds later
                    opts.Handlers.OnException<SqlException>()
                        .ScheduleRetry(3.Seconds(), 15.Seconds(), 30.Seconds());

                }).StartAsync();
            #endregion
        }

        public static async Task with_scripted_error_handling()
        {
            #region sample_AppWithScriptedErrorHandling

            using var host = Host.CreateDefaultBuilder()
                .UseWolverine(opts =>
                {
                    opts.Handlers.OnException<TimeoutException>()
                    // Just retry the message again on the
                    // first failure
                    .RetryOnce()

                    // On the 2nd failure, put the message back into the
                    // incoming queue to be retried later
                    .Then.Requeue()

                    // On the 3rd failure, retry the message again after a configurable
                    // cool-off period. This schedules the message
                    .Then.ScheduleRetry(15.Seconds())

                    // On the 4th failure, move the message to the dead letter queue
                    .Then.MoveToErrorQueue()

                    // Or instead you could just discard the message and stop
                    // all processing too!
                    .Then.Discard().AndPauseProcessing(5.Minutes());
                }).StartAsync();

            #endregion
        }
    }



    public class SqlException : Exception{}
}

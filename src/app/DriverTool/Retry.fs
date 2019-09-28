namespace DriverTool

[<AutoOpen>]
module Retry = 
    let retry = new RetryBuilder()
    let retryWithPolicy (retryPolicy : RetryPolicy) (retry : Retry<'T>) = 
        Retry (fun _ -> let (Retry retryFunc) = retry in retryFunc retryPolicy)
    let run (retry : Retry<'T>) (retryPolicy : RetryPolicy) : RetryResult<'T> =
        let (Retry retryFunc) = retry
        retryFunc retryPolicy


    ////// Example
    //let test = 
    //    let random = new Random()
    //    retry {
    //        return 1 / random.Next(0, 2)
    //    }

    ////(test, RetryPolicies.NoRetry()) ||> run
    ////(test, RetryPolicies.Retry 10) ||> run


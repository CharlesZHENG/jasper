# Move to error queue on an unmatched exception

-> id = 421610ac-25a3-4b63-8869-6482c07190de
-> lifecycle = Regression
-> max-retries = 0
-> last-updated = 2017-03-15T19:28:03.6818526Z
-> tags = 

[ErrorHandling]
|> IfTheChainHandlingIs
    [ChainErrorHandling]
    |> RetryOn errorType=DivideByZeroException

|> MessageAttempts
    [Rows]
    |> MessageAttempts-row Attempt=1, errorType=ArgumentNullException

|> MessageResult attempt=1, result=MovedToErrorQueue
~~~

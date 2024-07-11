![Logo](favicon.ico)

# QueueExecutor

a high performance in-memory async queue executor

Supports `.NET 4.5+`, `netstandard1.1`

## Getting Started

Install the 1.0 [NuGet package](https://www.nuget.org/packages/Dao.QueueExecutor).

## Release Notes

See the [Release Notes](ReleaseNotes.md)

## Examples

***QueueExecutor*** is a high performance non-blocking in-memory async queue executor

You can push any item (object, delegate) into the queue, and use multiple executor to handle the item concurrently

**Notice:** If you set more than one executor, the rest of them will only get to work when the fisrt one get busy.
```C#
// declare the queue, and declare a delegate as queue item, and set 4 concurrent executor to execute
public static readonly QueueExecutor<Func<Task<string>>, string> queue = new QueueExecutor<Func<Task<string>>, string>(4);

public QueueExecutorTest()
{
    // set the executor
    queue.Execute += SendRequests_Execute;
}

static async Task<string> SendRequests_Execute(Func<Task<string>> sendingRequest)
{
    // handle the queue item, and return the response
    return await sendingRequest();
}

public void CreateRequest(string content)
{
    // push the queue item
    queue.Push(() => SendingRequest(content));
}
```


***Catcher*** is a high performance non-blocking in-memory async notification

You can make a notification many times in the previous step, and the catcher will receive the notification at least once in the following step, and decide what to do.

**Notice:** If you set more than one catcher, the rest of them will only get to work when the fisrt one get busy.

```C#
// declare the notifier, and set 1 concurrent catcher to handle the notification.
public static readonly Catcher notifier = new Catcher(1);

public CatcherTest()
{
    // set the catcher
    notifier.Catch += Husband_Says_OK;
}

async Task Husband_Says_OK()
{
    // do what ever you want to do while receiving the notification.
    Thread.Sleep(TimeSpan.FromMinutes(10));
}

public void Wife_Is_Yelling()
{
    // create a notification (as many as you want) to anyone who is interested in it.
    for (var i = 0; i < 60; i++)
    {
        notifier.Throw();
    }
}
```

## Matrix

|                                                 | QueueExecutor | Catcher |
|-------------------------------------------------|:-------------:|:-------:|
| Queue based                                     | √             | X       |
| One request fires handler once                  | √             | X       |
| Multiple requests fire handler at least once    | X             | √       |
| Handle request (Execute / Catch)                | √             | √       |
| ~~Handle response (Executed)~~                  | √             | X       |
| Handle exception (OnException)                  | √             | √       |


## Benchmark
Running 10000000 times System.Threading.Channels.Channel's TryWrite & QueueExecutor's Push

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19045.4046/22H2/2022Update)
Intel Xeon E-2246G CPU 3.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 7.0.307

| Method             | Mean     | Error     | StdDev    | Median   | Allocated |
|------------------- |---------:|----------:|----------:|---------:|----------:|
| Channel_Write      | 502.7 ms | 102.70 ms | 302.80 ms | 289.6 ms |     76 MB |
| QueueExecutor_Push | 113.0 ms |   2.26 ms |   2.51 ms | 112.2 ms |   76.8 MB |

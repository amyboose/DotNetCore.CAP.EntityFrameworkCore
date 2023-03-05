using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.EntityFrameworkCore.Persistance;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Serialization;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace DotNetCore.CAP.EntityFrameworkCore.Internal;

internal class EntityFrameworkCapPublisher : IEntityFrameworkCapPublisher
{
    // ReSharper disable once InconsistentNaming
    protected static readonly DiagnosticListener s_diagnosticListener =
        new(CapDiagnosticListenerNames.DiagnosticListenerName);

    private readonly CapOptions _capOptions;
    private readonly IDispatcher _dispatcher;
    private readonly IDataStorage _storage;
    private readonly IBootstrapper _bootstrapper;
    private readonly ISerializer _serializer;

    public EntityFrameworkCapPublisher(IServiceProvider service)
    {
        ServiceProvider = service;
        _bootstrapper = service.GetRequiredService<IBootstrapper>();
        _dispatcher = service.GetRequiredService<IDispatcher>();
        _storage = service.GetRequiredService<IDataStorage>();
        _capOptions = service.GetRequiredService<IOptions<CapOptions>>().Value;
        _serializer = service.GetRequiredService<ISerializer>();
        Transaction = new AsyncLocal<ICapTransaction>();
    }

    public IServiceProvider ServiceProvider { get; }

    public AsyncLocal<ICapTransaction> Transaction { get; }

    public async Task PublishAsync<T>(string name, T? value, IDictionary<string, string?> headers,
        CancellationToken cancellationToken = default)
    {
        await PublishInternalAsync(name, value, headers, null, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishAsync<T>(string name, T? value, string? callbackName = null,
        CancellationToken cancellationToken = default)
    {
        var headers = new Dictionary<string, string?>
        {
            { Headers.CallbackName, callbackName }
        };
        await PublishAsync(name, value, headers, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T? value, IDictionary<string, string?> headers,
        CancellationToken cancellationToken = default)
    {
        if (delayTime <= TimeSpan.Zero)
        {
            throw new ArgumentException("Delay time span must be greater than 0", nameof(delayTime));
        }

        await PublishInternalAsync(name, value, headers, delayTime, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T? value, string? callbackName = null,
        CancellationToken cancellationToken = default)
    {
        var header = new Dictionary<string, string?>
        {
            { Headers.CallbackName, callbackName }
        };

        await PublishDelayAsync(delayTime, name, value, header, cancellationToken).ConfigureAwait(false);
    }

    public void Publish<T>(string name, T? value, string? callbackName = null)
    {
        PublishAsync(name, value, callbackName).ConfigureAwait(false).GetAwaiter().GetResult();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <param name="headers"></param>
    /// <param name="cancellationToken"></param>
    public async Task PublishDomainEvents<T>(ICapDbContext dbContext, string name, T? value, IDictionary<string, string?> headers,
        CancellationToken cancellationToken = default)
    {
        Message message = CreateMessage(name, value, headers);

        long? tracingTimestamp = null;
        try
        {
            tracingTimestamp = TracingBefore(message);

            var mediumMessage = new MediumMessage
            {
                DbId = message.GetId(),
                Origin = message,
                Content = _serializer.Serialize(message),
                Added = DateTime.Now,
                ExpiresAt = null,
                Retries = 0
            };

            PublishedOutbox publishedOutbox = new(new PublishedOutbox.Initializer()
            {
                Id = long.Parse(mediumMessage.DbId),
                Version = "v1",
                Name = name,
                Content = mediumMessage.Content,
                Retries = mediumMessage.Retries,
                Added = mediumMessage.Added,
                ExpiresAt = mediumMessage.ExpiresAt,
                StatusName = nameof(StatusName.Scheduled)
            });

            await dbContext.PublishedOutboxes.AddAsync(publishedOutbox);
            dbContext.AddStoredMessage(mediumMessage);

            TracingAfter(tracingTimestamp, message);
        }
        catch (Exception e)
        {
            TracingError(tracingTimestamp, message, e);

            throw;
        }
    }

    public void Publish<T>(string name, T? value, IDictionary<string, string?> headers)
    {
        PublishAsync(name, value, headers).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public void PublishDelay<T>(TimeSpan delayTime, string name, T? value, IDictionary<string, string?> headers)
    {
        PublishDelayAsync(delayTime, name, value, headers).ConfigureAwait(false);
    }

    public void PublishDelay<T>(TimeSpan delayTime, string name, T? value, string? callbackName = null)
    {
        PublishDelayAsync(delayTime, name, value, callbackName).ConfigureAwait(false);
    }

    private async Task PublishInternalAsync<T>(string name, T? value, IDictionary<string, string?> headers,
        TimeSpan? delayTime = null,
        CancellationToken cancellationToken = default)
    {
        Message message = CreateMessage(name, value, headers, delayTime);
        await StoreMessageAsync(name, message, delayTime, cancellationToken);
    }

    private Message CreateMessage<T>(string name, T? value, IDictionary<string, string?> headers,
        TimeSpan? delayTime = null)
    {
        if (!_bootstrapper.IsStarted)
        {
            throw new InvalidOperationException("CAP has not been started!");
        }
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
        if (!string.IsNullOrEmpty(_capOptions.TopicNamePrefix)) name = $"{_capOptions.TopicNamePrefix}.{name}";

        if (!headers.ContainsKey(Headers.MessageId))
        {
            var messageId = SnowflakeId.Default().NextId().ToString();
            headers.Add(Headers.MessageId, messageId);
        }

        if (!headers.ContainsKey(Headers.CorrelationId))
        {
            headers.Add(Headers.CorrelationId, headers[Headers.MessageId]);
            headers.Add(Headers.CorrelationSequence, 0.ToString());
        }

        headers.Add(Headers.MessageName, name);
        headers.Add(Headers.Type, typeof(T).Name);

        var publishTime = DateTime.Now;
        if (delayTime != null)
        {
            publishTime += delayTime.Value;
            headers.Add(Headers.DelayTime, delayTime.Value.ToString());
            headers.Add(Headers.SentTime, publishTime.ToString());
        }
        else
        {
            headers.Add(Headers.SentTime, publishTime.ToString());
        }

        var message = new Message(headers, value);
        return message;
    }

    private async Task StoreMessageAsync(string name, Message message, TimeSpan? delayTime = null,
        CancellationToken cancellationToken = default)
    {
        long? tracingTimestamp = null;
        try
        {
            tracingTimestamp = TracingBefore(message);

            if (Transaction.Value?.DbTransaction == null)
            {
                var mediumMessage = await _storage.StoreMessageAsync(name, message).ConfigureAwait(false);

                TracingAfter(tracingTimestamp, message);

                if (delayTime != null)
                {
                    var publishTime = DateTime.Now;
                    await _dispatcher.EnqueueToScheduler(mediumMessage, publishTime).ConfigureAwait(false);
                }
                else
                {
                    await _dispatcher.EnqueueToPublish(mediumMessage).ConfigureAwait(false);
                }
            }
            else
            {
                var transaction = (CapTransactionBase)Transaction.Value;

                var mediumMessage = await _storage.StoreMessageAsync(name, message, transaction.DbTransaction)
                    .ConfigureAwait(false);

                TracingAfter(tracingTimestamp, message);

                dynamic dynamicTransaction = transaction;
                dynamicTransaction.AddToSent(mediumMessage);

                if (transaction.AutoCommit) await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            TracingError(tracingTimestamp, message, e);

            throw;
        }
    }

    #region tracing

    private long? TracingBefore(Message message)
    {
        if (s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.BeforePublishMessageStore))
        {
            var eventData = new CapEventDataPubStore
            {
                OperationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Operation = message.GetName(),
                Message = message
            };

            s_diagnosticListener.Write(CapDiagnosticListenerNames.BeforePublishMessageStore, eventData);

            return eventData.OperationTimestamp;
        }

        return null;
    }

    private void TracingAfter(long? tracingTimestamp, Message message)
    {
        if (tracingTimestamp != null &&
            s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.AfterPublishMessageStore))
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var eventData = new CapEventDataPubStore
            {
                OperationTimestamp = now,
                Operation = message.GetName(),
                Message = message,
                ElapsedTimeMs = now - tracingTimestamp.Value
            };

            s_diagnosticListener.Write(CapDiagnosticListenerNames.AfterPublishMessageStore, eventData);
        }
    }

    private void TracingError(long? tracingTimestamp, Message message, Exception ex)
    {
        if (tracingTimestamp != null &&
            s_diagnosticListener.IsEnabled(CapDiagnosticListenerNames.ErrorPublishMessageStore))
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var eventData = new CapEventDataPubStore
            {
                OperationTimestamp = now,
                Operation = message.GetName(),
                Message = message,
                ElapsedTimeMs = now - tracingTimestamp.Value,
                Exception = ex
            };

            s_diagnosticListener.Write(CapDiagnosticListenerNames.ErrorPublishMessageStore, eventData);
        }
    }

    #endregion
}

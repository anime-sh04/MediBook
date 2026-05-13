using MediBook.Schedule.API.Messaging.Contracts;

namespace MediBook.Schedule.API.Messaging.Infrastructure;

public interface ISagaEventPublisher
{
    void PublishPaymentRequested(PaymentRequested @event);
}

using MediBook.Payment.API.Messaging.Contracts;

namespace MediBook.Payment.API.Messaging.Infrastructure;

/// <summary>
/// Defines the contract for publishing payment outcome events.
/// </summary>
public interface ISagaEventPublisher
{
    /// <summary>Published on successful payment.</summary>
    void PublishPaymentSucceeded(PaymentSucceeded @event);

    /// <summary>Published on failed payment (compensation).</summary>
    void PublishPaymentFailed(PaymentFailed @event);
}

using CoreAxis.EventBus;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.IntegrationEvents;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Tests.EventBus
{
    /// <summary>
    /// Unit tests for the InMemoryEventBus.
    /// </summary>
    public class InMemoryEventBusTests
    {
        /// <summary>
        /// Tests that Publish calls the handler for the event type.
        /// </summary>
        [Fact]
        public async Task Publish_ShouldCallHandlerForEventType()
        {
            // Arrange
            var eventBus = new InMemoryEventBus();
            var testEvent = new TestIntegrationEvent();
            var handlerMock = new Mock<IIntegrationEventHandler<TestIntegrationEvent>>();
            
            handlerMock.Setup(h => h.HandleAsync(testEvent))
                .Returns(Task.CompletedTask)
                .Verifiable();

            eventBus.Subscribe(handlerMock.Object);

            // Act
            await eventBus.PublishAsync(testEvent);

            // Assert
            handlerMock.Verify(h => h.HandleAsync(testEvent), Times.Once);
        }

        /// <summary>
        /// Tests that Publish doesn't call the handler after unsubscribing.
        /// </summary>
        [Fact]
        public async Task Publish_ShouldNotCallHandlerAfterUnsubscribe()
        {
            // Arrange
            var eventBus = new InMemoryEventBus();
            var testEvent = new TestIntegrationEvent();
            var handlerMock = new Mock<IIntegrationEventHandler<TestIntegrationEvent>>();
            
            handlerMock.Setup(h => h.HandleAsync(testEvent))
                .Returns(Task.CompletedTask);

            eventBus.Subscribe(handlerMock.Object);
            eventBus.Unsubscribe(handlerMock.Object);

            // Act
            await eventBus.PublishAsync(testEvent);

            // Assert
            handlerMock.Verify(h => h.HandleAsync(testEvent), Times.Never);
        }

        /// <summary>
        /// Tests that Publish calls multiple handlers for the same event type.
        /// </summary>
        [Fact]
        public async Task Publish_ShouldCallMultipleHandlersForSameEventType()
        {
            // Arrange
            var eventBus = new InMemoryEventBus();
            var testEvent = new TestIntegrationEvent();
            var handlerMock1 = new Mock<IIntegrationEventHandler<TestIntegrationEvent>>();
            var handlerMock2 = new Mock<IIntegrationEventHandler<TestIntegrationEvent>>();
            
            handlerMock1.Setup(h => h.HandleAsync(testEvent))
                .Returns(Task.CompletedTask)
                .Verifiable();
                
            handlerMock2.Setup(h => h.HandleAsync(testEvent))
                .Returns(Task.CompletedTask)
                .Verifiable();

            eventBus.Subscribe(handlerMock1.Object);
            eventBus.Subscribe(handlerMock2.Object);

            // Act
            await eventBus.PublishAsync(testEvent);

            // Assert
            handlerMock1.Verify(h => h.HandleAsync(testEvent), Times.Once);
            handlerMock2.Verify(h => h.HandleAsync(testEvent), Times.Once);
        }

        /// <summary>
        /// Tests that Publish only calls handlers for the matching event type.
        /// </summary>
        [Fact]
        public async Task Publish_ShouldOnlyCallHandlersForMatchingEventType()
        {
            // Arrange
            var eventBus = new InMemoryEventBus();
            var testEvent = new TestIntegrationEvent();
            var otherEvent = new OtherIntegrationEvent();
            var testHandlerMock = new Mock<IIntegrationEventHandler<TestIntegrationEvent>>();
            var otherHandlerMock = new Mock<IIntegrationEventHandler<OtherIntegrationEvent>>();
            
            testHandlerMock.Setup(h => h.HandleAsync(testEvent))
                .Returns(Task.CompletedTask)
                .Verifiable();
                
            otherHandlerMock.Setup(h => h.HandleAsync(otherEvent))
                .Returns(Task.CompletedTask)
                .Verifiable();

            eventBus.Subscribe(testHandlerMock.Object);
            eventBus.Subscribe(otherHandlerMock.Object);

            // Act
            await eventBus.PublishAsync(testEvent);

            // Assert
            testHandlerMock.Verify(h => h.HandleAsync(testEvent), Times.Once);
            otherHandlerMock.Verify(h => h.HandleAsync(otherEvent), Times.Never);
        }

        /// <summary>
        /// Tests that PublishDynamic calls the dynamic handler for the event type.
        /// </summary>
        [Fact]
        public async Task PublishDynamic_ShouldCallDynamicHandlerForEventType()
        {
            // Arrange
            var eventBus = new InMemoryEventBus();
            var testEvent = new TestIntegrationEvent();
            var handlerMock = new Mock<IDynamicIntegrationEventHandler>();
            
            handlerMock.Setup(h => h.HandleAsync(It.IsAny<object>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            eventBus.SubscribeDynamic("TestIntegrationEvent", handlerMock.Object);

            // Act
            await eventBus.PublishDynamicAsync(testEvent, "TestIntegrationEvent");

            // Assert
            handlerMock.Verify(h => h.HandleAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Tests that PublishDynamic doesn't call the dynamic handler after unsubscribing.
        /// </summary>
        [Fact]
        public async Task PublishDynamic_ShouldNotCallDynamicHandlerAfterUnsubscribe()
        {
            // Arrange
            var eventBus = new InMemoryEventBus();
            var testEvent = new TestIntegrationEvent();
            var handlerMock = new Mock<IDynamicIntegrationEventHandler>();
            
            handlerMock.Setup(h => h.HandleAsync(It.IsAny<object>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            eventBus.SubscribeDynamic("TestIntegrationEvent", handlerMock.Object);
            eventBus.UnsubscribeDynamic("TestIntegrationEvent", handlerMock.Object);

            // Act
            await eventBus.PublishDynamicAsync(testEvent, "TestIntegrationEvent");

            // Assert
            handlerMock.Verify(h => h.HandleAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Tests that PublishDynamic calls multiple dynamic handlers for the same event name.
        /// </summary>
        [Fact]
        public async Task PublishDynamic_ShouldCallMultipleDynamicHandlersForSameEventName()
        {
            // Arrange
            var eventBus = new InMemoryEventBus();
            var testEvent = new TestIntegrationEvent();
            var handlerMock1 = new Mock<IDynamicIntegrationEventHandler>();
            var handlerMock2 = new Mock<IDynamicIntegrationEventHandler>();
            
            handlerMock1.Setup(h => h.HandleAsync(It.IsAny<object>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
                
            handlerMock2.Setup(h => h.HandleAsync(It.IsAny<object>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            eventBus.SubscribeDynamic("TestIntegrationEvent", handlerMock1.Object);
            eventBus.SubscribeDynamic("TestIntegrationEvent", handlerMock2.Object);

            // Act
            await eventBus.PublishDynamicAsync(testEvent, "TestIntegrationEvent");

            // Assert
            handlerMock1.Verify(h => h.HandleAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Once);
            handlerMock2.Verify(h => h.HandleAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Tests that PublishDynamic only calls dynamic handlers for the matching event name.
        /// </summary>
        [Fact]
        public async Task PublishDynamic_ShouldOnlyCallDynamicHandlersForMatchingEventName()
        {
            // Arrange
            var eventBus = new InMemoryEventBus();
            var testEvent = new TestIntegrationEvent();
            var handlerMock1 = new Mock<IDynamicIntegrationEventHandler>();
            var handlerMock2 = new Mock<IDynamicIntegrationEventHandler>();
            
            handlerMock1.Setup(h => h.HandleAsync(It.IsAny<object>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
                
            handlerMock2.Setup(h => h.HandleAsync(It.IsAny<object>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            eventBus.SubscribeDynamic("TestIntegrationEvent", handlerMock1.Object);
            eventBus.SubscribeDynamic("OtherIntegrationEvent", handlerMock2.Object);

            // Act
            await eventBus.PublishDynamicAsync(testEvent, "TestIntegrationEvent");

            // Assert
            handlerMock1.Verify(h => h.HandleAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Once);
            handlerMock2.Verify(h => h.HandleAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Never);
        }
    }

    /// <summary>
    /// Test integration event for testing purposes.
    /// </summary>
    public class TestIntegrationEvent : IntegrationEvent
    {
        // EventType is inherited from base class and doesn't need to be overridden
    }

    /// <summary>
    /// Another test integration event for testing purposes.
    /// </summary>
    public class OtherIntegrationEvent : IntegrationEvent
    {
        // EventType is inherited from base class and doesn't need to be overridden
    }
}
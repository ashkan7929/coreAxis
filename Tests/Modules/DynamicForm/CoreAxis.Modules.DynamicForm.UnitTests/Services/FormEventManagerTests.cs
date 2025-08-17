using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Application.Services.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Modules.DynamicForm.UnitTests.Services
{
    public class FormEventManagerTests
    {
        private readonly Mock<ILogger<FormEventManager>> _loggerMock;
        private readonly FormEventManager _formEventManager;
        private readonly Mock<IFormEventHandler> _handlerMock;
        private readonly Guid _formId = Guid.NewGuid();

        public FormEventManagerTests()
        {
            _loggerMock = new Mock<ILogger<FormEventManager>>();
            _formEventManager = new FormEventManager(_loggerMock.Object);
            _handlerMock = new Mock<IFormEventHandler>();
        }

        [Fact]
        public void RegisterHandler_ShouldAddHandlerToForm()
        {
            // Act
            _formEventManager.RegisterHandler(_formId, _handlerMock.Object);

            // Assert
            var handlers = _formEventManager.GetHandlers(_formId);
            Assert.Single(handlers);
            Assert.Contains(_handlerMock.Object, handlers);
        }

        [Fact]
        public void RegisterGlobalHandler_ShouldAddGlobalHandler()
        {
            // Act
            _formEventManager.RegisterGlobalHandler(_handlerMock.Object);

            // Assert
            var globalHandlers = _formEventManager.GetGlobalHandlers();
            Assert.Single(globalHandlers);
            Assert.Contains(_handlerMock.Object, globalHandlers);
        }

        [Fact]
        public void RegisterHandler_WithNullHandler_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _formEventManager.RegisterHandler(_formId, null!));
        }

        [Fact]
        public void RegisterGlobalHandler_WithNullHandler_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _formEventManager.RegisterGlobalHandler(null!));
        }

        [Fact]
        public void UnregisterHandler_ShouldRemoveHandlerFromForm()
        {
            // Arrange
            _formEventManager.RegisterHandler(_formId, _handlerMock.Object);

            // Act
            _formEventManager.UnregisterHandler(_formId, _handlerMock.Object);

            // Assert
            var handlers = _formEventManager.GetHandlers(_formId);
            Assert.Empty(handlers);
        }

        [Fact]
        public void UnregisterGlobalHandler_ShouldRemoveGlobalHandler()
        {
            // Arrange
            _formEventManager.RegisterGlobalHandler(_handlerMock.Object);

            // Act
            _formEventManager.UnregisterGlobalHandler(_handlerMock.Object);

            // Assert
            var globalHandlers = _formEventManager.GetGlobalHandlers();
            Assert.Empty(globalHandlers);
        }

        [Fact]
        public async Task TriggerOnInitAsync_ShouldCallHandlerOnInit()
        {
            // Arrange
            var context = CreateTestContext();
            var expectedResult = new FormEventResult { Success = true, Message = "Test" };
            
            _handlerMock.Setup(h => h.OnInitAsync(It.IsAny<FormEventContext>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(expectedResult);
            
            _formEventManager.RegisterHandler(_formId, _handlerMock.Object);

            // Act
            var result = await _formEventManager.TriggerOnInitAsync(context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal(expectedResult, result.Value[0]);
            _handlerMock.Verify(h => h.OnInitAsync(context, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TriggerOnChangeAsync_ShouldCallHandlerOnChange()
        {
            // Arrange
            var context = CreateTestContext();
            context.ChangedField = "testField";
            var expectedResult = new FormEventResult { Success = true, Message = "Test" };
            
            _handlerMock.Setup(h => h.OnChangeAsync(It.IsAny<FormEventContext>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(expectedResult);
            
            _formEventManager.RegisterHandler(_formId, _handlerMock.Object);

            // Act
            var result = await _formEventManager.TriggerOnChangeAsync(context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal(expectedResult, result.Value[0]);
            _handlerMock.Verify(h => h.OnChangeAsync(context, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TriggerBeforeSubmitAsync_ShouldCallHandlerBeforeSubmit()
        {
            // Arrange
            var context = CreateTestContext();
            var expectedResult = new FormEventResult { Success = true, Message = "Test" };
            
            _handlerMock.Setup(h => h.BeforeSubmitAsync(It.IsAny<FormEventContext>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(expectedResult);
            
            _formEventManager.RegisterHandler(_formId, _handlerMock.Object);

            // Act
            var result = await _formEventManager.TriggerBeforeSubmitAsync(context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal(expectedResult, result.Value[0]);
            _handlerMock.Verify(h => h.BeforeSubmitAsync(context, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TriggerAfterSubmitAsync_ShouldCallHandlerAfterSubmit()
        {
            // Arrange
            var context = CreateTestContext();
            var expectedResult = new FormEventResult { Success = true, Message = "Test" };
            
            _handlerMock.Setup(h => h.AfterSubmitAsync(It.IsAny<FormEventContext>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(expectedResult);
            
            _formEventManager.RegisterHandler(_formId, _handlerMock.Object);

            // Act
            var result = await _formEventManager.TriggerAfterSubmitAsync(context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal(expectedResult, result.Value[0]);
            _handlerMock.Verify(h => h.AfterSubmitAsync(context, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TriggerBeforeSubmitAsync_WithCancelledContext_ShouldStopExecution()
        {
            // Arrange
            var context = CreateTestContext();
            var handler1Mock = new Mock<IFormEventHandler>();
            var handler2Mock = new Mock<IFormEventHandler>();
            
            handler1Mock.Setup(h => h.BeforeSubmitAsync(It.IsAny<FormEventContext>(), It.IsAny<CancellationToken>()))
                       .Callback<FormEventContext, CancellationToken>((ctx, ct) => ctx.Cancel = true)
                       .ReturnsAsync(new FormEventResult { Success = true, Message = "Cancelled" });
            
            handler2Mock.Setup(h => h.BeforeSubmitAsync(It.IsAny<FormEventContext>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new FormEventResult { Success = true, Message = "Should not be called" });
            
            _formEventManager.RegisterHandler(_formId, handler1Mock.Object);
            _formEventManager.RegisterHandler(_formId, handler2Mock.Object);

            // Act
            var result = await _formEventManager.TriggerBeforeSubmitAsync(context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value); // Only first handler should be executed
            handler1Mock.Verify(h => h.BeforeSubmitAsync(context, It.IsAny<CancellationToken>()), Times.Once);
            handler2Mock.Verify(h => h.BeforeSubmitAsync(It.IsAny<FormEventContext>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task TriggerEvent_WithGlobalAndFormHandlers_ShouldCallBoth()
        {
            // Arrange
            var context = CreateTestContext();
            var globalHandlerMock = new Mock<IFormEventHandler>();
            var formHandlerMock = new Mock<IFormEventHandler>();
            
            globalHandlerMock.Setup(h => h.OnInitAsync(It.IsAny<FormEventContext>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new FormEventResult { Success = true, Message = "Global" });
            
            formHandlerMock.Setup(h => h.OnInitAsync(It.IsAny<FormEventContext>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new FormEventResult { Success = true, Message = "Form" });
            
            _formEventManager.RegisterGlobalHandler(globalHandlerMock.Object);
            _formEventManager.RegisterHandler(_formId, formHandlerMock.Object);

            // Act
            var result = await _formEventManager.TriggerOnInitAsync(context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            globalHandlerMock.Verify(h => h.OnInitAsync(context, It.IsAny<CancellationToken>()), Times.Once);
            formHandlerMock.Verify(h => h.OnInitAsync(context, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TriggerEvent_WithHandlerException_ShouldContinueWithOtherHandlers()
        {
            // Arrange
            var context = CreateTestContext();
            var failingHandlerMock = new Mock<IFormEventHandler>();
            var successHandlerMock = new Mock<IFormEventHandler>();
            
            failingHandlerMock.Setup(h => h.OnInitAsync(It.IsAny<FormEventContext>(), It.IsAny<CancellationToken>()))
                             .ThrowsAsync(new InvalidOperationException("Test exception"));
            
            successHandlerMock.Setup(h => h.OnInitAsync(It.IsAny<FormEventContext>(), It.IsAny<CancellationToken>()))
                             .ReturnsAsync(new FormEventResult { Success = true, Message = "Success" });
            
            _formEventManager.RegisterHandler(_formId, failingHandlerMock.Object);
            _formEventManager.RegisterHandler(_formId, successHandlerMock.Object);

            // Act
            var result = await _formEventManager.TriggerOnInitAsync(context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            Assert.False(result.Value[0].Success); // First handler failed
            Assert.True(result.Value[1].Success);  // Second handler succeeded
        }

        private FormEventContext CreateTestContext()
        {
            return new FormEventContext
            {
                FormId = _formId,
                UserId = "test-user",
                TenantId = "test-tenant",
                FormData = new Dictionary<string, object?> { { "field1", "value1" } },
                PreviousFormData = new Dictionary<string, object?>(),
                Metadata = new Dictionary<string, object?>(),
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
using Moq;
using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResumeAI.Payment.API.Controllers;
using ResumeAI.Payment.API.Data;
using ResumeAI.Payment.API.DTOs;
using ResumeAI.Payment.API.Models;
using ResumeAI.Payment.API.Services;
using MassTransit;
using Microsoft.Extensions.Configuration;
using FluentAssertions;

namespace ResumeAI.Payment.Tests;

[TestFixture]
public class PaymentControllerTests
{
    private Mock<IPaymentGateway> _mockGateway;
    private Mock<IPublishEndpoint> _mockPublish;
    private Mock<IConfiguration> _mockConfig;
    private PaymentDbContext _context;

    [SetUp]
    public void Setup()
    {
        _mockGateway = new Mock<IPaymentGateway>();
        _mockPublish = new Mock<IPublishEndpoint>();
        _mockConfig  = new Mock<IConfiguration>();

        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PaymentDbContext(options);

        _mockConfig.Setup(c => c["Razorpay:KeyId"]).Returns("test_key");
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task CreateOrder_ReturnsOk_WithRazorpayId()
    {
        // Arrange
        _mockGateway.Setup(g => g.CreateOrder(It.IsAny<decimal>(), "INR"))
            .ReturnsAsync("order_test_123");

        var controller = new PaymentController(_context, _mockGateway.Object, _mockPublish.Object, _mockConfig.Object);
        var request = new CreateOrderRequest(1, 499, "Premium");

        // Act
        var result = await controller.CreateOrder(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CreateOrderResponse>().Subject;
        response.OrderId.Should().Be("order_test_123");
        
        var tx = await _context.Transactions.FirstOrDefaultAsync();
        tx.Should().NotBeNull();
        tx!.RazorpayOrderId.Should().Be("order_test_123");
    }

    [Test]
    public async Task VerifyPayment_Succeeds_WhenSignatureIsValid()
    {
        // Arrange
        var tx = new PaymentTransaction
        {
            UserId = 1,
            Amount = 499,
            RazorpayOrderId = "order_123",
            Status = PaymentStatus.Pending
        };
        _context.Transactions.Add(tx);
        await _context.SaveChangesAsync();

        _mockGateway.Setup(g => g.VerifySignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var controller = new PaymentController(_context, _mockGateway.Object, _mockPublish.Object, _mockConfig.Object);
        var request = new VerifyPaymentRequest("order_123", "pay_123", "sig_123", 1);

        // Act
        var result = await controller.VerifyPayment(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var updatedTx = await _context.Transactions.FirstAsync();
        updatedTx.Status.Should().Be(PaymentStatus.Captured);
        
        _mockPublish.Verify(p => p.Publish(It.IsAny<ResumeAI.Shared.Events.PaymentSucceededMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

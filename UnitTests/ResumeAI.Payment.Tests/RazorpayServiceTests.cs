using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using ResumeAI.Payment.API.Services;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;

namespace ResumeAI.Payment.Tests;

[TestFixture]
public class RazorpayServiceTests
{
    private Mock<IConfiguration> _mockConfig;
    private RazorpayService _service;
    private readonly string _testSecret = "test_secret_123";

    [SetUp]
    public void Setup()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["Razorpay:KeySecret"]).Returns(_testSecret);
        _mockConfig.Setup(c => c["Razorpay:KeyId"]).Returns("test_key_id");
        
        _service = new RazorpayService(_mockConfig.Object);
    }

    [Test]
    public void VerifySignature_ReturnsTrue_ForValidSignature()
    {
        // Arrange
        string orderId = "order_O8vGfSjXzXzXzX";
        string paymentId = "pay_O8vGg1XzXzXzX";
        
        // Calculate expected signature manually
        string payload = $"{orderId}|{paymentId}";
        byte[] keyBytes = Encoding.UTF8.GetBytes(_testSecret);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        
        using var hmac = new HMACSHA256(keyBytes);
        byte[] hashBytes = hmac.ComputeHash(payloadBytes);
        string expectedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        // Act
        bool isValid = _service.VerifySignature(orderId, paymentId, expectedSignature);

        // Assert
        isValid.Should().BeTrue();
    }

    [Test]
    public void VerifySignature_ReturnsFalse_ForInvalidSignature()
    {
        // Act
        bool isValid = _service.VerifySignature("order_1", "pay_1", "wrong_signature");

        // Assert
        isValid.Should().BeFalse();
    }
}

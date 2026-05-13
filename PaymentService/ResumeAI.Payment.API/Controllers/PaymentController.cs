using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResumeAI.Payment.API.Data;
using ResumeAI.Payment.API.DTOs;
using ResumeAI.Payment.API.Models;
using ResumeAI.Payment.API.Services;
using MassTransit;
using ResumeAI.Shared.Events;

namespace ResumeAI.Payment.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly PaymentDbContext _context;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IConfiguration _config;

    public PaymentController(
        PaymentDbContext context,
        IPaymentGateway paymentGateway,
        IPublishEndpoint publishEndpoint,
        IConfiguration config)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _publishEndpoint = publishEndpoint;
        _config = config;
    }

    [HttpPost("create-order")]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            Console.WriteLine($"Creating order for User: {request.UserId}, Amount: {request.Amount}");
            var razorpayOrderId = await _paymentGateway.CreateOrder(request.Amount, "INR");

            var transaction = new PaymentTransaction
            {
                UserId = request.UserId,
                Amount = request.Amount,
                PlanName = request.PlanName,
                RazorpayOrderId = razorpayOrderId,
                Status = PaymentStatus.Pending
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Order created successfully: {razorpayOrderId}");

            return Ok(new CreateOrderResponse(
                razorpayOrderId, 
                request.Amount, 
                "INR", 
                _config["Razorpay:KeyId"] ?? "rzp_test_mock_key"
            ));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR creating order: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"INNER ERROR: {ex.InnerException.Message}");
            
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.RazorpayOrderId == request.RazorpayOrderId);

        if (transaction == null)
            return NotFound("Order not found");

        var isValid = _paymentGateway.VerifySignature(
            request.RazorpayOrderId, 
            request.RazorpayPaymentId, 
            request.RazorpaySignature);

        if (isValid)
        {
            transaction.Status = PaymentStatus.Captured;
            transaction.RazorpayPaymentId = request.RazorpayPaymentId;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify other services
            await _publishEndpoint.Publish(new PaymentSucceededMessage
            {
                UserId = transaction.UserId,
                Amount = transaction.Amount,
                PlanName = transaction.PlanName ?? "Premium",
                PaymentId = transaction.RazorpayPaymentId,
                Timestamp = DateTime.UtcNow
            });

            return Ok(new { status = "success", message = "Payment verified successfully" });
        }

        transaction.Status = PaymentStatus.Failed;
        await _context.SaveChangesAsync();

        return BadRequest(new { status = "error", message = "Invalid signature" });
    }

    [HttpGet("history/{userId}")]
    public async Task<ActionResult<IEnumerable<PaymentTransaction>>> GetHistory(int userId)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
}

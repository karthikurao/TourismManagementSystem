# Tourism Management System - Stripe Payment Integration

## Stripe Setup Instructions

### 1. Create a Stripe Account
1. Go to [https://stripe.com](https://stripe.com)
2. Sign up for a free account
3. Verify your email address

### 2. Get Your API Keys
1. Log into your Stripe Dashboard
2. Click on "Developers" in the left sidebar
3. Click on "API keys"
4. Copy your **Publishable key** and **Secret key** from the test data section

### 3. Configure Your Application
1. Open `appsettings.json` in the TourismManagementSystem project
2. Replace the placeholder keys with your actual Stripe test keys:

```json
{
  "Stripe": {
    "PublishableKey": "pk_test_your_actual_publishable_key_here",
    "SecretKey": "sk_test_your_actual_secret_key_here"
  }
}
```

### 4. Test Credit Card Numbers
Stripe provides test credit card numbers for testing:

- **Success**: `4242 4242 4242 4242`
- **Decline**: `4000 0000 0000 0002`
- **Insufficient funds**: `4000 0000 0000 9995`

For all test cards:
- Use any future expiry date (e.g., 12/25)
- Use any 3-digit CVC (e.g., 123)
- Use any zip code (e.g., 12345)

### 5. Payment Flow
1. User creates a booking (status: "Pending")
2. User is redirected to Stripe Checkout
3. After successful payment:
   - Booking status changes to "Confirmed"
   - Package seats are reduced
   - Payment record is created with status "Success"
   - Receipt URLs are retrieved and stored

### 6. Features Implemented
- ? Secure Stripe Checkout integration
- ? Real-time payment validation
- ? Automatic seat allocation after payment
- ? Payment receipts with Stripe invoice download
- ? Booking status management
- ? Refund handling (for cancellations)
- ? Test mode with fake payments
- ? Professional UI/UX
- ? Multiple receipt URL retrieval methods

### 7. Testing the Integration
1. Create a user account and login
2. Browse packages and create a booking
3. You'll be redirected to the payment page
4. Click "Pay Now" to go to Stripe Checkout
5. Use test card `4242 4242 4242 4242` with any future date and CVC
6. Complete the payment to see the confirmation
7. Try downloading the Stripe receipt from the confirmation page or My Bookings

### 8. Receipt Download Troubleshooting

#### If "Stripe receipt not available" appears:

**Method 1: Try the "Try Get Stripe Receipt" button**
- This attempts to retrieve the receipt from multiple Stripe sources
- Works for older payments that might not have stored receipt URLs

**Method 2: Check Stripe Dashboard**
1. Log into your Stripe Dashboard
2. Go to Payments ? All payments
3. Find your payment and click on it
4. Look for the "Receipt" or "Invoice" section
5. Copy the URL manually if needed

**Method 3: Debug the payment (for developers)**
- Visit `/Payment/DebugPayment/{paymentId}` to see payment details
- Check which Stripe IDs are stored in the database
- Verify the payment was processed correctly

**Method 4: Admin refresh (requires admin role)**
- Admins can use `/Payment/RefreshStripeReceipt/{paymentId}` to force refresh

#### Common Issues and Solutions:

**Issue**: "Payment initialization failed: You may only specify one of these parameters: customer, customer_email"
**Cause**: Stripe API conflict when both Customer ID and CustomerEmail are provided
**Solution**: Fixed in code - now uses either Customer ID or CustomerEmail, but not both

**Issue**: "Stripe receipt not available"
**Cause**: Receipt URL not retrieved during payment processing
**Solution**: Use the "Try Get Stripe Receipt" button or check Stripe Dashboard

**Issue**: "Error downloading receipt"
**Cause**: Network issues or invalid Stripe keys
**Solution**: Check internet connection and verify Stripe API keys

**Issue**: Receipt button doesn't appear
**Cause**: Payment status is not "Success" or PaymentId is missing
**Solution**: Verify payment was completed successfully

**Issue**: "Error creating Stripe customer"
**Cause**: Invalid Stripe API keys or network issues
**Solution**: Verify Stripe keys in appsettings.json and internet connection

### 9. API Endpoints for Receipts

- `GET /Payment/GetStripeReceiptUrl/{paymentId}` - Get receipt URL via AJAX
- `GET /Payment/DownloadStripeReceipt/{paymentId}` - Direct redirect to receipt
- `POST /Payment/RefreshStripeReceipt/{paymentId}` - Admin: Force refresh receipt URL
- `GET /Payment/DebugPayment/{paymentId}` - Debug: Show payment details
- `GET /Payment/TestStripeConnection` - Admin: Test Stripe connection

### 10. Going Live
When ready for production:
1. Activate your Stripe account
2. Replace test keys with live keys
3. Update webhook endpoints if needed
4. Enable live mode in Stripe dashboard
5. Test thoroughly with small amounts

## Important Notes
- This implementation uses Stripe Checkout (hosted payment page)
- All payments are processed securely by Stripe
- PCI compliance is handled by Stripe
- No sensitive payment data is stored in your database
- The integration includes proper error handling and security checks
- Receipt URLs are retrieved using multiple fallback methods
- Invoice creation is enabled for better receipt generation

## Support
For Stripe-related issues, refer to the [Stripe Documentation](https://stripe.com/docs) or contact Stripe support.

## Troubleshooting Receipt Downloads

If you're having trouble downloading Stripe receipts, try these steps in order:

1. **Basic Check**: Ensure payment status is "Success"
2. **Try Refresh**: Use "Try Get Stripe Receipt" button
3. **Check Database**: Verify StripeReceiptUrl field is populated
4. **Stripe Dashboard**: Manually find receipt in Stripe dashboard
5. **Contact Support**: If all else fails, contact system administrator
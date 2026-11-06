# QA: POST /api/Order/SelectOption

Body contains orderId and orderInqueryOptionId. No pickupId or schedule is required.
Only the owner can confirm an option belonging to that order. The option's payment method and amounts are preserved.
Exactly one option is chosen. Record selection history and keep online payment as a separate action.
Confirmation must not assign PickupDateTime or create a payment. Expired carrier quotes and in-flight, uncertain or previously registered shipments reject changes. Rejected/draft carrier shipments may be reselected; payment state alone does not finalize shipping. Legacy non-carrier paid/pending quotes retain their existing payment safeguards.
Concurrent selection/quotation/registration uses the same order-row lock.
Response retains pickupOptions as a legacy price-option name; description contains no fabricated time.
Tests: OrderQuoteFlowTests, OrderOwnershipTests, PaymentFlowTests.

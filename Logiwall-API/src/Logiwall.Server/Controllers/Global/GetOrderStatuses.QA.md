# QA: GET /api/Common/GetOrderStatuses

Public endpoint; no body. Returns ServiceResponse of general status id/code/Persian title. It does not return carrier-specific codes or payment statuses. GetOrders status filters use these IDs.

Check Draft=0, Registered=10, Delivered=20 and Cancelled=21. The complete catalogue includes registration recovery, all mapped carrier stages and multi-parcel aggregate statuses. Coverage: ShipmentTests, OrderStatusAggregationTests.

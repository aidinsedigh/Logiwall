# QA: development OTP

In Development with Otp:UseDevelopmentCode=true, SendOtpCode stores 12345 and returns success without calling the SMS service.
The default Development config enables this temporary local mode. Restart the backend after changing configuration.
Production ignores this option and uses the normal random code and SMS flow.

Verify a new phone can request a code and confirm 12345 to receive a token, even if the SMS service is unavailable.
Verify wrong/expired/consumed codes fail. Disabled accounts, five-attempt limit and resend cooldown still apply.
Normal-mode SMS failures must still return failure. Keep OTP values out of logs and API responses.
Registration completeness and ownership behavior remain unchanged.

Automated coverage: DevelopmentOtpTests and OtpSendPathTests, with an isolated SQL Server database.
